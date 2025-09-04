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

        [ObservableProperty]
        private string estado = "Activo";

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

        public void Inicializar()
        {
            try
            {
                var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<GestLogDbContext>()
                    .UseSqlServer(GetProductionConnectionString())
                    .Options;
                using var dbContext = new GestLogDbContext(options);
                var personas = dbContext.Personas.Where(p => p.Activo).ToList();
                PersonasDisponibles = new ObservableCollection<Persona>(personas);
                PersonasFiltradas = new ObservableCollection<Persona>(personas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar las personas para usuario asignado");
                MessageBox.Show("No se pudieron cargar los usuarios disponibles.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetProductionConnectionString()
        {
            // Configuración de conexión de producción
            return "Server=SIMICSGROUPWKS1\\SIMICSBD;Database=BD_ Pruebas;User Id=sa;Password=REMOVED_SECRET;TrustServerCertificate=True;Connection Timeout=30;";
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
                .UseSqlServer(GetProductionConnectionString())
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
                Estado = Estado
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
        [RelayCommand(CanExecute = nameof(CanGuardarEquipo))]
        public async Task GuardarAsync()
        {
            await GuardarEquipoAsync();
            // Cerrar la ventana después de guardar
            if (Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this) is Window win)
                win.DialogResult = true;
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
                await ObtenerDiscosAutomaticosAsync();                
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
                var output = EjecutarPowerShellCompleto("Get-WmiObject Win32_DiskDrive | Select-Object Model, Size, InterfaceType, Manufacturer | ConvertTo-Csv -NoTypeInformation");
                _logger.LogDebug($"[DISCOS] Resultado PowerShell Win32_DiskDrive CSV: '{output}'");
                if (string.IsNullOrWhiteSpace(output))
                {
                    _logger.LogWarning("[DISCOS] La salida de PowerShell está vacía");
                    throw new Exception("PowerShell no devolvió ningún resultado");
                }
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                _logger.LogDebug($"[DISCOS] Total de líneas obtenidas: {lines.Count}");
                if (lines.Count <= 1)
                {
                    _logger.LogWarning("[DISCOS] Solo se obtuvo la cabecera CSV, sin datos de discos");
                    throw new Exception("No hay datos de discos en la salida");
                }
                int discoNum = 1;
                for (int i = 1; i < lines.Count; i++)
                {
                    var csv = lines[i];
                    var parts = ParseCsvLine(csv);
                    if (parts.Length < 4) continue;
                    var modelo = parts[0].Trim('"');
                    var sizeStr = parts[1].Trim('"');
                    var interfaz = parts[2].Trim('"');
                    var fabricante = parts[3].Trim('"');
                    long sizeBytes = 0;
                    int capacidadGB = 0;
                    if (long.TryParse(sizeStr, out sizeBytes))
                    {
                        // Cálculo comercial: dividir por 1,000,000,000 y redondear
                        capacidadGB = (int)Math.Round(sizeBytes / 1_000_000_000.0);
                        // Si la capacidad está entre 480 y 512, mostrar 512
                        if (capacidadGB >= 480 && capacidadGB < 520) capacidadGB = 512;
                    }
                    // Inferir tipo
                    string tipo = "HDD";
                    if (modelo.ToUpper().Contains("NVME")) tipo = "NVMe";
                    else if (modelo.ToUpper().Contains("SSD")) tipo = "SSD";
                    else if (modelo.ToUpper().Contains("EMMC")) tipo = "eMMC";
                    else if (interfaz == "SSD" || interfaz == "NVMe" || interfaz == "eMMC") tipo = interfaz;
                    // Extraer marca del modelo
                    string marca = fabricante;
                    var palabras = modelo.Split(' ');
                    foreach (var palabra in palabras)
                    {
                        if (palabra.ToUpper().Contains("MICRON") || palabra.ToUpper().Contains("SAMSUNG") || palabra.ToUpper().Contains("KINGSTON") || palabra.ToUpper().Contains("SEAGATE") || palabra.ToUpper().Contains("WD") || palabra.ToUpper().Contains("TOSHIBA") || palabra.ToUpper().Contains("CRUCIAL"))
                        {
                            marca = palabra;
                            break;
                        }
                    }
                    if (string.IsNullOrWhiteSpace(marca) || marca == "(Unidades de disco estándar)") marca = modelo;
                    var disco = new DiscoEntity
                    {
                        Tipo = tipo,
                        CapacidadGB = capacidadGB,
                        Marca = marca,
                        Modelo = modelo
                    };
                    Application.Current.Dispatcher.Invoke(() => ListaDiscos.Add(disco));
                    discoNum++;
                }
                if (ListaDiscos.Count == 0)
                {
                    _logger.LogWarning("[DISCOS] No se detectaron discos automáticamente. Permitiendo edición manual.");
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show("No se detectaron discos automáticamente. Se ha añadido un disco vacío para edición manual.", "Discos automáticos", MessageBoxButton.OK, MessageBoxImage.Warning));
                    Application.Current.Dispatcher.Invoke(() => ListaDiscos.Add(new DiscoEntity
                    {
                        Tipo = "HDD",
                        CapacidadGB = null,
                        Marca = string.Empty,
                        Modelo = string.Empty
                    }));
                }
                else
                {
                    _logger.LogInformation($"[DISCOS] ✅ Detectados {ListaDiscos.Count} discos correctamente");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DISCOS] Error en la detección automática");
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Error al detectar los discos automáticamente. Se ha añadido un disco vacío para edición manual.", "Discos automáticos", MessageBoxButton.OK, MessageBoxImage.Error));
                Application.Current.Dispatcher.Invoke(() => ListaDiscos.Add(new DiscoEntity
                {
                    Tipo = "HDD",
                    CapacidadGB = null,
                    Marca = string.Empty,
                    Modelo = string.Empty
                }));
            }
            _logger.LogDebug($"[DISCOS] Lista final de discos: {string.Join(", ", ListaDiscos.Select(d => $"{d.Tipo} {d.CapacidadGB}GB {d.Marca} {d.Modelo}"))}");
        }

        [RelayCommand(CanExecute = nameof(CanAgregarDiscoManual))]
        public void AgregarDiscoManual()
        {
            var nuevoDisco = new DiscoEntity
            {
                Tipo = "SSD",
                CapacidadGB = 256,
                Marca = string.Empty,
                Modelo = string.Empty
            };
            ListaDiscos.Add(nuevoDisco);
        }
    }
}
