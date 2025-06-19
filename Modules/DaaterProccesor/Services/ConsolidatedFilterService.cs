using System.Data;
using System.Linq;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.DaaterProccesor.Services;

public class ConsolidatedFilterService : IConsolidatedFilterService
{
    private readonly IGestLogLogger _logger;

    public ConsolidatedFilterService(IGestLogLogger logger)
    {
        _logger = logger;
    }

    public DataTable FilterRows(DataTable consolidatedTable)
    {
        _logger.LogDebug("🔍 Iniciando filtrado de datos consolidados: {RowCount} filas", consolidatedTable.Rows.Count);
        
        // Primer filtro: columna DESCRIPCION GENERAL PARTIDA ARANCELARIA
        string colFiltro1 = "DESCRIPCION GENERAL PARTIDA ARANCELARIA";
        if (!consolidatedTable.Columns.Contains(colFiltro1))
        {
            _logger.LogWarning("⚠️ No se encontró la columna requerida para filtro: {ColumnName}", colFiltro1);
            System.Windows.MessageBox.Show($"No se encontró la columna '{colFiltro1}' en el DataTable.", "Error filtro");
            return consolidatedTable;
        }
        
        _logger.LogDebug("🔎 Aplicando primer filtro por: {ColumnName}", colFiltro1);
        var primerFiltrado = consolidatedTable.AsEnumerable()
            .Where(row => row[colFiltro1]?.ToString() == "Productos planos laminados en caliente"
                       || row[colFiltro1]?.ToString() == "Perfiles");

        // Segundo filtro: columna SIGNIFICADO SUB-PARTIDA NIVEL 1
        string colFiltro2 = "SIGNIFICADO SUB-PARTIDA NIVEL 1";
        if (!consolidatedTable.Columns.Contains(colFiltro2))
        {
            _logger.LogWarning("⚠️ No se encontró la columna requerida para filtro: {ColumnName}", colFiltro2);
            System.Windows.MessageBox.Show($"No se encontró la columna '{colFiltro2}' en el DataTable.", "Error filtro");
            return consolidatedTable;
        }
        
        string[] valoresPermitidos = new[] {
            "Ángulos",
            "De espesor superior a 10 mm",
            "De espesor superior a 4,75 mm",
            "De espesor superior o igual a 3 mm pero inferior a 4,75 mm",
            "De espesor superior o igual a 3 mm pero inferior o igual a 4,75 mm",
            "De espesor superior o igual a 4,75 mm pero inferior o igual a 10 mm",
            "Perfiles en H (Vigas en H)",
            "Perfiles en I (Vigas en I)",
            "Perfiles en L (Ángulos)",
            "Perfiles en U (Canales)"
        };
        
        _logger.LogDebug("🔎 Aplicando segundo filtro por: {ColumnName} con {ValuesCount} valores permitidos", 
            colFiltro2, valoresPermitidos.Length);
            
        var segundoFiltrado = primerFiltrado
            .Where(row => valoresPermitidos.Contains(row[colFiltro2]?.ToString() ?? string.Empty));

        if (!segundoFiltrado.Any())
        {
            _logger.LogWarning("⚠️ Filtrado no produjo resultados");
            return consolidatedTable.Clone(); // Retorna tabla vacía con mismas columnas
        }

        var resultado = segundoFiltrado.CopyToDataTable();
        _logger.LogDebug("✅ Filtrado completado: {FilteredRowCount} filas resultantes de {OriginalRowCount}", 
            resultado.Rows.Count, consolidatedTable.Rows.Count);
            
        return resultado;
    }

