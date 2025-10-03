using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;

namespace GestLog.Behaviors
{
    /// <summary>
    /// Behavior sencillo para TextBox que formatea entrada como dd/MM/yyyy
    /// - Usar: local:DateMaskBehavior.EnableMask="True" en un TextBox o DatePicker
    /// </summary>
    public static class DateMaskBehavior
    {
        public static readonly DependencyProperty EnableMaskProperty =
            DependencyProperty.RegisterAttached("EnableMask", typeof(bool), typeof(DateMaskBehavior), new PropertyMetadata(false, OnEnableMaskChanged));

        public static void SetEnableMask(DependencyObject element, bool value) => element.SetValue(EnableMaskProperty, value);
        public static bool GetEnableMask(DependencyObject element) => (bool)element.GetValue(EnableMaskProperty);

        private static void OnEnableMaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is System.Windows.Controls.TextBox tb)
            {
                if ((bool)e.NewValue)
                {
                    tb.PreviewTextInput += Tb_PreviewTextInput;
                    tb.PreviewKeyDown += Tb_PreviewKeyDown;
                    tb.LostFocus += Tb_LostFocus;
                    System.Windows.DataObject.AddPastingHandler(tb, OnTextBoxPaste);
                }
                else
                {
                    tb.PreviewTextInput -= Tb_PreviewTextInput;
                    tb.PreviewKeyDown -= Tb_PreviewKeyDown;
                    tb.LostFocus -= Tb_LostFocus;
                    System.Windows.DataObject.RemovePastingHandler(tb, OnTextBoxPaste);
                }
            }
            else if (d is System.Windows.Controls.DatePicker dp)
            {
                // Si se adjunta a DatePicker, intentar encontrar el TextBox dentro de su template
                if ((bool)e.NewValue)
                {
                    dp.Loaded += Dp_Loaded;
                    dp.Unloaded += Dp_Unloaded;
                }
                else
                {
                    dp.Loaded -= Dp_Loaded;
                    dp.Unloaded -= Dp_Unloaded;
                }
            }
        }

        private static void Dp_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.DatePicker dp)
            {
                var tb = FindTextBoxInDatePicker(dp);
                if (tb is not null)
                {
                    tb.PreviewTextInput -= Tb_PreviewTextInput;
                    tb.PreviewKeyDown -= Tb_PreviewKeyDown;
                    tb.LostFocus -= Tb_LostFocus;
                    System.Windows.DataObject.RemovePastingHandler(tb, OnTextBoxPaste);
                }
            }
        }

        private static void Dp_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.DatePicker dp)
            {
                dp.ApplyTemplate();
                var tb = FindTextBoxInDatePicker(dp);
                if (tb is not null)
                {
                    if (dp.SelectedDate.HasValue)
                        tb.Text = dp.SelectedDate.Value.ToString("dd/MM/yyyy");

                    tb.PreviewTextInput += Tb_PreviewTextInput;
                    tb.PreviewKeyDown += Tb_PreviewKeyDown;
                    tb.LostFocus += Tb_LostFocus;
                    System.Windows.DataObject.AddPastingHandler(tb, OnTextBoxPaste);

                    dp.SelectedDateChanged += (s, ev) =>
                    {
                        if (dp.SelectedDate.HasValue)
                            tb.Text = dp.SelectedDate.Value.ToString("dd/MM/yyyy");
                        else
                            tb.Text = string.Empty;
                    };
                }
            }
        }

        private static System.Windows.Controls.TextBox? FindTextBoxInDatePicker(System.Windows.Controls.DatePicker dp)
        {
            try
            {
                dp.ApplyTemplate();
                var part = dp.Template.FindName("PART_TextBox", dp) as System.Windows.Controls.TextBox;
                if (part is not null) return part;

                // Fallback: buscar en visual tree
                return FindChildTextBox(dp);
            }
            catch
            {
                return null;
            }
        }

        private static System.Windows.Controls.TextBox? FindChildTextBox(DependencyObject parent)
        {
            if (parent == null) return null;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is System.Windows.Controls.TextBox tb) return tb;
                var found = FindChildTextBox(child);
                if (found is not null) return found;
            }
            return null;
        }

        private static void OnTextBoxPaste(object sender, System.Windows.DataObjectPastingEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
            {
                if (e.DataObject.GetDataPresent(System.Windows.DataFormats.Text))
                {
                    var pasted = e.DataObject.GetData(System.Windows.DataFormats.Text) as string ?? string.Empty;
                    var sanitized = SanitizeAndFormat(pasted);
                    tb.Text = sanitized;
                    tb.CaretIndex = tb.Text.Length;
                    e.CancelCommand();
                }
            }
        }

        private static void Tb_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
            {
                var text = tb.Text?.Trim() ?? string.Empty;
                if (DateTime.TryParseExact(text, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    tb.Text = dt.ToString("dd/MM/yyyy");
                }
                // else dejar lo que el usuario escribió (no válido)
            }
        }

        private static void Tb_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
            {
                // Permitir Backspace y Delete por defecto
                if (e.Key == System.Windows.Input.Key.Back || e.Key == System.Windows.Input.Key.Delete) return;

                // Al presionar Enter, forzar lost focus behaviour
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    Tb_LostFocus(tb, null!);
                    e.Handled = true;
                }
            }
        }

        private static void Tb_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
            {
                var input = e.Text;
                if (string.IsNullOrEmpty(input)) return;

                // Sólo permitir dígitos
                if (!char.IsDigit(input, 0))
                {
                    e.Handled = true;
                    return;
                }

                var currentText = tb.Text ?? string.Empty;
                var selStart = tb.SelectionStart;
                var selLength = tb.SelectionLength;

                // Contar dígitos antes de la posición de selección (ignorar '/'), y cuántos dígitos hay en la selección
                int digitsBefore = 0;
                for (int i = 0; i < Math.Min(selStart, currentText.Length); i++) if (char.IsDigit(currentText[i])) digitsBefore++;
                int digitsInSelection = 0;
                for (int i = selStart; i < Math.Min(selStart + selLength, currentText.Length); i++) if (char.IsDigit(currentText[i])) digitsInSelection++;

                // Obtener la secuencia de dígitos actual
                var digitsBuilder = new System.Text.StringBuilder();
                foreach (var ch in currentText) if (char.IsDigit(ch)) digitsBuilder.Append(ch);
                var digits = digitsBuilder.ToString();

                // Eliminar los dígitos que están siendo reemplazados por la selección
                if (digitsInSelection > 0 && digitsBefore + digitsInSelection <= digits.Length)
                {
                    digits = digits.Remove(digitsBefore, digitsInSelection);
                }

                // Insertar el nuevo dígito en la posición correspondiente dentro de la secuencia de dígitos
                if (digitsBefore <= digits.Length)
                    digits = digits.Insert(digitsBefore, input);
                else
                    digits = digits + input;

                // Limitar a 8 dígitos (ddMMyyyy)
                if (digits.Length > 8) digits = digits.Substring(0, 8);

                // Formatear a dd/MM/yyyy
                var formatted = SanitizeAndFormat(digits);

                // Calcular nueva posición del caret basada en índice de dígito (ponerlo después del dígito insertado)
                int newDigitIndex = digitsBefore + 1; // índice (1-based) del dígito recién insertado
                int caretPos = newDigitIndex;
                if (newDigitIndex >= 4) // después de segundo separador
                    caretPos += 2;
                else if (newDigitIndex >= 2) // después del primer separador
                    caretPos += 1;

                // Ajustar límites
                if (caretPos > formatted.Length) caretPos = formatted.Length;

                // Actualizar texto en dispatcher
                tb.Dispatcher.InvokeAsync(() =>
                {
                    tb.Text = formatted;
                    tb.CaretIndex = caretPos;
                }, System.Windows.Threading.DispatcherPriority.Normal);

                e.Handled = true;
            }
        }

        private static string SanitizeAndFormat(string raw)
        {
            var digits = new StringBuilder();
            foreach (var ch in raw)
            {
                if (char.IsDigit(ch)) digits.Append(ch);
            }

            // Max 8 digits for ddMMyyyy
            var s = digits.ToString();
            if (s.Length > 8) s = s.Substring(0, 8);

            if (s.Length == 0) return string.Empty;
            if (s.Length <= 2) return s;
            if (s.Length <= 4) return s.Insert(2, "/");
            // >=5
            var withFirst = s.Insert(2, "/");
            return withFirst.Insert(5, "/");
        }
    }
}
