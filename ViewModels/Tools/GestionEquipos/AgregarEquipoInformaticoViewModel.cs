using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.DatabaseConnection;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;
using Microsoft.EntityFrameworkCore;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using GestLog.Modules.Personas.Models;
using Modules.Personas.Interfaces;

namespace GestLog.ViewModels.Tools.GestionEquipos
{
    public partial class AgregarEquipoInformaticoViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isLoadingRam;

        [ObservableProperty]
        private ObservableCollection<SlotRamEntity> listaRam = new();

        [ObservableProperty]
        private string codigo = string.Empty;
        [ObservableProperty]
        private string usuarioAsignado = string.Empty;
        [ObservableProperty]
        private string nombreEquipo = string.Empty;
        [ObservableProperty]
        private string sede = "Administrativa - Barranquilla";
        [ObservableProperty]
        private string marca = string.Empty;
        [ObservableProperty]
        private string modelo = string.Empty;
        [ObservableProperty]
        private string procesador = string.Empty;
        [ObservableProperty]
        private string so = string.Empty;
        [ObservableProperty]
        private string serialNumber = string.Empty;
        [ObservableProperty]
        private string observaciones = string.Empty;

        [ObservableProperty]
        private ObservableCollection<DiscoEntity> listaDiscos = new();

        [ObservableProperty]
        private ObservableCollection<Persona> personasDisponibles = new();

        [ObservableProperty]
        private Persona? personaAsignada;

        [ObservableProperty]
        private string filtroPersonaAsignada = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Persona> personasFiltradas = new();

        public string[] TiposRam { get; } = new[] { "DDR3", "DDR4", "DDR5", "LPDDR4", "LPDDR5" };
        public string[] TiposDisco { get; } = new[] { "HDD", "SSD", "NVMe", "eMMC" };

        [ObservableProperty]
        private bool isLoadingDiscos;        
        private readonly ICurrentUserService _currentUserService;
        private CurrentUserInfo _currentUser;
        private readonly IGestLogLogger _logger;

        [ObservableProperty]
        private bool canGuardarEquipo;
        [ObservableProperty]
        private bool canObtenerCamposAutomaticos;
        [ObservableProperty]
        private bool canObtenerDiscosAutomaticos;
        [ObservableProperty]
        private bool canAgregarDiscoManual;
        [ObservableProperty]
        private bool canEliminarDisco;        
        public AgregarEquipoInformaticoViewModel(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            
            // Obtener logger del service provider
            var serviceProvider = LoggingService.GetServiceProvider();
            _logger = serviceProvider.GetRequiredService<IGestLogLogger>();
            
            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;
        }

        private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
        {
            _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
        }        
        public void RecalcularPermisos()
        {
            CanGuardarEquipo = _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");
            CanObtenerCamposAutomaticos = _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");
            CanObtenerDiscosAutomaticos = _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");
            CanAgregarDiscoManual = _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");
            CanEliminarDisco = _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");
        }

        public async Task InicializarAsync()
        {
            await InicializarPersonasAsignadasAsync();
            // ...cargar otros datos si es necesario...
        }

