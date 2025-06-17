using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using GestLog.ViewModels;

namespace GestLog.Views;

/// <summary>
/// Lógica de interacción para FirstRunSetupDialog.xaml
/// Dialog para la configuración automática inicial de la aplicación
/// </summary>
public partial class FirstRunSetupDialog : Window
{    /// <summary>
    /// ViewModel para el setup automático
    /// </summary>
    public FirstRunSetupViewModel? ViewModel { get; private set; }

    /// <summary>
    /// Constructor por defecto requerido para XAML
    /// </summary>
    public FirstRunSetupDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Constructor que inicializa el dialog con el ViewModel
    /// </summary>
    /// <param name="viewModel">ViewModel inyectado</param>
    public FirstRunSetupDialog(FirstRunSetupViewModel viewModel) : this()
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;
        
        // Configurar el dialog para modo automático
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        
        // Suscribirse a eventos del ViewModel
        ViewModel.SetupCompleted += OnSetupCompleted;
        
        // Manejar eventos de la ventana
        Loaded += Window_Loaded;
    }

    /// <summary>
    /// Maneja el evento de configuración completada
    /// </summary>
    private void OnSetupCompleted(object? sender, bool isSuccess)
    {
        DialogResult = isSuccess;
        Close();
    }

    /// <summary>
    /// Maneja el evento de carga de la ventana
    /// </summary>
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // En modo automático, iniciar la configuración inmediatamente
        if (ViewModel != null)
        {
            // Si hay comandos automáticos, ejecutarlos
            if (ViewModel.ConfigureCommand?.CanExecute(null) == true)
            {
                ViewModel.ConfigureCommand.Execute(null);
            }
        }
    }

    /// <summary>
    /// Limpieza de recursos cuando se cierra el dialog
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        // Desuscribirse de eventos para evitar memory leaks
        if (ViewModel != null)
        {
            ViewModel.SetupCompleted -= OnSetupCompleted;
        }
        
        base.OnClosed(e);
    }

    /// <summary>
    /// Factory method para crear una instancia del dialog con DI
    /// </summary>
    /// <param name="serviceProvider">Proveedor de servicios</param>
    /// <returns>Nueva instancia del dialog</returns>
    public static FirstRunSetupDialog Create(IServiceProvider serviceProvider)
    {
        var viewModel = serviceProvider.GetRequiredService<FirstRunSetupViewModel>();
        return new FirstRunSetupDialog(viewModel);
    }
}
