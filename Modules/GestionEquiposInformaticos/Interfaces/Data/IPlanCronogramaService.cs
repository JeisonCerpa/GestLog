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
        Task<EjecucionSemanal> RegistrarEjecucionAsync(Guid planId, int anioISO, int semanaISO, DateTime fechaEjecucion, string usuarioEjecuta, string? resultadoJson = null);
        Task<List<PlanCronogramaEquipo>> GetPlanesParaSemanaAsync(int anioISO, int semanaISO);
        Task<List<EjecucionSemanal>> GetEjecucionesByAnioAsync(int anioISO);
    }
}