    /// <summary>
    /// Filtra únicamente registros con partida arancelaria 7225400000 (ACEROS ESPECIALES)
    /// </summary>
    public DataTable FilterAcerosEspeciales(DataTable consolidatedTable)
    {
        _logger.LogDebug("🔍 Filtrando ACEROS ESPECIALES (7225400000): {RowCount} filas", consolidatedTable.Rows.Count);
        
        const string colPartida = "PARTIDA ARANCELARIA";
        const long partidaEspeciales = 7225400000;
        
        // Verificar que existe la columna
        if (!consolidatedTable.Columns.Contains(colPartida))
        {
            _logger.LogWarning("⚠️ No se encontró la columna '{ColumnName}' para filtro de ACEROS ESPECIALES", colPartida);
            System.Windows.MessageBox.Show($"No se encontró la columna '{colPartida}' en los datos.", "Error filtro");
            return consolidatedTable.Clone(); // Retorna tabla vacía con mismas columnas
        }
        
        // Aplicar filtro por partida arancelaria específica
        _logger.LogDebug("🔎 Buscando registros con partida arancelaria: {Partida}", partidaEspeciales);
        
        var filtrados = consolidatedTable.AsEnumerable()
            .Where(row =>
            {
                var partidaValue = row[colPartida];
                if (partidaValue != null && long.TryParse(partidaValue.ToString(), out var partida))
                {
                    return partida == partidaEspeciales;
                }
                return false;
            });

        if (!filtrados.Any())
        {
            _logger.LogWarning("⚠️ No se encontraron registros para ACEROS ESPECIALES (partida {Partida})", partidaEspeciales);
            return consolidatedTable.Clone(); // Retorna tabla vacía con mismas columnas
        }

        var resultado = filtrados.CopyToDataTable();
        _logger.LogInformation("✅ ACEROS ESPECIALES filtrados: {FilteredRowCount} registros encontrados", resultado.Rows.Count);
        
        return resultado;
    }    /// <summary>
    /// Filtra registros para LAMINAS según criterios específicos
    /// </summary>
    public DataTable FilterLaminas(DataTable consolidatedTable)
    {
        _logger.LogDebug("🔍 Filtrando LAMINAS: {RowCount} filas", consolidatedTable.Rows.Count);
          const string colDescripcionGeneral = "DESCRIPCION GENERAL PARTIDA ARANCELARIA";
        const string colSignificadoSubPartida = "SIGNIFICADO SUB-PARTIDA";
        const string colSignificadoSubPartidaNivel1 = "SIGNIFICADO SUB-PARTIDA NIVEL 1";
        const string colFOBPorTon = "FOB POR TON";
        
        // Verificar que existen las columnas necesarias
        var requiredColumns = new[] { colDescripcionGeneral, colSignificadoSubPartida, colSignificadoSubPartidaNivel1, colFOBPorTon };
        foreach (var column in requiredColumns)
        {
            if (!consolidatedTable.Columns.Contains(column))
            {
                _logger.LogWarning("⚠️ No se encontró la columna '{ColumnName}' para filtro de LAMINAS", column);
                System.Windows.MessageBox.Show($"No se encontró la columna '{column}' en los datos.", "Error filtro LAMINAS");
                return consolidatedTable.Clone(); // Retorna tabla vacía con mismas columnas
            }
        }
        
        _logger.LogDebug("🔎 Aplicando filtro LAMINAS con FOB POR TON entre 0 y 1000...");
        
        var filtrados = consolidatedTable.AsEnumerable()
            .Where(row =>
            {
                // 1. Filtro por DESCRIPCIÓN GENERAL: únicamente "Productos planos laminados en caliente"
                var descripcionGeneral = row[colDescripcionGeneral]?.ToString() ?? string.Empty;
                if (descripcionGeneral != "Productos planos laminados en caliente")
                    return false;
                
                // 2. Filtro por SIGNIFICADO SUB-PARTIDA: valores específicos para láminas
                var significadoSubPartida = row[colSignificadoSubPartida]?.ToString() ?? string.Empty;
                var subPartidaValida = significadoSubPartida == "Láminas, laminados en caliente, sin relieve" ||
                                     significadoSubPartida == "Láminas, laminados en caliente, con motivos en relieve";
                if (!subPartidaValida)
                    return false;
                  // 3. Filtro por SIGNIFICADO SUB-PARTIDA NIVEL 1: quitar "De espesor inferior a 3 mm"
                var significadoSubPartidaNivel1 = row[colSignificadoSubPartidaNivel1]?.ToString() ?? string.Empty;
                if (significadoSubPartidaNivel1 == "De espesor inferior a 3 mm")
                    return false;
                
                // 4. Filtro por FOB POR TON: entre 0 y 1000 (> 0 y < 1000)
                var fobPorTonValue = row[colFOBPorTon];
                if (fobPorTonValue != null && decimal.TryParse(fobPorTonValue.ToString(), out var fobPorTon))
                {
                    if (fobPorTon <= 0 || fobPorTon >= 1000)
                        return false;
                }
                else
                {
                    // Si no se puede parsear el valor FOB, excluir el registro
                    return false;
                }
                
                return true;
            });

        if (!filtrados.Any())
        {
            _logger.LogWarning("⚠️ No se encontraron registros para LAMINAS");
            return consolidatedTable.Clone(); // Retorna tabla vacía con mismas columnas
        }

        var resultado = filtrados.CopyToDataTable();
        _logger.LogInformation("✅ LAMINAS filtradas: {FilteredRowCount} registros encontrados", resultado.Rows.Count);
        
        return resultado;
    }
    /// <summary>
    /// Filtra registros para ROLLOS según criterios específicos
    /// </summary>
    public DataTable FilterRollos(DataTable consolidatedTable)
    {
        _logger.LogDebug("🔍 Filtrando ROLLOS: {RowCount} filas", consolidatedTable.Rows.Count);
        
        const string colDescripcionGeneral = "DESCRIPCION GENERAL PARTIDA ARANCELARIA";
        const string colSignificadoSubPartida = "SIGNIFICADO SUB-PARTIDA";
        const string colSignificadoSubPartidaNivel1 = "SIGNIFICADO SUB-PARTIDA NIVEL 1";
        const string colFOBPorTon = "FOB POR TON";
        
        // Verificar que existen las columnas necesarias
        var requiredColumns = new[] { colDescripcionGeneral, colSignificadoSubPartida, colSignificadoSubPartidaNivel1, colFOBPorTon };
        foreach (var column in requiredColumns)
        {
            if (!consolidatedTable.Columns.Contains(column))
            {
                _logger.LogWarning("⚠️ No se encontró la columna '{ColumnName}' para filtro de ROLLOS", column);
                System.Windows.MessageBox.Show($"No se encontró la columna '{column}' en los datos.", "Error filtro ROLLOS");
                return consolidatedTable.Clone(); // Retorna tabla vacía con mismas columnas
            }
        }
        
        _logger.LogDebug("🔎 Aplicando filtro ROLLOS con FOB POR TON entre 0 y 1000...");
        
        var filtrados = consolidatedTable.AsEnumerable()
            .Where(row =>
            {
                // 1. Filtro por DESCRIPCIÓN GENERAL: únicamente "Productos planos laminados en caliente"
                var descripcionGeneral = row[colDescripcionGeneral]?.ToString() ?? string.Empty;
                if (descripcionGeneral != "Productos planos laminados en caliente")
                    return false;
                
                // 2. Filtro por SIGNIFICADO SUB-PARTIDA: valores específicos para rollos
                var significadoSubPartida = row[colSignificadoSubPartida]?.ToString() ?? string.Empty;
                var subPartidaValida = significadoSubPartida == "Rollos, laminados en caliente, sin relieve ni decapar" ||
                                     significadoSubPartida == "Rollos, laminados en caliente, con motivos en relieve" ||
                                     significadoSubPartida == "Rollos, laminados en caliente, decapados";
                if (!subPartidaValida)
                    return false;
                
                // 3. Filtro por SIGNIFICADO SUB-PARTIDA NIVEL 1: quitar "De espesor inferior a 3 mm"
                var significadoSubPartidaNivel1 = row[colSignificadoSubPartidaNivel1]?.ToString() ?? string.Empty;
                if (significadoSubPartidaNivel1 == "De espesor inferior a 3 mm")
                    return false;
                
                // 4. Filtro por FOB POR TON: entre 0 y 1000 (> 0 y < 1000)
                var fobPorTonValue = row[colFOBPorTon];
                if (fobPorTonValue != null && decimal.TryParse(fobPorTonValue.ToString(), out var fobPorTon))
                {
                    if (fobPorTon <= 0 || fobPorTon >= 1000)
                        return false;
                }
                else
                {
                    // Si no se puede parsear el valor FOB, excluir el registro
                    return false;
                }
                
                return true;
            });

        if (!filtrados.Any())
        {
            _logger.LogWarning("⚠️ No se encontraron registros para ROLLOS");
            return consolidatedTable.Clone(); // Retorna tabla vacía con mismas columnas
        }

        var resultado = filtrados.CopyToDataTable();
        _logger.LogInformation("✅ ROLLOS filtrados: {FilteredRowCount} registros encontrados", resultado.Rows.Count);
        
        return resultado;
    }
    /// <summary>
    /// Filtra registros para ANGULOS según criterios específicos
    /// </summary>
    public DataTable FilterAngulos(DataTable consolidatedTable)
    {
        _logger.LogDebug("🔍 Filtrando ANGULOS: {RowCount} filas", consolidatedTable.Rows.Count);
        
        const string colDescripcionGeneral = "DESCRIPCION GENERAL PARTIDA ARANCELARIA";
        const string colSignificadoSubPartidaNivel1 = "SIGNIFICADO SUB-PARTIDA NIVEL 1";
        const string colFOBPorTon = "FOB POR TON";
        
        // Verificar que existen las columnas necesarias
        var requiredColumns = new[] { colDescripcionGeneral, colSignificadoSubPartidaNivel1, colFOBPorTon };
        foreach (var column in requiredColumns)
        {
            if (!consolidatedTable.Columns.Contains(column))
            {
                _logger.LogWarning("⚠️ No se encontró la columna '{ColumnName}' para filtro de ANGULOS", column);
                System.Windows.MessageBox.Show($"No se encontró la columna '{column}' en los datos.", "Error filtro ANGULOS");
                return consolidatedTable.Clone(); // Retorna tabla vacía con mismas columnas
            }
        }
        
        _logger.LogDebug("🔎 Aplicando filtro ANGULOS con FOB POR TON entre 0 y 1000...");
        
        var filtrados = consolidatedTable.AsEnumerable()
            .Where(row =>
            {
                // 1. Filtro por DESCRIPCIÓN GENERAL: únicamente "Perfiles"
                var descripcionGeneral = row[colDescripcionGeneral]?.ToString() ?? string.Empty;
                if (descripcionGeneral != "Perfiles")
                    return false;
                
                // 2. Filtro por SIGNIFICADO SUB-PARTIDA NIVEL 1: valores específicos para ángulos
                var significadoSubPartidaNivel1 = row[colSignificadoSubPartidaNivel1]?.ToString() ?? string.Empty;
                var subPartidaValida = significadoSubPartidaNivel1 == "Perfiles en L (Ángulos)" ||
                                     significadoSubPartidaNivel1 == "Ángulos";
                if (!subPartidaValida)
                    return false;
                
                // 3. Filtro por FOB POR TON: entre 0 y 1000 (> 0 y < 1000)
                var fobPorTonValue = row[colFOBPorTon];
                if (fobPorTonValue != null && decimal.TryParse(fobPorTonValue.ToString(), out var fobPorTon))
                {
                    if (fobPorTon <= 0 || fobPorTon >= 1000)
                        return false;
                }
                else
                {
                    // Si no se puede parsear el valor FOB, excluir el registro
                    return false;
                }
                
                return true;
            });

        if (!filtrados.Any())
        {
            _logger.LogWarning("⚠️ No se encontraron registros para ANGULOS");
            return consolidatedTable.Clone(); // Retorna tabla vacía con mismas columnas
        }

        var resultado = filtrados.CopyToDataTable();
        _logger.LogInformation("✅ ANGULOS filtrados: {FilteredRowCount} registros encontrados", resultado.Rows.Count);
        
        return resultado;
    }
    /// <summary>
    /// Filtra registros para CANALES según criterios específicos
    /// </summary>
    public DataTable FilterCanales(DataTable consolidatedTable)
    {
        _logger.LogDebug("🔍 Filtrando CANALES: {RowCount} filas", consolidatedTable.Rows.Count);
        
        const string colDescripcionGeneral = "DESCRIPCION GENERAL PARTIDA ARANCELARIA";
        const string colSignificadoSubPartidaNivel1 = "SIGNIFICADO SUB-PARTIDA NIVEL 1";
        const string colFOBPorTon = "FOB POR TON";
        
        // Verificar que existen las columnas necesarias
        var requiredColumns = new[] { colDescripcionGeneral, colSignificadoSubPartidaNivel1, colFOBPorTon };
        foreach (var column in requiredColumns)
        {
            if (!consolidatedTable.Columns.Contains(column))
            {
                _logger.LogWarning("⚠️ No se encontró la columna '{ColumnName}' para filtro de CANALES", column);
                System.Windows.MessageBox.Show($"No se encontró la columna '{column}' en los datos.", "Error filtro CANALES");
                return consolidatedTable.Clone(); // Retorna tabla vacía con mismas columnas
            }
        }
        
        _logger.LogDebug("🔎 Aplicando filtro CANALES con FOB POR TON entre 0 y 1000...");
        
        var filtrados = consolidatedTable.AsEnumerable()
            .Where(row =>
            {
                // 1. Filtro por DESCRIPCIÓN GENERAL: únicamente "Perfiles"
                var descripcionGeneral = row[colDescripcionGeneral]?.ToString() ?? string.Empty;
                if (descripcionGeneral != "Perfiles")
                    return false;
                
                // 2. Filtro por SIGNIFICADO SUB-PARTIDA NIVEL 1: únicamente "Perfiles en U (Canales)"
                var significadoSubPartidaNivel1 = row[colSignificadoSubPartidaNivel1]?.ToString() ?? string.Empty;
                if (significadoSubPartidaNivel1 != "Perfiles en U (Canales)")
                    return false;
                
                // 3. Filtro por FOB POR TON: entre 0 y 1000 (> 0 y < 1000)
                var fobPorTonValue = row[colFOBPorTon];
                if (fobPorTonValue != null && decimal.TryParse(fobPorTonValue.ToString(), out var fobPorTon))
                {
                    if (fobPorTon <= 0 || fobPorTon >= 1000)
                        return false;
                }
                else
                {
                    // Si no se puede parsear el valor FOB, excluir el registro
                    return false;
                }
                
                return true;
            });

        if (!filtrados.Any())
        {
            _logger.LogWarning("⚠️ No se encontraron registros para CANALES");
            return consolidatedTable.Clone(); // Retorna tabla vacía con mismas columnas
        }

        var resultado = filtrados.CopyToDataTable();
        _logger.LogInformation("✅ CANALES filtrados: {FilteredRowCount} registros encontrados", resultado.Rows.Count);
        
        return resultado;
    }
    /// <summary>
    /// Filtra registros para VIGAS según criterios específicos
    /// </summary>
    public DataTable FilterVigas(DataTable consolidatedTable)
    {
        _logger.LogDebug("🔍 Filtrando VIGAS: {RowCount} filas", consolidatedTable.Rows.Count);
        
        const string colDescripcionGeneral = "DESCRIPCION GENERAL PARTIDA ARANCELARIA";
        const string colSignificadoSubPartidaNivel1 = "SIGNIFICADO SUB-PARTIDA NIVEL 1";
        const string colFOBPorTon = "FOB POR TON";
        
        // Verificar que existen las columnas necesarias
        var requiredColumns = new[] { colDescripcionGeneral, colSignificadoSubPartidaNivel1, colFOBPorTon };
        foreach (var column in requiredColumns)
        {
            if (!consolidatedTable.Columns.Contains(column))
            {
                _logger.LogWarning("⚠️ No se encontró la columna '{ColumnName}' para filtro de VIGAS", column);
                System.Windows.MessageBox.Show($"No se encontró la columna '{column}' en los datos.", "Error filtro VIGAS");
                return consolidatedTable.Clone(); // Retorna tabla vacía con mismas columnas
            }
        }
        
        _logger.LogDebug("🔎 Aplicando filtro VIGAS con FOB POR TON entre 0 y 1000...");
        
        var filtrados = consolidatedTable.AsEnumerable()
            .Where(row =>
            {
                // 1. Filtro por DESCRIPCIÓN GENERAL: únicamente "Perfiles"
                var descripcionGeneral = row[colDescripcionGeneral]?.ToString() ?? string.Empty;
                if (descripcionGeneral != "Perfiles")
                    return false;
                
                // 2. Filtro por SIGNIFICADO SUB-PARTIDA NIVEL 1: valores específicos para vigas
                var significadoSubPartidaNivel1 = row[colSignificadoSubPartidaNivel1]?.ToString() ?? string.Empty;
                var subPartidaValida = significadoSubPartidaNivel1 == "Perfiles en H (Vigas en H)" ||
                                     significadoSubPartidaNivel1 == "Perfiles en I (Vigas en I)";
                if (!subPartidaValida)
                    return false;
                
                // 3. Filtro por FOB POR TON: entre 0 y 1000 (> 0 y < 1000)
                var fobPorTonValue = row[colFOBPorTon];
                if (fobPorTonValue != null && decimal.TryParse(fobPorTonValue.ToString(), out var fobPorTon))
                {
                    if (fobPorTon <= 0 || fobPorTon >= 1000)
                        return false;
                }
                else
                {
                    // Si no se puede parsear el valor FOB, excluir el registro
                    return false;
                }
                
                return true;
            });

        if (!filtrados.Any())
        {
            _logger.LogWarning("⚠️ No se encontraron registros para VIGAS");
            return consolidatedTable.Clone(); // Retorna tabla vacía con mismas columnas
        }

        var resultado = filtrados.CopyToDataTable();
        _logger.LogInformation("✅ VIGAS filtradas: {FilteredRowCount} registros encontrados", resultado.Rows.Count);
        
        return resultado;
    }
}
