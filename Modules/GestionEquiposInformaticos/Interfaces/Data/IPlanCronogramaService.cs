using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;

namespace GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data
{    
    public interface IPlanCronogramaService
    {
        Task<List<PlanCronogramaEquipo>> GetAllAsync();
        Task<PlanCronogramaEquipo?> GetByIdAsync(Guid planId);
        Task<List<PlanCronogramaEquipo>> GetByCodigoEquipoAsync(string codigoEquipo);
        Task<PlanCronogramaEquipo> CreateAsync(PlanCronogramaEquipo plan);
        Task UpdateAsync(PlanCronogramaEquipo plan);
        Task DeleteAsync(Guid planId);
        Task<List<EjecucionSemanal>> GetEjecucionesByPlanAsync(Guid planId, int anio);
        Task<List<EjecucionSemanal>> GetEjecucionesByEquipoAsync(string codigoEquipo, int anio);  // ✅ NUEVO
        Task<EjecucionSemanal> RegistrarEjecucionAsync(Guid planId, int anioISO, int semanaISO, DateTime fechaEjecucion, string usuarioEjecuta, string? resultadoJson = null);
        Task<List<PlanCronogramaEquipo>> GetPlanesParaSemanaAsync(int anioISO, int semanaISO);        Task<List<EjecucionSemanal>> GetEjecucionesByAnioAsync(int anioISO);
        
        /// <summary>
        /// ✅ NUEVO: Obtiene ejecuciones CON TRAZABILIDAD COMPLETA.
        /// 
        /// Por cada plan activo:
        /// 1. Calcula semanas esperadas desde FechaCreacion hasta fin del año actual
        /// 2. Consulta ejecuciones registradas de ese plan
        /// 3. Detecta semanas faltantes (sin registro)
        /// 4. Para cada semana faltante: GENERA y GUARDA un registro con Estado=3 (NoRealizada)
        /// 5. Evita duplicados verificando si ya existe antes de insertar
        /// 
        /// Permite ver qué mantenimientos fueron olvidados o están pendientes.
        /// Los registros se guardan en BD para trazabilidad permanente.
        /// </summary>
        Task<List<EjecucionSemanal>> GenerarYObtenerEjecucionesConTrazabilidadAsync(int anioISO);
        
        Task<List<int>> GetAvailableYearsAsync();
    }
}
