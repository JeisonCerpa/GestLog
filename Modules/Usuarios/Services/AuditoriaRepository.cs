using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Modules.Usuarios.Interfaces;
using GestLog.Modules.Usuarios.Models;

namespace Modules.Usuarios.Services
{
    public class AuditoriaRepository : IAuditoriaRepository
    {
        public AuditoriaRepository()
        {
            // Inicialización de recursos de datos
        }

        public async Task RegistrarAsync(Auditoria auditoria)
        {
            // Implementación básica: simula persistencia en log
            await Task.Run(() =>
            {
                // Aquí deberías guardar en base de datos, por ahora solo loguea
                Console.WriteLine($"[AUDITORÍA] {auditoria.FechaHora}: {auditoria.Accion} sobre {auditoria.EntidadAfectada} ({auditoria.IdEntidad}) por {auditoria.UsuarioResponsable}. Detalle: {auditoria.Detalle}");
            });
        }

        public Task<IEnumerable<Auditoria>> ObtenerPorEntidadAsync(string entidadAfectada, Guid idEntidad)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Auditoria>> ObtenerPorUsuarioAsync(string usuarioResponsable)
        {
            throw new NotImplementedException();
        }
    }
}
