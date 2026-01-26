using System.Collections.Generic;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;

namespace GestLog.Modules.GestionMantenimientos.Models.Import
{
    public class SeguimientoImportResult
    {
        public int ImportedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int IgnoredCount { get; set; }
        public List<(int Row, string Reason)> IgnoredRows { get; set; } = new List<(int, string)>();
        public List<SeguimientoMantenimientoDto> ImportedItems { get; set; } = new List<SeguimientoMantenimientoDto>();
    }
}
