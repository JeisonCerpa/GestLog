using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;

namespace GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data
{
    /// <summary>
    /// Interfaz para servicios de mantenimientos correctivos (reactivos)
    /// Define operaciones CRUD y de gestión de reparaciones no programadas
    /// </summary>
    public interface IMantenimientoCorrectivoService
    {
        /// <summary>
        /// Obtiene todos los mantenimientos correctivos con opción de filtrar por estado
        /// </summary>
        /// <param name="includeDadosDeBaja">Si true, incluye registros dados de baja</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de mantenimientos correctivos</returns>
        Task<List<MantenimientoCorrectivoDto>> ObtenerTodosAsync(
            bool includeDadosDeBaja = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene un mantenimiento correctivo por su ID
        /// </summary>
        /// <param name="id">ID del mantenimiento</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>DTO del mantenimiento o null si no existe</returns>
        Task<MantenimientoCorrectivoDto?> ObtenerPorIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene mantenimientos correctivos de un equipo específico
        /// </summary>
        /// <param name="equipoInformaticoId">ID del equipo</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de mantenimientos del equipo</returns>
        Task<List<MantenimientoCorrectivoDto>> ObtenerPorEquipoAsync(
            int equipoInformaticoId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene mantenimientos correctivos de un periférico específico
        /// </summary>
        /// <param name="perifericoId">ID del periférico</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de mantenimientos del periférico</returns>
        Task<List<MantenimientoCorrectivoDto>> ObtenerPorPerifericoAsync(
            int perifericoId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene mantenimientos correctivos que están en estado "En Reparación"
        /// Útil para validar si un equipo/periférico puede ser dado de baja
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de mantenimientos en reparación</returns>
        Task<List<MantenimientoCorrectivoDto>> ObtenerEnReparacionAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea un nuevo mantenimiento correctivo
        /// Cambia automáticamente el estado del equipo/periférico a "En reparación"
        /// </summary>
        /// <param name="dto">DTO con los datos del nuevo mantenimiento</param>
        /// <param name="usuarioRegistroId">ID del usuario que registra</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>ID del mantenimiento creado</returns>
        Task<int> CrearAsync(
            MantenimientoCorrectivoDto dto,
            int usuarioRegistroId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza un mantenimiento correctivo existente
        /// </summary>
        /// <param name="dto">DTO con los datos actualizados</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>true si la actualización fue exitosa</returns>
        Task<bool> ActualizarAsync(
            MantenimientoCorrectivoDto dto,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Marca un mantenimiento como completado
        /// Cambia el estado a "Completado" y restaura el estado anterior del equipo/periférico
        /// </summary>
        /// <param name="id">ID del mantenimiento</param>
        /// <param name="observaciones">Observaciones finales (opcional)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>true si se completó exitosamente</returns>
        Task<bool> CompletarAsync(
            int id,
            string? observaciones = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancela un mantenimiento correctivo
        /// Cambia el estado a "Cancelado" y restaura el estado anterior del equipo/periférico
        /// </summary>
        /// <param name="id">ID del mantenimiento</param>
        /// <param name="razonCancelacion">Razón por la cual se cancela</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>true si se canceló exitosamente</returns>
        Task<bool> CancelarAsync(
            int id,
            string razonCancelacion,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Da de baja lógica un mantenimiento correctivo
        /// </summary>
        /// <param name="id">ID del mantenimiento</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>true si se dio de baja exitosamente</returns>
        Task<bool> DarDeBajaAsync(
            int id,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica si existe un mantenimiento correctivo en progreso para un equipo
        /// Usado para validar si el equipo puede ser dado de baja
        /// </summary>
        /// <param name="equipoInformaticoId">ID del equipo</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>true si existe mantenimiento en progreso</returns>
        Task<bool> ExisteMantenimientoEnProgresoAsync(
            int equipoInformaticoId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica si existe un mantenimiento correctivo en progreso para un periférico
        /// </summary>
        /// <param name="perifericoId">ID del periférico</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>true si existe mantenimiento en progreso</returns>
        Task<bool> ExisteMantenimientoPerifericoEnProgresoAsync(
            int perifericoId,
            CancellationToken cancellationToken = default);
    }
}
