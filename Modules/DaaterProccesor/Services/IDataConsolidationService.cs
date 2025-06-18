using System.Collections.Generic;
using System.Data;
using System.Threading;
using GestLog.Models.Configuration.Modules;

namespace GestLog.Modules.DaaterProccesor.Services;

public interface IDataConsolidationService
{
    DataTable ConsolidarDatos(
        string folderPath,
        Dictionary<string, string> paises,
        Dictionary<long, string[]> partidas,
        Dictionary<string, string> proveedores,
        System.IProgress<double> progress,
        CancellationToken cancellationToken = default
    );
    
    DataTable ConsolidarDatos(
        string folderPath,
        Dictionary<string, string> paises,
        Dictionary<long, string[]> partidas,
        Dictionary<string, string> proveedores,
        DaaterProcessorSettings settings,
        System.IProgress<double> progress,
        CancellationToken cancellationToken = default
    );
}
