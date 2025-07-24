using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;
using Modules.Usuarios.Interfaces;

namespace Modules.Usuarios.Services
{
    public class TipoDocumentoRepository : ITipoDocumentoRepository
    {
        private readonly string _connectionString;

        public TipoDocumentoRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<TipoDocumento>> ObtenerTodosAsync()
        {
            var lista = new List<TipoDocumento>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SELECT IdTipoDocumento, Nombre, Codigo, Descripcion FROM TipoDocumento", conn);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new TipoDocumento
                {
                    IdTipoDocumento = reader.GetGuid(0),
                    Nombre = reader.GetString(1),
                    Codigo = reader.GetString(2),
                    Descripcion = reader.GetString(3)
                });
            }
            return lista;
        }

        public async Task<TipoDocumento?> ObtenerPorIdAsync(Guid id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SELECT IdTipoDocumento, Nombre, Codigo, Descripcion FROM TipoDocumento WHERE IdTipoDocumento = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new TipoDocumento
                {
                    IdTipoDocumento = reader.GetGuid(0),
                    Nombre = reader.GetString(1),
                    Codigo = reader.GetString(2),
                    Descripcion = reader.GetString(3)
                };
            }
            return null;
        }

        public async Task<TipoDocumento?> ObtenerPorCodigoAsync(string codigo)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SELECT IdTipoDocumento, Nombre, Codigo, Descripcion FROM TipoDocumento WHERE Codigo = @codigo", conn);
            cmd.Parameters.AddWithValue("@codigo", codigo);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new TipoDocumento
                {
                    IdTipoDocumento = reader.GetGuid(0),
                    Nombre = reader.GetString(1),
                    Codigo = reader.GetString(2),
                    Descripcion = reader.GetString(3)
                };
            }
            return null;
        }

        public async Task AgregarAsync(TipoDocumento tipoDocumento)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("INSERT INTO TipoDocumento (IdTipoDocumento, Nombre, Codigo, Descripcion) VALUES (@id, @nombre, @codigo, @descripcion)", conn);
            cmd.Parameters.AddWithValue("@id", tipoDocumento.IdTipoDocumento);
            cmd.Parameters.AddWithValue("@nombre", tipoDocumento.Nombre);
            cmd.Parameters.AddWithValue("@codigo", tipoDocumento.Codigo);
            cmd.Parameters.AddWithValue("@descripcion", tipoDocumento.Descripcion);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task ActualizarAsync(TipoDocumento tipoDocumento)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("UPDATE TipoDocumento SET Nombre = @nombre, Codigo = @codigo, Descripcion = @descripcion WHERE IdTipoDocumento = @id", conn);
            cmd.Parameters.AddWithValue("@id", tipoDocumento.IdTipoDocumento);
            cmd.Parameters.AddWithValue("@nombre", tipoDocumento.Nombre);
            cmd.Parameters.AddWithValue("@codigo", tipoDocumento.Codigo);
            cmd.Parameters.AddWithValue("@descripcion", tipoDocumento.Descripcion);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task EliminarAsync(Guid id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("DELETE FROM TipoDocumento WHERE IdTipoDocumento = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
