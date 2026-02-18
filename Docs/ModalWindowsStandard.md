# Estándar para Ventanas Modales (Diálogos)

Guía rápida para crear ventanas modales consistentes en GestLog.

## Estructura XAML

```xaml
<Window x:Class="GestLog.Modules.[Modulo].Views.[Carpeta].MiDialogView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:conv="clr-namespace:GestLog.Converters"
        Title="Título del Diálogo" 
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        SizeToContent="Manual"
        WindowStartupLocation="CenterOwner"
        ShowInTaskbar="False"
        UseLayoutRounding="True"
        SnapsToDevicePixels="True"
        TextOptions.TextFormattingMode="Display"
        TextOptions.TextRenderingMode="ClearType"
        TextOptions.TextHintingMode="Fixed">

    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="/Modules/GestionEquiposInformaticos/Views/Equipos/ModalWindowsStandard.xaml" />
            </ResourceDictionary.MergedDictionaries>
            <conv:BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter" />
            <conv:InverseBooleanToVisibilityConverter x:Key="InverseBooleanToVisibilityConverter" />
            <conv:InverseBooleanConverter x:Key="InverseBooleanConverter" />
            <conv:NullToVisibilityConverter x:Key="NullToVisibilityConverter" />
        </ResourceDictionary>
    </Window.Resources>

    <!-- OVERLAY MODAL - Fondo oscuro semitransparente -->
    <Grid x:Name="RootGrid" Background="#80000000" MouseLeftButtonDown="Overlay_MouseLeftButtonDown" 
          UseLayoutRounding="True" SnapsToDevicePixels="True">
        
        <!-- CARD CENTRADA -->
        <Border x:Name="Card" Width="750" MaxHeight="700"
                Background="{StaticResource SurfaceBrush}"
                CornerRadius="8" Padding="0"
                HorizontalAlignment="Center" VerticalAlignment="Center"
                BorderThickness="1" BorderBrush="{StaticResource BorderBrush}"
                Effect="{StaticResource WindowShadow}" MouseLeftButtonDown="Panel_MouseLeftButtonDown">
            
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>

                <!-- HEADER - Barra superior con gradiente -->
                <Border Grid.Row="0" Background="{StaticResource PrimaryBrush}" CornerRadius="8,8,0,0" 
                        Padding="24,16" Effect="{StaticResource HeaderShadow}">
                    <DockPanel>
                        <!-- Título e ícono a la izquierda -->
                        <StackPanel DockPanel.Dock="Left" Orientation="Horizontal" VerticalAlignment="Center">
                            <Border Background="White" CornerRadius="6" Padding="6" Margin="0,0,16,0" 
                                    Width="36" Height="36" Effect="{StaticResource SectionShadow}">
                                <TextBlock Text="🔧" FontSize="16" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                            </Border>
                            <StackPanel VerticalAlignment="Center">
                                <TextBlock Text="Título del Diálogo" Style="{StaticResource HeaderTextStyle}"/>
                                <TextBlock Text="Descripción breve" Style="{StaticResource SubHeaderTextStyle}" Margin="0,3,0,0"/>
                            </StackPanel>
                        </StackPanel>
                        
                        <!-- Botón Cerrar X a la derecha -->
                        <Button DockPanel.Dock="Right" Style="{StaticResource CloseButton}" 
                                Click="CancelarButton_Click" ToolTip="Cerrar (Esc)">
                            <Grid Width="14" Height="14">
                                <Line X1="0" Y1="0" X2="14" Y2="14" Stroke="White" StrokeThickness="2" 
                                      StrokeStartLineCap="Round" StrokeEndLineCap="Round"/>
                                <Line X1="14" Y1="0" X2="0" Y2="14" Stroke="White" StrokeThickness="2" 
                                      StrokeStartLineCap="Round" StrokeEndLineCap="Round"/>
                            </Grid>
                        </Button>
                    </DockPanel>
                </Border>

                <!-- CONTENIDO - Scrolleable -->
                <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto" Padding="24,20" 
                              Background="{StaticResource LightGrayBrush}">
                    <StackPanel>
                        <!-- [AQUÍ VA TU CONTENIDO - CAMPOS DE FORMULARIO] -->
                    </StackPanel>
                </ScrollViewer>

                <!-- FOOTER - Botones de acción -->
                <Border Grid.Row="2" Background="#F5F5F5" BorderBrush="#E0E0E0" BorderThickness="0,1,0,0" 
                        CornerRadius="0,0,8,8" Padding="24,16">
                    <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                        <Button Content="Cancelar" Width="120" Height="36"
                                Background="#EEEEEE" Foreground="{StaticResource TextPrimaryBrush}"
                                BorderThickness="1" BorderBrush="{StaticResource BorderBrush}"
                                FontWeight="SemiBold" Click="CancelarButton_Click"/>
                        <Button Content="Guardar" Width="120" Height="36" Margin="12,0,0,0"
                                Background="{StaticResource PrimaryBrush}" Foreground="White"
                                BorderThickness="0" FontWeight="SemiBold" FontSize="13"
                                Command="{Binding GuardarCommand}"/>
                    </StackPanel>
                </Border>
            </Grid>
        </Border>
    </Grid>
</Window>
```