        [RelayCommand(CanExecute = nameof(CanGuardarEquipo))]
        public async Task GuardarEquipoAsync()
        {
            if (string.IsNullOrWhiteSpace(Codigo) || string.IsNullOrWhiteSpace(NombreEquipo))
            {
                MessageBox.Show("El código y el nombre del equipo son obligatorios.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<GestLogDbContext>()
                .UseSqlServer("Data Source=.;Initial Catalog=GestLog;Integrated Security=True;TrustServerCertificate=True;") // TODO: Usar configuración real
                .Options;
            using var dbContext = new GestLogDbContext(options);
            if (dbContext.EquiposInformaticos.Any(e => e.Codigo == Codigo))
            {
                MessageBox.Show("Ya existe un equipo con ese código.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var equipo = new EquipoInformaticoEntity
            {
                Codigo = Codigo,
                UsuarioAsignado = PersonaAsignada?.NombreCompleto ?? string.Empty,
                NombreEquipo = NombreEquipo,
                Sede = Sede,
                Marca = Marca,
                Modelo = Modelo,
                Procesador = Procesador,
                SO = So,
                SerialNumber = SerialNumber,
                Observaciones = Observaciones,
                FechaCreacion = DateTime.Now,
                Estado = "Disponible"
            };
            dbContext.EquiposInformaticos.Add(equipo);
            int slotNum = 1;
            foreach (var slot in ListaRam)
            {
                slot.CodigoEquipo = Codigo;
                slot.NumeroSlot = slotNum++;
                dbContext.SlotsRam.Add(slot);
            }
            int discoNum = 1;
            foreach (var disco in ListaDiscos)
            {
                disco.CodigoEquipo = Codigo;
                disco.NumeroDisco = discoNum++;
                dbContext.Discos.Add(disco);
            }
            await dbContext.SaveChangesAsync();
            MessageBox.Show("Equipo guardado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }        
        [RelayCommand(CanExecute = nameof(CanObtenerCamposAutomaticos))]
        public async Task ObtenerCamposAutomaticosAsync()
        {
            if (IsLoadingRam) return;
            IsLoadingRam = true;
            try
            {
                await Task.Run(() => ObtenerCamposAutomaticos());
                // También obtener discos automáticamente
                await Task.Run(() => ObtenerDiscosAutomaticos());                
                // Detección automática del sistema operativo
                if (string.IsNullOrWhiteSpace(So))
                {
                    So = System.Environment.OSVersion.VersionString;
                }
            }
            finally
            {
                IsLoadingRam = false;
            }
        }        
        private void ObtenerCamposAutomaticos()
        {
            _logger.LogInformation("🔍 Iniciando detección automática de campos del equipo");
            
            // Marca
            Marca = EjecutarCmdWmic("computersystem get manufacturer");
            if (string.IsNullOrWhiteSpace(Marca))
                Marca = EjecutarPowerShell("Get-WmiObject -Class Win32_ComputerSystem | Select-Object -ExpandProperty Manufacturer");
            
            // Modelo
            Modelo = EjecutarCmdWmic("computersystem get model");
            if (string.IsNullOrWhiteSpace(Modelo))
                Modelo = EjecutarPowerShell("Get-WmiObject -Class Win32_ComputerSystem | Select-Object -ExpandProperty Model");
            
            // Nombre equipo
            NombreEquipo = EjecutarCmd("hostname");
            if (string.IsNullOrWhiteSpace(NombreEquipo))
                NombreEquipo = EjecutarPowerShell("$env:COMPUTERNAME");
            
            // Procesador
            Procesador = EjecutarCmdWmic("cpu get name");
            if (string.IsNullOrWhiteSpace(Procesador))
                Procesador = EjecutarPowerShell("Get-WmiObject -Class Win32_Processor | Select-Object -ExpandProperty Name");
            
            // SO
            So = EjecutarCmdWmic("os get caption");
            if (string.IsNullOrWhiteSpace(So))
            {
                So = EjecutarPowerShell("Get-WmiObject -Class Win32_OperatingSystem | Select-Object -ExpandProperty Caption");
            }
            
            if (string.IsNullOrWhiteSpace(So))
            {
                So = System.Environment.OSVersion.VersionString;
            }
            
            // Serial
            SerialNumber = EjecutarCmdWmic("bios get serialnumber");
            if (string.IsNullOrWhiteSpace(SerialNumber))
                SerialNumber = EjecutarPowerShell("Get-WmiObject -Class Win32_BIOS | Select-Object -ExpandProperty SerialNumber");
            
            // RAM
            ObtenerRamAutomatica();
            
            _logger.LogInformation("✅ Detección automática de campos completada");
        }

        private string EjecutarCmdWmic(string args)
        {
            return EjecutarProceso("cmd.exe", $"/c wmic {args}");
        }
        private string EjecutarCmd(string args)
        {
            return EjecutarProceso("cmd.exe", $"/c {args}");
        }
        private string EjecutarPowerShell(string args)
        {
            return EjecutarProceso("powershell.exe", $"-Command \"{args}\"");
        }

        /// <summary>
        /// Ejecuta un comando PowerShell y devuelve la salida completa sin filtrar.
        /// Usado para comandos que requieren toda la salida (como CSV).
        /// </summary>
        private string EjecutarPowerShellCompleto(string args)
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = $"-Command \"{args}\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                
                _logger.LogDebug($"[PowerShell] Ejecutando comando completo: {args}");
                
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                
                if (!string.IsNullOrWhiteSpace(error))
                {
                    _logger.LogWarning($"[PowerShell] Error en comando: {error}");
                }
                
                _logger.LogDebug($"[PowerShell] Salida completa recibida ({output?.Length ?? 0} caracteres)");
                return output ?? string.Empty;
            }
            catch (Exception ex) 
            { 
                _logger.LogError(ex, $"[PowerShell] Error ejecutando comando: {args}");
                return string.Empty; 
            }
        }

        private string EjecutarProceso(string fileName, string arguments)
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                
                if (!string.IsNullOrWhiteSpace(error))
                {
                    _logger.LogDebug("⚠️ Error del proceso {FileName}: {Error}", fileName, error);
                }
                
                if (string.IsNullOrWhiteSpace(output))
                {
                    return string.Empty;
                }
                
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
                
                if (lines.Length > 1 && !lines[1].ToLower().Contains("manufacturer") && !lines[1].ToLower().Contains("model") && !lines[1].ToLower().Contains("name") && !lines[1].ToLower().Contains("caption") && !lines[1].ToLower().Contains("serialnumber"))
                {
                    return lines[1].Trim();
                }
                if (lines.Length > 0)
                {
                    return lines[0].Trim();
                }
                
                return string.Empty;
            }
            catch (Exception ex) 
            { 
                _logger.LogError(ex, "❌ Error ejecutando proceso {FileName} {Arguments}", fileName, arguments);
                return string.Empty; 
            }
        }        
        private void ObtenerRamAutomatica()
        {
            _logger.LogDebug("[RAM] Iniciando detección con WMI clásico");
            Application.Current.Dispatcher.Invoke(() => ListaRam.Clear());
            try
            {
                // Usar Get-WmiObject Win32_PhysicalMemory para máxima compatibilidad
                var output = EjecutarPowerShellCompleto("Get-WmiObject Win32_PhysicalMemory | Select-Object SMBIOSMemoryType, Capacity, Manufacturer, PartNumber, Speed | ConvertTo-Csv -NoTypeInformation");
                _logger.LogDebug($"[RAM] Resultado PowerShell Win32_PhysicalMemory CSV: '{output}'");
                
                if (string.IsNullOrWhiteSpace(output))
                {
                    _logger.LogWarning("[RAM] La salida de PowerShell está vacía");
                    throw new Exception("PowerShell no devolvió ningún resultado");
                }
                
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                _logger.LogDebug($"[RAM] Total de líneas obtenidas: {lines.Count}");
                
                if (lines.Count <= 1)
                {
                    _logger.LogWarning("[RAM] Solo se obtuvo la cabecera CSV, sin datos de módulos");
                    throw new Exception("No hay datos de módulos de RAM en la salida");
                }
                
                int slotsOcupados = 0;
                for (int i = 1; i < lines.Count; i++)
                {
                    var csv = lines[i];
                    _logger.LogDebug($"[RAM] Procesando línea CSV {i}: '{csv}'");
                    // El CSV puede tener comas en PartNumber, así que usar un parser robusto
                    var parts = ParseCsvLine(csv);
                    _logger.LogDebug($"[RAM] Partes parseadas: {string.Join(" | ", parts)}");
                    
                    if (parts.Length < 5)
                    {
                        _logger.LogDebug($"[RAM] Línea ignorada por longitud ({parts.Length} campos): '{csv}'");
                        continue;
                    }
                    var tipoStr = parts[0].Trim('"'); // SMBIOSMemoryType
                    var capacidadStr = parts[1].Trim('"');
                    var fabricante = parts[2].Trim('"');
                    var modelo = parts[3].Trim('"'); // PartNumber
                    var velocidad = parts[4].Trim('"');
                    _logger.LogDebug($"[RAM] Datos extraídos - Slot: {slotsOcupados+1}, Capacidad: {capacidadStr}, Marca: {fabricante}, Frecuencia: {velocidad}, Modelo: {modelo}, Tipo: {tipoStr}");
                    
                    long capacidadBytes = 0;
                    int capacidadGB = 0;
                    if (long.TryParse(capacidadStr, out capacidadBytes))
                        capacidadGB = (int)(capacidadBytes / (1024 * 1024 * 1024));
                    
                    var tipoMemoriaTraducido = TraducirTipoMemoria(tipoStr);
                    var tipoCombo = ObtenerTipoMemoriaCombo(tipoMemoriaTraducido);
                    
                    var slot = new SlotRamEntity
                    {
                        NumeroSlot = ++slotsOcupados,
                        CapacidadGB = capacidadGB,
                        Marca = fabricante,
                        Frecuencia = velocidad,
                        TipoMemoria = tipoCombo ?? $"Desconocido ({tipoStr})",
                        Ocupado = true,
                        Observaciones = modelo
                    };
                    
                    _logger.LogDebug($"[RAM] Creando slot: {slot.NumeroSlot} - {slot.CapacidadGB}GB {slot.TipoMemoria} {slot.Marca}");
                    Application.Current.Dispatcher.Invoke(() => ListaRam.Add(slot));
                }
                
                string resumen = $"Slots ocupados: {slotsOcupados}";
                _logger.LogDebug($"[RAM] Resumen: {resumen}");
                
                if (ListaRam.Count == 0)
                {
                    _logger.LogWarning("[RAM] No se detectaron módulos de RAM automáticamente. Permitiendo edición manual.");
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show("No se detectaron módulos de RAM automáticamente. Se ha añadido un slot vacío para edición manual.\n" + resumen, "RAM automática", MessageBoxButton.OK, MessageBoxImage.Warning));
                    Application.Current.Dispatcher.Invoke(() => ListaRam.Add(new SlotRamEntity
                    {
                        NumeroSlot = 1,
                        CapacidadGB = null,
                        Marca = string.Empty,
                        Frecuencia = string.Empty,
                        TipoMemoria = string.Empty,
                        Ocupado = false,
                        Observaciones = "Manual"
                    }));
                }
                else
                {
                    _logger.LogInformation($"[RAM] ✅ Detectados {ListaRam.Count} módulos de RAM correctamente");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RAM] Error en la detección con WMI clásico");
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Error al detectar la RAM automáticamente. Se ha añadido un slot vacío para edición manual.", "RAM automática", MessageBoxButton.OK, MessageBoxImage.Error));
                Application.Current.Dispatcher.Invoke(() => ListaRam.Add(new SlotRamEntity
                {
                    NumeroSlot = 1,
                    CapacidadGB = null,
                    Marca = string.Empty,
                    Frecuencia = string.Empty,
                    TipoMemoria = string.Empty,
                    Ocupado = false,
                    Observaciones = "Manual"
                }));
            }
            _logger.LogDebug($"[RAM] Lista final de RAM: {string.Join(", ", ListaRam.Select(r => $"Slot {r.NumeroSlot}: {r.CapacidadGB}GB {r.TipoMemoria} {r.Marca}"))}");
        }

        private string[] ParseCsvLine(string line)
        {
            // Parser robusto para CSV con comillas y comas en campos
            var result = new List<string>();
            bool inQuotes = false;
            int start = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"') inQuotes = !inQuotes;
                else if (line[i] == ',' && !inQuotes)
                {
                    result.Add(line.Substring(start, i - start));
                    start = i + 1;
                }
            }
            result.Add(line.Substring(start));
            return result.Select(s => s.Trim()).ToArray();
        }

