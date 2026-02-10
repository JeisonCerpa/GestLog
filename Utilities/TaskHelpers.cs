using System;
using System.Threading.Tasks;
using GestLog.Services.Core.Logging;

namespace GestLog.Utilities
{
    public static class TaskHelpers
    {
        /// <summary>
        /// Ejecuta una tarea en modo fire-and-forget y captura cualquier excepción, registrándola con el logger.
        /// </summary>
        public static async void FireAndForgetSafeAsync(this Task task, IGestLogLogger logger, string? context = null)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"Unhandled exception in fire-and-forget task. Context: {context}");
            }
        }
    }
}
