using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionVehiculos.Interfaces;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Modules.GestionVehiculos.Models.Entities;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.GestionVehiculos.Services.Data
{
    /// <summary>
    /// Servicio para gestión de documentos de vehículos
    /// Implementa operaciones CRUD y consultas especializadas para documentos
    /// </summary>
    public class VehicleDocumentService : IVehicleDocumentService
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly IGestLogLogger _logger;

        public VehicleDocumentService(IDbContextFactory<GestLogDbContext> dbContextFactory, IGestLogLogger logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene todos los documentos de un vehículo específico
        /// </summary>
        public async Task<List<VehicleDocumentDto>> GetByVehicleIdAsync(Guid vehicleId)
        {
            try
            {
                // _logger.LogInformation($"VehicleDocumentService: Iniciando GetByVehicleIdAsync para VehicleId={vehicleId}");
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var documents = await context.VehicleDocuments
                    .AsNoTracking()
                    .Where(d => d.VehicleId == vehicleId && d.IsActive)
                    .OrderByDescending(d => d.ExpirationDate)
                    .ToListAsync();

                // _logger.LogInformation($"VehicleDocumentService: {documents.Count} documentos encontrados para VehicleId={vehicleId}");

                return documents.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener documentos del vehículo {vehicleId}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene un documento específico por su ID
        /// </summary>
        public async Task<VehicleDocumentDto?> GetByIdAsync(Guid documentId)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var document = await context.VehicleDocuments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                return document == null ? null : MapToDto(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener documento {documentId}");
                throw;
            }
        }

        /// <summary>
        /// Crea un nuevo documento de vehículo
        /// </summary>
        public async Task<Guid> AddAsync(VehicleDocumentDto documentDto)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                
                // Validar que el vehículo existe
                var vehicleExists = await context.Vehicles.AnyAsync(v => v.Id == documentDto.VehicleId);
                if (!vehicleExists)
                {
                    throw new InvalidOperationException($"El vehículo {documentDto.VehicleId} no existe");
                }                var document = new VehicleDocument
                {
                    Id = Guid.NewGuid(),
                    VehicleId = documentDto.VehicleId,
                    DocumentType = documentDto.DocumentType,
                    DocumentNumber = documentDto.DocumentNumber,
                    IssuedDate = documentDto.IssuedDate,
                    ExpirationDate = documentDto.ExpirationDate,
                    FileName = documentDto.FileName,
                    FilePath = documentDto.FilePath,
                    Notes = documentDto.Notes,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                context.VehicleDocuments.Add(document);
                await context.SaveChangesAsync();

                return document.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear documento de vehículo");
                throw;
            }
        }

        /// <summary>
        /// Actualiza un documento existente
        /// </summary>
        public async Task<bool> UpdateAsync(VehicleDocumentDto documentDto)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                
                var document = await context.VehicleDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentDto.Id);

                if (document == null)
                {
                    return false;
                }                document.DocumentType = documentDto.DocumentType;
                document.DocumentNumber = documentDto.DocumentNumber;
                document.IssuedDate = documentDto.IssuedDate;
                document.ExpirationDate = documentDto.ExpirationDate;
                document.FileName = documentDto.FileName;
                document.FilePath = documentDto.FilePath;
                document.Notes = documentDto.Notes;
                document.UpdatedAt = DateTimeOffset.UtcNow;

                context.VehicleDocuments.Update(document);
                await context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar documento");
                throw;
            }
        }

        /// <summary>
        /// Elimina un documento (borrado físico)
        /// </summary>
        public async Task<bool> DeleteAsync(Guid documentId)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                
                var document = await context.VehicleDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    return false;
                }

                // Eliminar físicamente la entidad de la base de datos
                context.VehicleDocuments.Remove(document);

                await context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar documento");
                throw;
            }
        }

        /// <summary>
        /// Obtiene documentos vencidos de un vehículo
        /// </summary>
        public async Task<List<VehicleDocumentDto>> GetExpiredDocumentsAsync(Guid vehicleId)
        {            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                
                var today = DateTimeOffset.UtcNow.Date;
                var documents = await context.VehicleDocuments
                    .AsNoTracking()
                    .Where(d => d.VehicleId == vehicleId 
                        && d.IsActive 
                        && d.ExpirationDate.Date < today)
                    .OrderBy(d => d.ExpirationDate)
                    .ToListAsync();

                return documents.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener documentos vencidos");
                throw;
            }
        }

        /// <summary>
        /// Obtiene documentos próximos a vencer (dentro de N días)
        /// </summary>
        public async Task<List<VehicleDocumentDto>> GetSoonToExpireDocumentsAsync(Guid vehicleId, int daysThreshold = 30)
        {            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                
                var today = DateTimeOffset.UtcNow.Date;
                var expirationLimit = today.AddDays(daysThreshold);

                var documents = await context.VehicleDocuments
                    .AsNoTracking()
                    .Where(d => d.VehicleId == vehicleId 
                        && d.IsActive 
                        && d.ExpirationDate.Date >= today
                        && d.ExpirationDate.Date <= expirationLimit)
                    .OrderBy(d => d.ExpirationDate)
                    .ToListAsync();

                return documents.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener documentos próximos a vencer");
                throw;
            }
        }

        /// <summary>
        /// Valida si un vehículo tiene todos los documentos requeridos vigentes
        /// </summary>
        public async Task<bool> HasRequiredDocumentsAsync(Guid vehicleId, params string[] requiredDocumentTypes)
        {
            try
            {
                if (requiredDocumentTypes == null || requiredDocumentTypes.Length == 0)
                {
                    return true;
                }                using var context = await _dbContextFactory.CreateDbContextAsync();
                
                var today = DateTimeOffset.UtcNow.Date;

                foreach (var docType in requiredDocumentTypes)
                {
                    var hasValidDocument = await context.VehicleDocuments
                        .AsNoTracking()
                        .AnyAsync(d => d.VehicleId == vehicleId 
                            && d.IsActive 
                            && d.DocumentType == docType
                            && d.ExpirationDate.Date >= today);

                    if (!hasValidDocument)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar documentos requeridos");
                throw;
            }
        }

        /// <summary>
        /// Obtiene estadísticas de documentos para un vehículo
        /// </summary>
        public async Task<DocumentStatisticsDto> GetStatisticsAsync(Guid vehicleId)
        {            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                
                var today = DateTimeOffset.UtcNow.Date;

                var allDocuments = await context.VehicleDocuments
                    .AsNoTracking()
                    .Where(d => d.VehicleId == vehicleId && d.IsActive)
                    .ToListAsync();

                var validDocuments = allDocuments.Count(d => d.ExpirationDate.Date >= today);
                var expiredDocuments = allDocuments.Count(d => d.ExpirationDate.Date < today);
                var soonToExpireDocuments = allDocuments.Count(d => 
                    d.ExpirationDate.Date >= today && 
                    d.ExpirationDate.Date <= today.AddDays(30));

                return new DocumentStatisticsDto
                {
                    TotalDocuments = allDocuments.Count,
                    ValidDocuments = validDocuments,
                    ExpiredDocuments = expiredDocuments,
                    SoonToExpireDocuments = soonToExpireDocuments
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de documentos");
                throw;
            }
        }

        /// <summary>
        /// Mapea una entidad VehicleDocument a su DTO
        /// </summary>
        private static VehicleDocumentDto MapToDto(VehicleDocument document)
        {
            return new VehicleDocumentDto
            {
                Id = document.Id,
                VehicleId = document.VehicleId,
                DocumentType = document.DocumentType,
                DocumentNumber = document.DocumentNumber,
                IssuedDate = document.IssuedDate,
                ExpirationDate = document.ExpirationDate,
                FileName = document.FileName,
                FilePath = document.FilePath,
                Notes = document.Notes,
                IsActive = document.IsActive,
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt
            };
        }
    }
}
