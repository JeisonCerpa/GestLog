using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GestLog;
using GestLog.Modules.GestionVehiculos.Interfaces.Storage;

namespace GestLog.Modules.GestionVehiculos.Services.Utilities
{
    public static class FacturaStorageHelper
    {
        public static async Task<string?> PickAndUploadFacturaAsync(Window owner, string logicalPrefix)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar factura (PDF o imagen)",
                Filter = "Archivos PDF/Imagen|*.pdf;*.png;*.jpg;*.jpeg|PDF (*.pdf)|*.pdf|Imagen (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Todos los archivos (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dlg.ShowDialog(owner) != true)
            {
                return null;
            }

            var serviceProvider = ((App)System.Windows.Application.Current).ServiceProvider;
            var storage = serviceProvider?.GetService(typeof(IPhotoStorageService)) as IPhotoStorageService;
            if (storage == null)
            {
                System.Windows.MessageBox.Show(owner,
                    "No se encontró el servicio de almacenamiento. No fue posible subir la factura.",
                    "Storage no disponible",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return null;
            }

            try
            {
                var ext = Path.GetExtension(dlg.FileName) ?? string.Empty;
                var fileName = $"{Sanitize(logicalPrefix)}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";

                using var stream = File.OpenRead(dlg.FileName);
                var storagePath = await storage.SaveOriginalAsync(stream, fileName);
                return storagePath;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(owner,
                    $"No se pudo subir la factura al storage: {ex.Message}",
                    "Error al adjuntar factura",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return null;
            }
        }

        public static async Task OpenFacturaAsync(Window owner, string? storagePath)
        {
            if (string.IsNullOrWhiteSpace(storagePath))
            {
                System.Windows.MessageBox.Show(owner,
                    "No hay una factura adjunta para visualizar.",
                    "Factura no disponible",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var serviceProvider = ((App)System.Windows.Application.Current).ServiceProvider;
            var storage = serviceProvider?.GetService(typeof(IPhotoStorageService)) as IPhotoStorageService;
            if (storage == null)
            {
                System.Windows.MessageBox.Show(owner,
                    "No se encontró el servicio de almacenamiento para abrir la factura.",
                    "Storage no disponible",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                var uri = await storage.GetUriAsync(storagePath);
                Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(owner,
                    $"No se pudo abrir la factura: {ex.Message}",
                    "Error al abrir factura",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static string Sanitize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "factura";
            }

            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (var ch in input.Trim().Replace(' ', '_'))
            {
                if (Array.IndexOf(invalid, ch) >= 0)
                {
                    continue;
                }

                sb.Append(ch);
            }

            return sb.Length == 0 ? "factura" : sb.ToString();
        }
    }
}