        private string TraducirTipoMemoria(string memoryType)
        {
            switch (memoryType)
            {
                case "24": return "DDR3";
                case "26": return "DDR4";
                case "32": return "DDR5";
                case "30": return "LPDDR4";
                case "31": return "LPDDR5";
                default: return string.Empty;
            }
        }
        private string ObtenerTipoMemoriaCombo(string tipoTraducido)
        {
            if (!string.IsNullOrWhiteSpace(tipoTraducido) && TiposRam.Contains(tipoTraducido))
                return tipoTraducido;
            return string.Empty;
        }

        [RelayCommand(CanExecute = nameof(CanObtenerDiscosAutomaticos))]
        public async Task ObtenerDiscosAutomaticosAsync()
        {
            if (IsLoadingDiscos) return;
            IsLoadingDiscos = true;
            try
            {
                await Task.Run(() => ObtenerDiscosAutomaticos());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener los discos: {ex.Message}", "Discos automáticos", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoadingDiscos = false;
            }
        }        
        private void ObtenerDiscosAutomaticos()
        {
            _logger.LogDebug("[DISCOS] Iniciando detección automática de discos");
            Application.Current.Dispatcher.Invoke(() => ListaDiscos.Clear());
            
            try
            {                
                // Usar Get-CimInstance del namespace Storage para información más precisa del tipo de disco
                var output = EjecutarPowerShellCompleto("Get-CimInstance -Namespace root\\Microsoft\\Windows\\Storage -ClassName MSFT_PhysicalDisk | Select-Object DeviceId, MediaType, Size, Manufacturer, Model | ConvertTo-Csv -NoTypeInformation");
                _logger.LogDebug($"[DISCOS] Resultado PowerShell Win32_DiskDrive CSV: '{output}'");
                
                if (string.IsNullOrWhiteSpace(output))
                {
                    _logger.LogWarning("[DISCOS] La salida de PowerShell está vacía");
                    throw new Exception("PowerShell no devolvió ningún resultado para discos");
                }
                
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                _logger.LogDebug($"[DISCOS] Total de líneas obtenidas: {lines.Count}");
                
                if (lines.Count <= 1)
                {
                    _logger.LogWarning("[DISCOS] Solo se obtuvo la cabecera CSV, sin datos de discos");
                    throw new Exception("No hay datos de discos en la salida");
                }
                
                int discosDetectados = 0;
                for (int i = 1; i < lines.Count; i++)
                {
                    var csv = lines[i];
                    _logger.LogDebug($"[DISCOS] Procesando línea CSV {i}: '{csv}'");                    
                    // Usar el parser robusto para CSV
                    var parts = ParseCsvLine(csv);
                    _logger.LogDebug($"[DISCOS] Partes parseadas: {string.Join(" | ", parts)}");
                    
                    if (parts.Length < 5)
                    {
                        _logger.LogDebug($"[DISCOS] Línea ignorada por longitud ({parts.Length} campos): '{csv}'");
                        continue;
                    }
                    
                    var deviceId = parts[0].Trim('"'); // DeviceId
                    var mediaTypeStr = parts[1].Trim('"'); // MediaType (número)
                    var capacidadStr = parts[2].Trim('"'); // Size
                    var fabricante = parts[3].Trim('"'); // Manufacturer
                    var modelo = parts[4].Trim('"'); // Model
                      _logger.LogDebug($"[DISCOS] Datos extraídos - DeviceId: {deviceId}, MediaType: {mediaTypeStr}, Capacidad: {capacidadStr}, Fabricante: {fabricante}, Modelo: {modelo}");
                      long capacidadBytes = 0;
                    int capacidadGB = 0;
                    if (long.TryParse(capacidadStr, out capacidadBytes))
                    {
                        // Usar división decimal usando base 1000 (como los fabricantes) para mostrar capacidades más intuitivas
                        double capacidadGBDecimal = capacidadBytes / (1000.0 * 1000.0 * 1000.0);
                        capacidadGB = (int)Math.Round(capacidadGBDecimal);
                        _logger.LogDebug($"[DISCOS] Capacidad calculada: {capacidadBytes} bytes = {capacidadGBDecimal:F2} GB → {capacidadGB} GB (base 1000)");
                    }
                    
                    var tipoNormalizado = NormalizarTipoDiscoStorage(mediaTypeStr, modelo);
                    
                    var disco = new DiscoEntity
                    {
                        NumeroDisco = ++discosDetectados,
                        Tipo = tipoNormalizado,
                        CapacidadGB = capacidadGB,
                        Marca = fabricante,
                        Modelo = modelo,
                        CodigoEquipo = Codigo
                    };
                    
                    _logger.LogDebug($"[DISCOS] Creando disco: {disco.NumeroDisco} - {disco.CapacidadGB}GB {disco.Tipo} {disco.Marca} {disco.Modelo}");
                    Application.Current.Dispatcher.Invoke(() => ListaDiscos.Add(disco));
                }
                
                string resumen = $"Discos detectados: {discosDetectados}";
                _logger.LogDebug($"[DISCOS] Resumen: {resumen}");
                
                if (ListaDiscos.Count == 0)
                {
                    _logger.LogWarning("[DISCOS] No se detectaron discos automáticamente. Permitiendo edición manual.");
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show("No se detectaron discos automáticamente. Se ha añadido un disco vacío para edición manual.\n" + resumen, "Discos automáticos", MessageBoxButton.OK, MessageBoxImage.Warning));
                    Application.Current.Dispatcher.Invoke(() => ListaDiscos.Add(new DiscoEntity
                    {
                        NumeroDisco = 1,
                        Tipo = "Otro",
                        CapacidadGB = null,
                        Marca = string.Empty,
                        Modelo = string.Empty,
                        CodigoEquipo = Codigo
                    }));
                }
                else
                {
                    _logger.LogInformation($"[DISCOS] ✅ Detectados {ListaDiscos.Count} discos correctamente");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DISCOS] Error en la detección automática de discos");
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Error al detectar los discos automáticamente. Se ha añadido un disco vacío para edición manual.", "Discos automáticos", MessageBoxButton.OK, MessageBoxImage.Error));
                Application.Current.Dispatcher.Invoke(() => ListaDiscos.Add(new DiscoEntity
                {
                    NumeroDisco = 1,
                    Tipo = "Otro",
                    CapacidadGB = null,
                    Marca = string.Empty,
                    Modelo = string.Empty,
                    CodigoEquipo = Codigo
                }));
            }
            
