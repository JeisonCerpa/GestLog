using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GestLog.Behaviors
{
    /// <summary>
    /// Comportamiento adjunto para forzar que el TextBox editable interno de un ComboBox use CharacterCasing = Upper.
    /// Uso en XAML: behaviors:ComboBoxEditableUpperBehavior.EnableUppercase="True"
    /// </summary>
    public static class ComboBoxEditableUpperBehavior
    {
        public static readonly DependencyProperty EnableUppercaseProperty =
            DependencyProperty.RegisterAttached(
                "EnableUppercase",
                typeof(bool),
                typeof(ComboBoxEditableUpperBehavior),
                new PropertyMetadata(false, OnEnableUppercaseChanged));

        public static void SetEnableUppercase(DependencyObject element, bool value) => element.SetValue(EnableUppercaseProperty, value);
        public static bool GetEnableUppercase(DependencyObject element) => (bool)element.GetValue(EnableUppercaseProperty);

        private static void OnEnableUppercaseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is System.Windows.Controls.ComboBox cb)
            {
                if ((bool)e.NewValue)
                {
                    cb.Loaded += ComboBox_Loaded;
                    cb.Unloaded += ComboBox_Unloaded;
                }
                else
                {
                    cb.Loaded -= ComboBox_Loaded;
                    cb.Unloaded -= ComboBox_Unloaded;
                    // intentar limpiar handlers si existe el TextBox
                    TryCleanup(cb);
                }
            }
        }

        private static void ComboBox_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox cb)
            {
                cb.Loaded -= ComboBox_Loaded;
                cb.Unloaded -= ComboBox_Unloaded;
                TryCleanup(cb);
            }
        }

        private static void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox cb)
            {
                // Intentar aplicar inmediatamente y de nuevo en el dispatcher por si la plantilla aún no estaba lista
                TryApply(cb);
                cb.Dispatcher.BeginInvoke((Action)(() => TryApply(cb)), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private static void TryApply(System.Windows.Controls.ComboBox cb)
        {
            try
            {
                cb.ApplyTemplate();
                if (cb.Template == null) return;

                var tb = cb.Template.FindName("PART_EditableTextBox", cb) as System.Windows.Controls.TextBox;
                if (tb != null)
                {
                    tb.CharacterCasing = System.Windows.Controls.CharacterCasing.Upper;

                    // Agregar handler para pegado y asegurarnos de convertir texto pegado a mayúsculas
                    System.Windows.DataObject.RemovePastingHandler(tb, new System.Windows.DataObjectPastingEventHandler(OnPasting));
                    System.Windows.DataObject.AddPastingHandler(tb, new System.Windows.DataObjectPastingEventHandler(OnPasting));
                }
            }
            catch
            {
                // No hacer nada; en ocasiones la plantilla no está lista, se volverá a intentar.
            }
        }

        private static void TryCleanup(System.Windows.Controls.ComboBox cb)
        {
            try
            {
                if (cb.Template == null) return;
                var tb = cb.Template.FindName("PART_EditableTextBox", cb) as System.Windows.Controls.TextBox;
                if (tb != null)
                {
                    System.Windows.DataObject.RemovePastingHandler(tb, new System.Windows.DataObjectPastingEventHandler(OnPasting));
                }
            }
            catch { }
        }

        private static void OnPasting(object sender, System.Windows.DataObjectPastingEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
            {
                if (e.DataObject.GetDataPresent(System.Windows.DataFormats.UnicodeText))
                {
                    var text = e.DataObject.GetData(System.Windows.DataFormats.UnicodeText) as string ?? string.Empty;
                    var upper = text.ToUpperInvariant();
                    var data = new System.Windows.DataObject();
                    data.SetData(System.Windows.DataFormats.UnicodeText, upper);
                    e.DataObject = data;
                }
            }
        }
    }
}