## Code-Behind (C#)

```csharp
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using GestLog.Modules.[Modulo].ViewModels.[Carpeta];
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Modules.[Modulo].Views.[Carpeta]
{
    public partial class MiDialogView : Window
    {
        public [VIEWMODEL] ViewModel { get; private set; }

        public MiDialogView()
        {
            InitializeComponent();

            // Obtener ViewModel desde DI
            var app = (App)System.Windows.Application.Current;
            var viewModel = app.ServiceProvider?.GetRequiredService<[VIEWMODEL]>();
            
            if (viewModel == null)
                throw new InvalidOperationException($"No se pudo obtener [VIEWMODEL]");

            ViewModel = viewModel;
            DataContext = ViewModel;

            // Suscribirse al evento de éxito para cerrar automáticamente
            ViewModel.OnExito += (s, e) =>
            {
                DialogResult = true;
                Close();
            };

            // Manejar Escape para cerrar
            this.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    DialogResult = false;
                    Close();
                }
            };

            // Asegura que el overlay cubra toda la pantalla del owner
            var ownerWindow = Application.Current?.MainWindow;
            if (ownerWindow != null)
                ConfigurarParaVentanaPadre(ownerWindow);
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Cerrar al hacer clic en el overlay oscuro (solo RootGrid)
            if (sender is Grid grid && grid.Name == "RootGrid")
            {
                e.Handled = true;
                DialogResult = false;
                Close();
            }
        }

        private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Configura la ventana como modal maximizado sobre una ventana padre
        /// </summary>
        public void ConfigurarParaVentanaPadre(System.Windows.Window? parentWindow)
        {
            if (parentWindow == null) return;
            
            this.Owner = parentWindow;
            this.ShowInTaskbar = false;
            this.WindowState = WindowState.Maximized;
            
            // Mantener maximizado si el owner se mueve/redimensiona
            this.Loaded += (s, e) =>
            {
                if (this.Owner != null)
                {
                    this.Owner.LocationChanged += (s2, e2) => 
                    {
                        if (this.WindowState != WindowState.Maximized)
                            this.WindowState = WindowState.Maximized;
                    };
                    this.Owner.SizeChanged += (s2, e2) => 
                    {
                        if (this.WindowState != WindowState.Maximized)
                            this.WindowState = WindowState.Maximized;
                    };
                }
            };
        }
    }
}
```

## ViewModel

En tu ViewModel, agrega este evento:

```csharp
public event EventHandler? OnExito;

// En el comando de guardar exitoso:
protected virtual void AlGuardarExitoso()
{
    OnExito?.Invoke(this, EventArgs.Empty);
}
```

## Uso desde ViewModel Principal

```csharp
[RelayCommand]
public async Task AbrirDialogoAsync()
{
    var dialog = new MiDialogView();
    var ownerWindow = System.Windows.Application.Current?.MainWindow;

    if (ownerWindow != null)
    {
        dialog.ConfigurarParaVentanaPadre(ownerWindow);
    }

    if (dialog.ShowDialog() == true)
    {
        // Hacer lo que corresponda después del éxito
        await RecargarDatos();
    }
}
```

## Checklist Rápido

