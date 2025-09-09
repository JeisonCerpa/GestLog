using System.Windows;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.ViewModels.Tools.GestionEquipos;
using GestLog.Modules.DatabaseConnection;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace GestLog.Views.Tools.GestionEquipos
{
    public partial class DetallesEquipoInformaticoView : Window
    {
        public DetallesEquipoInformaticoView(EquipoInformaticoEntity equipo)
        {
            InitializeComponent();

            // Intentar obtener GestLogDbContext desde Application.Current.Properties si fue registrado
            GestLogDbContext? db = null;
            try
            {
                if (System.Windows.Application.Current?.Properties.Contains("DbContext") == true)
                    db = System.Windows.Application.Current.Properties["DbContext"] as GestLogDbContext;
            }
            catch { }

            // Pasar null si no se encuentra; ViewModel maneja null
            DataContext = new DetallesEquipoInformaticoViewModel(equipo, db);
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Abrir la ventana de AgregarEquipoInformaticoView y precargar datos para edición
                var editarWindow = new AgregarEquipoInformaticoView();
                if (editarWindow.DataContext is GestLog.ViewModels.Tools.GestionEquipos.AgregarEquipoInformaticoViewModel vm)
                {
                    string? usuarioAsignadoOriginal = null;
                    // Mapear los campos principales desde el ViewModel de Detalles (usar una única variable `det`)
                    if (this.DataContext is GestLog.ViewModels.Tools.GestionEquipos.DetallesEquipoInformaticoViewModel det)
                    {
                        vm.Codigo = det.Codigo;
                        vm.NombreEquipo = det.NombreEquipo ?? string.Empty;
                        vm.Marca = det.Marca ?? string.Empty;
                        vm.Modelo = det.Modelo ?? string.Empty;
                        vm.Procesador = det.Procesador ?? string.Empty;
                        vm.So = det.SO ?? string.Empty;
                        vm.SerialNumber = det.SerialNumber ?? string.Empty;
                        vm.CodigoAnydesk = det.CodigoAnydesk ?? string.Empty;
                        vm.Observaciones = det.Observaciones ?? string.Empty;
                        vm.Costo = det.Costo;
                        vm.FechaCompra = det.FechaCompra;

                        // Marcar modo edición y guardar código original para identificación en BD
                        vm.IsEditing = true;
                        vm.OriginalCodigo = det.Codigo;

                        // Cargar listas de RAM y discos
                        vm.ListaRam = new System.Collections.ObjectModel.ObservableCollection<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.SlotRamEntity>(det.SlotsRam);
                        vm.ListaDiscos = new System.Collections.ObjectModel.ObservableCollection<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.DiscoEntity>(det.Discos);

                        // Guardar el nombre del usuario asignado para usarlo una vez inicializado el VM de edición
                        usuarioAsignadoOriginal = det.UsuarioAsignado;
                    }

                    // Inicializar VM (carga de personas u otros recursos) antes de mostrar la ventana
                    try { await vm.InicializarAsync(); } catch { /* no crítico si falla aquí */ }

                    // Seleccionar la persona asignada usando el nombre capturado (si aplica)
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(usuarioAsignadoOriginal))
                        {
                            // Si no hay lista de personas cargada, dejar el texto en el filtro para que el usuario lo vea
                            if (vm.PersonasDisponibles == null || !vm.PersonasDisponibles.Any())
                            {
                                vm.FiltroPersonaAsignada = usuarioAsignadoOriginal.Trim();
                            }
                            else
                            {
                                // Normalizar función para comparar sin acentos ni mayúsculas
                                static string NormalizeString(string s)
                                {
                                    if (string.IsNullOrWhiteSpace(s))
                                        return string.Empty;
                                    var normalized = s.Normalize(System.Text.NormalizationForm.FormD);
                                    var chars = normalized.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray();
                                    return new string(chars).Normalize(System.Text.NormalizationForm.FormC).Trim().ToLowerInvariant();
                                }

                                var objetivo = NormalizeString(usuarioAsignadoOriginal);

                                // Intentos de emparejamiento: exacto normalizado, contains, y como último recurso dejar el filtro con el texto
                                var persona = vm.PersonasDisponibles.FirstOrDefault(p => NormalizeString(p.NombreCompleto) == objetivo);
                                if (persona == null)
                                    persona = vm.PersonasDisponibles.FirstOrDefault(p => NormalizeString(p.NombreCompleto).Contains(objetivo) || objetivo.Contains(NormalizeString(p.NombreCompleto)));

                                if (persona != null)
                                    vm.PersonaAsignada = persona;
                                else
                                    vm.FiltroPersonaAsignada = usuarioAsignadoOriginal.Trim();
                            }
                        }
                    }
                    catch { /* No crítico */ }
                }

                var result = editarWindow.ShowDialog();
                // Si se guardó (DialogResult == true), refrescar la vista de detalles
                if (result == true)
                {
                    try
                    {
                        // Intentar obtener un DbContext compartido desde Application.Current.Properties
                        GestLogDbContext? db = null;
                        try
                        {
                            if (System.Windows.Application.Current?.Properties.Contains("DbContext") == true)
                                db = System.Windows.Application.Current.Properties["DbContext"] as GestLogDbContext;
                        }
                        catch { }

                        // Determinar código del equipo (puede haber cambiado durante la edición)
                        string? codigoParaCargar = null;
                        if (editarWindow.DataContext is GestLog.ViewModels.Tools.GestionEquipos.AgregarEquipoInformaticoViewModel vm2)
                            codigoParaCargar = vm2.Codigo;

                        if (!string.IsNullOrWhiteSpace(codigoParaCargar) && db != null)
                        {
                            // Recargar desde BD incluyendo colecciones
                            var equipoRef = db.EquiposInformaticos
                                .Include(e => e.SlotsRam)
                                .Include(e => e.Discos)
                                .FirstOrDefault(e => e.Codigo == codigoParaCargar);                            if (equipoRef != null)
                            {
                                // Reasignar DataContext con la entidad recargada
                                this.DataContext = new DetallesEquipoInformaticoViewModel(equipoRef, db);
                                return;
                            }
                        }

                        // Fallback: si no hay DbContext compartido o no se encontró en BD, reconstruir entidad desde el VM de edición
                        if (editarWindow.DataContext is GestLog.ViewModels.Tools.GestionEquipos.AgregarEquipoInformaticoViewModel vmFallback)
                        {
                            var equipoReconstruido = new EquipoInformaticoEntity
                            {
                                Codigo = vmFallback.Codigo,
                                UsuarioAsignado = vmFallback.PersonaAsignada?.NombreCompleto ?? string.Empty,
                                NombreEquipo = vmFallback.NombreEquipo,
                                Sede = vmFallback.Sede,
                                Marca = vmFallback.Marca,
                                Modelo = vmFallback.Modelo,
                                Procesador = vmFallback.Procesador,
                                SO = vmFallback.So,
                                SerialNumber = vmFallback.SerialNumber,
                                Observaciones = vmFallback.Observaciones,
                                FechaCreacion = DateTime.Now,
                                FechaCompra = vmFallback.FechaCompra,
                                Costo = vmFallback.Costo,
                                CodigoAnydesk = vmFallback.CodigoAnydesk,
                                Estado = vmFallback.Estado,
                                SlotsRam = vmFallback.ListaRam?.ToList() ?? new System.Collections.Generic.List<Modules.GestionEquiposInformaticos.Models.Entities.SlotRamEntity>(),
                                Discos = vmFallback.ListaDiscos?.ToList() ?? new System.Collections.Generic.List<Modules.GestionEquiposInformaticos.Models.Entities.DiscoEntity>()
                            };                            // Asignar nuevo DataContext usando el equipo reconstruido (sin DbContext)
                            this.DataContext = new DetallesEquipoInformaticoViewModel(equipoReconstruido, null);
                        }
                    }
                    catch
                    {
                        // No crítico: si falla el refresco, mantenemos la vista actual
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al abrir el editor: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
