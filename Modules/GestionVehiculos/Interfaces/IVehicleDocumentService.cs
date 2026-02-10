using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.GestionVehiculos.Models.DTOs;

namespace GestLog.Modules.GestionVehiculos.Interfaces
{
    /// <summary>
    /// Interfaz para servicios de gestión de documentos de vehículos
    /// </summary>
    public interface IVehicleDocumentService
    {
        /// <summary>
        /// Obtiene todos los documentos de un vehículo específico
        /// </summary>
        /// <param name="vehicleId">ID del vehículo</param>
        /// <returns>Lista de documentos del vehículo</returns>
        Task<List<VehicleDocumentDto>> GetByVehicleIdAsync(Guid vehicleId);

        /// <summary>
        /// Obtiene un documento específico por su ID
        /// </summary>
        /// <param name="documentId">ID del documento</param>
        /// <returns>DTO del documento o null si no existe</returns>
        Task<VehicleDocumentDto?> GetByIdAsync(Guid documentId);

        /// <summary>
        /// Crea un nuevo documento de vehículo
        /// </summary>
        /// <param name="documentDto">DTO con los datos del documento</param>
        /// <returns>ID del documento creado</returns>
        Task<Guid> AddAsync(VehicleDocumentDto documentDto);

        /// <summary>
        /// Actualiza un documento existente
        /// </summary>
        /// <param name="documentDto">DTO con los datos actualizados</param>
        /// <returns>True si se actualizó correctamente</returns>
        Task<bool> UpdateAsync(VehicleDocumentDto documentDto);

        /// <summary>
        /// Elimina un documento
        /// </summary>
        /// <param name="documentId">ID del documento a eliminar</param>
        /// <returns>True si se eliminó correctamente</returns>
        Task<bool> DeleteAsync(Guid documentId);

        /// <summary>
        /// Obtiene documentos vencidos de un vehículo
        /// </summary>
        /// <param name="vehicleId">ID del vehículo</param>
        /// <returns>Lista de documentos vencidos</returns>
        Task<List<VehicleDocumentDto>> GetExpiredDocumentsAsync(Guid vehicleId);

        /// <summary>
        /// Obtiene documentos próximos a vencer (dentro de N días)
        /// </summary>
        /// <param name="vehicleId">ID del vehículo</param>
        /// <param name="daysThreshold">Número de días para considerar como "próximo a vencer"</param>
        /// <returns>Lista de documentos próximos a vencer</returns>
        Task<List<VehicleDocumentDto>> GetSoonToExpireDocumentsAsync(Guid vehicleId, int daysThreshold = 30);

        /// <summary>
        /// Valida si un vehículo tiene todos los documentos requeridos vigentes
        /// </summary>
        /// <param name="vehicleId">ID del vehículo</param>
        /// <param name="requiredDocumentTypes">Tipos de documentos requeridos</param>
        /// <returns>True si tiene todos los documentos vigentes</returns>
        Task<bool> HasRequiredDocumentsAsync(Guid vehicleId, params string[] requiredDocumentTypes);

        /// <summary>
        /// Obtiene estadísticas de documentos para un vehículo
        /// </summary>
        /// <param name="vehicleId">ID del vehículo</param>
        /// <returns>Objeto con estadísticas</returns>
        Task<DocumentStatisticsDto> GetStatisticsAsync(Guid vehicleId);
    }

    /// <summary>
    /// DTO con estadísticas de documentos
    /// </summary>
    public class DocumentStatisticsDto
    {
        public int TotalDocuments { get; set; }
        public int ValidDocuments { get; set; }
        public int ExpiredDocuments { get; set; }
        public int SoonToExpireDocuments { get; set; }
    }
}
