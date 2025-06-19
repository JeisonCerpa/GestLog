using System.Data;

namespace GestLog.Modules.DaaterProccesor.Services;

public interface IConsolidatedFilterService
{
    DataTable FilterRows(DataTable consolidatedTable);
    DataTable FilterAcerosEspeciales(DataTable consolidatedTable);
    DataTable FilterLaminas(DataTable consolidatedTable);
    DataTable FilterRollos(DataTable consolidatedTable);
    DataTable FilterAngulos(DataTable consolidatedTable);
    DataTable FilterCanales(DataTable consolidatedTable);
    DataTable FilterVigas(DataTable consolidatedTable);
}