            _logger.LogDebug($"[DISCOS] Lista final de discos: {string.Join(", ", ListaDiscos.Select(d => $"Disco {d.NumeroDisco}: {d.CapacidadGB}GB {d.Tipo} {d.Marca} {d.Modelo}"))}");
        }        
        private string NormalizarTipoDiscoStorage(string mediaTypeStr, string modelo = "")
        {
            _logger.LogDebug($"[DISCOS] Normalizando tipo Storage - MediaType: '{mediaTypeStr}', Modelo: '{modelo}'");
            
            // Convertir MediaType string a número
            if (!int.TryParse(mediaTypeStr, out int mediaType))
            {
                _logger.LogDebug($"[DISCOS] MediaType no es numérico: '{mediaTypeStr}', usando fallback por modelo");
                return NormalizarTipoDiscoPorModelo(modelo);
            }
            
            // Mapeo según Microsoft Storage MediaType values
            // https://docs.microsoft.com/en-us/windows/win32/api/winioctl/ns-winioctl-storage_media_type
            return mediaType switch
            {
                0 => "Otro",        // Unknown
                3 => "HDD",         // FixedMedia (traditional HDD)
                4 => "SSD",         // SSD
                5 => "Otro",        // SCM (Storage Class Memory)
                _ => "Otro"         // Otros valores no definidos
            };
        }
        
        private string NormalizarTipoDiscoPorModelo(string modelo)
        {
            if (string.IsNullOrWhiteSpace(modelo)) return "Otro";
            
            var modeloLower = modelo.ToLower().Trim();
            
            // Detectar NVMe primero (más específico)
            if (modeloLower.Contains("nvme") || modeloLower.Contains("m.2"))
                return "NVMe";
            
            // Detectar SSD
            if (modeloLower.Contains("ssd") || modeloLower.Contains("solid state"))
                return "SSD";
            
            // Detectar HDD
            if (modeloLower.Contains("hdd") || modeloLower.Contains("seagate") || 
                modeloLower.Contains("western digital") || modeloLower.Contains("wd"))
                return "HDD";
            
            // Detectar eMMC
            if (modeloLower.Contains("emmc") || modeloLower.Contains("embedded"))
                return "eMMC";
            
            return "Otro";
        }

        [RelayCommand(CanExecute = nameof(CanAgregarDiscoManual))]
        public void AgregarDiscoManual()
        {
            var disco = new DiscoEntity
            {
                NumeroDisco = ListaDiscos.Count + 1,
                Tipo = "SSD",
                CapacidadGB = 256,
                Marca = string.Empty,
                Modelo = string.Empty,
                CodigoEquipo = Codigo
            };
            ListaDiscos.Add(disco);
        }

        [RelayCommand(CanExecute = nameof(CanEliminarDisco))]
        public void EliminarDisco(DiscoEntity disco)
        {
            if (ListaDiscos.Contains(disco))
                ListaDiscos.Remove(disco);
        }

        [RelayCommand]
        public void AgregarRam()
        {
            if (!_currentUser.HasPermission("GestionEquipos.GestionarRam")) return;
            
            var slot = new SlotRamEntity
            {
                NumeroSlot = ListaRam.Count + 1,
                CapacidadGB = 8,
                Marca = string.Empty,
                Frecuencia = string.Empty,
                TipoMemoria = "DDR4",
                Ocupado = true,
                Observaciones = string.Empty,
                CodigoEquipo = Codigo
            };
            ListaRam.Add(slot);
        }

        [RelayCommand]
        public void EliminarRam(SlotRamEntity slot)
        {
            if (!_currentUser.HasPermission("GestionEquipos.GestionarRam")) return;
            
            if (ListaRam.Contains(slot))
                ListaRam.Remove(slot);
        }        
        public async Task CargarPersonasDisponiblesAsync()
        {
            try
            {
                var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
                var personaService = serviceProvider.GetService(typeof(IPersonaService)) as IPersonaService;
                if (personaService == null)
                {
                    PersonasDisponibles = new ObservableCollection<Persona>();
                    return;
                }
                var personas = await personaService.BuscarPersonasAsync("");
                var personasActivas = personas.Where(p => p.Activo).ToList();
                Application.Current.Dispatcher.Invoke(() => {
                    PersonasDisponibles = new ObservableCollection<Persona>(personasActivas);
                });
                // Actualizar filtro después de cargar
                ActualizarFiltroPersonas();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error cargando personas disponibles: {Message}", ex.Message);
                Application.Current.Dispatcher.Invoke(() => {
                    PersonasDisponibles = new ObservableCollection<Persona>();
                    ActualizarFiltroPersonas();
                });
            }
        }

        private void ActualizarFiltroPersonas()
        {
            if (PersonasDisponibles == null || PersonasDisponibles.Count == 0)
            {
                PersonasFiltradas = new ObservableCollection<Persona>();
                return;
            }
            if (string.IsNullOrWhiteSpace(FiltroPersonaAsignada))
            {
                PersonasFiltradas = new ObservableCollection<Persona>(PersonasDisponibles);
            }
            else
            {
                var filtro = FiltroPersonaAsignada.Trim().ToLowerInvariant();
                var filtradas = PersonasDisponibles.Where(p => p.NombreCompleto.ToLowerInvariant().Contains(filtro)).ToList();
                PersonasFiltradas = new ObservableCollection<Persona>(filtradas);
            }
        }

        partial void OnFiltroPersonaAsignadaChanged(string value)
        {
            ActualizarFiltroPersonas();
        }

        partial void OnPersonasDisponiblesChanged(ObservableCollection<Persona> value)
        {
            ActualizarFiltroPersonas();
        }

        [RelayCommand]
        public async Task InicializarPersonasAsignadasAsync()
        {
            await CargarPersonasDisponiblesAsync();
        }

        // Eliminar referencias a propiedades eliminadas de la entidad principal
        // public int? SlotsTotales { get; set; }
        // public int? SlotsUtilizados { get; set; }
        // public string? TipoRam { get; set; }
        // public int? CapacidadTotalRamGB { get; set; }
        // public int? CantidadDiscos { get; set; }
        // public int? CapacidadTotalDiscosGB { get; set; }
        // Si se requiere mostrar totales, calcularlos desde ListaRam y ListaDiscos
    }
}
