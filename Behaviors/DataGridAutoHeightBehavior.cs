using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace GestLog.Behaviors
{
    /// <summary>
    /// Comportamiento que ajusta automáticamente la altura del DataGrid
    /// basado en el espacio disponible en la pantalla.
    /// El scroll aparece cuando la ventana está a 5px del borde de la pantalla.
    /// </summary>
    public class DataGridAutoHeightBehavior : Behavior<DataGrid>
    {
        private Window? _window;
        private const double MarginScreenBottom = 5; // Margen desde el borde inferior de la pantalla

        protected override void OnAttached()
        {
            base.OnAttached();
            
            AssociatedObject.Loaded += DataGrid_Loaded;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            
            if (AssociatedObject != null)
            {
                AssociatedObject.Loaded -= DataGrid_Loaded;
            }

            if (_window != null)
            {
                _window.SizeChanged -= Window_SizeChanged;
                _window.LocationChanged -= Window_LocationChanged;
            }
        }

        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            _window = Window.GetWindow(AssociatedObject);
            if (_window != null)
            {
                _window.SizeChanged += Window_SizeChanged;
                _window.LocationChanged += Window_LocationChanged;
                AdjustHeight();
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustHeight();
        }

        private void Window_LocationChanged(object? sender, System.EventArgs e)
        {
            AdjustHeight();
        }        private void AdjustHeight()
        {
            if (_window == null || AssociatedObject == null)
                return;

            // Obtener la altura total de la pantalla
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            
            // Obtener la posición Y de la ventana (distancia desde el top de la pantalla)
            var windowTop = _window.Top;
            
            // Obtener la altura actual de la ventana
            var windowHeight = _window.ActualHeight;
            
            // Calcular cuánto espacio habría si la ventana creciera más
            var bottomPosition = windowTop + windowHeight;            // Si la ventana está a 5px del borde inferior, limitar el crecimiento
            var spaceToBottom = screenHeight - bottomPosition;
              if (spaceToBottom < MarginScreenBottom)
            {
                // Estamos cerca del borde, ajustar la altura del DataGrid
                // Necesitamos calcular cuánto espacio ocupan los otros elementos
                // El ScrollViewer del diálogo permite scroll para todo, entonces solo limitamos el DataGrid a ~150px de margen
                var fixedElementsHeight = 150; // Reducido porque el diálogo tiene su propio scroll
                
                var maxGridHeight = windowHeight - fixedElementsHeight;
                
                if (maxGridHeight > 100) // Altura mínima
                {
                    AssociatedObject.MaxHeight = maxGridHeight;
                    AssociatedObject.Height = double.NaN; // Auto
                }
            }
            else
            {
                // Hay espacio suficiente, dejar que crezca libremente
                AssociatedObject.MaxHeight = double.PositiveInfinity;
                AssociatedObject.Height = double.NaN; // Auto
            }
        }
    }
}