- ✅ XAML: Grid overlay (#80000000) → Border card → Header/Contenido/Footer
- ✅ Referenciar `ModalWindowsStandard.xaml` en Window.Resources
- ✅ Code-Behind: Constructor sin argumentos, obtener ViewModel desde DI
- ✅ Handlers: `Overlay_MouseLeftButtonDown`, `Panel_MouseLeftButtonDown`, `KeyDown` para Escape
- ✅ Método: `ConfigurarParaVentanaPadre(owner)` para maximizar
- ✅ Evento: `OnExito` en ViewModel que dispare `DialogResult = true`
- ✅ Comando: `GuardarCommand` en ViewModel que dispare `OnExito`

## Nota: Icono del header (cuadrado blanco)

Para conseguir la apariencia consistente del sistema (ícono dentro de un cuadrado blanco redondeado) use siempre un `Border` con fondo blanco, `CornerRadius` y sin borde visible. Importante: fijar `BorderThickness="0"` y `BorderBrush="Transparent"` evita que en algunas configuraciones de DPI o subpixel aparezca un borde blanco fino alrededor del card.

Recomendación XAML (copiar en el header de los modales):

```xaml
<Border Background="White" CornerRadius="8" Padding="8" Margin="0,0,16,0"
        Width="44" Height="44" BorderThickness="0" BorderBrush="Transparent"
        Effect="{StaticResource SectionShadow}">
    <TextBlock Text="📄" FontSize="18" HorizontalAlignment="Center" VerticalAlignment="Center"/>
</Border>
```

Notas adicionales:

- Use `{StaticResource SectionShadow}` o la key de sombra definida en `ModalWindowsStandard.xaml` para mantener la consistencia; los convertidores y sombras deben definirse en un ResourceDictionary cargado desde `App.xaml` para poder usar `{StaticResource ...}` de forma segura.
- Si desea que el icono tenga fondo del color primario en algunos modales, reemplace `Background="White"` por `Background="{StaticResource PrimaryBrush}"` y ajuste `TextBlock.Foreground` en consecuencia.
- No confunda este `Border` del icono con el `Border` principal del `Card`. El `Card` debe tener `BorderThickness="0"` y `BorderBrush="Transparent"` si no desea líneas alrededor de la tarjeta.

## Notas Importantes

- **Window.Resources**: Siempre mergear `ModalWindowsStandard.xaml`
- **Overlay**: Usar `#80000000` (50% negro) para consistencia
- **Maximizar**: Usar `WindowState.Maximized` en `ConfigurarParaVentanaPadre()` - evita problemas de DPI y pantallas múltiples
- **Eventos**: `OnExito` debe dispararse en el ViewModel cuando el guardado sea exitoso
- **Cierre por Overlay**: Validar que `RootGrid` sea el Name del Grid raíz para evitar cierres accidentales
- **DI**: El ViewModel se obtiene desde `app.ServiceProvider.GetRequiredService<[VIEWMODEL]>()` - debe estar registrado en `Startup.UsuariosPersonas.cs`

- **Nota añadida (Comportamiento recomendado)**: si el constructor de la ventana obtiene el ViewModel desde el contenedor DI, es recomendable llamar automáticamente a `ConfigurarParaVentanaPadre(Application.Current?.MainWindow)` justo después de asignar el `DataContext`. Esto garantiza que la ventana modal se maximice sobre la ventana padre y que el overlay oscuro (`RootGrid`) cubra toda la pantalla — evita el problema de overlay pequeño centrado.

  Ejemplo corto en el constructor (C#):

  ```csharp
  InitializeComponent();
  var app = (App)Application.Current;
  var vm = app.ServiceProvider?.GetService<MyDialogViewModel>();
  if (vm != null)
  {
      DataContext = vm;
      vm.Owner = this;

      // Asegura que el overlay cubra toda la pantalla del owner
      var ownerWindow = Application.Current?.MainWindow;
      if (ownerWindow != null)
          ConfigurarParaVentanaPadre(ownerWindow);
  }
  ```

  - Razón: algunos diálogos se abren usando el constructor vacío que resuelve el ViewModel desde DI; al maximizar la ventana desde el código constructor se evita que el Grid overlay quede limitado al tamaño de la tarjeta (card) central.
  - Alternativa: el llamador puede usar `dialog.ConfigurarParaVentanaPadre(owner)` antes de `ShowDialog()` si prefiere control explícito.

---

**Última actualización**: 2025-12-15  
**Ejemplos**: `RegistroMantenimientoCorrectivoDialog`, `CompletarCancelarMantenimientoDialog`, `DetallesEquipoInformaticoView`
