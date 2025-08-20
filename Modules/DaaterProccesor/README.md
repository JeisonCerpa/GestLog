# DaaterProccesor — Guía de uso y documentación técnica

Este documento incluye, primero, una guía clara para el usuario final sobre cómo usar el módulo DaaterProccesor desde la UI y qué esperar en términos de formato Excel y mensajes de error. Después se incluye una sección orientada a desarrolladores con el comportamiento técnico del parser, helpers disponibles y notas de integración.

---

## Guía para el usuario (primero)

Qué hace el módulo
- DaaterProccesor procesa archivos Excel que contienen información de embarques y proveedores para consolidar y normalizar datos.

Cómo usarlo desde la UI
1. Abra la vista del módulo DaaterProccesor en Herramientas → DaaterProccesor.
2. Seleccione el archivo Excel (.xlsx) que desea procesar.
3. Inicie el proceso (botón "Procesar" o similar). El proceso puede tardar según el tamaño del archivo.
4. Al finalizar verá un mensaje de éxito con el número de filas procesadas o un mensaje de error si el archivo no cumple el formato esperado.

Formato Excel esperado (resumen para usuarios)
- El archivo debe ser .xlsx.
- La primera fila se interpreta como fila de cabeceras (headers). Los nombres de cabecera pueden tener mayúsculas o minúsculas; el parser no es sensible a mayúsculas.
- Algunas cabeceras pueden aparecer más de una vez en la hoja cuando el formato lo requiere (p. ej. "IMPORTADOR" para NIT y para Nombre). El módulo espera ciertas repeticiones (si faltan ocurrencias mínimas, fallará).

Mensajes que verá en pantalla
- Éxito: "Procesados N elementos" o mensaje equivalente según la UI.
- Error de formato Excel: mostrará "Error Excel: <detalle>" con una explicación clara en español (p. ej. "Faltan columnas: IMPORTADOR (2) ").
- Error inesperado: "Error inesperado" y recomendación de contactar soporte.
- Operación cancelada: "Operación cancelada" si el usuario o el sistema aborta el proceso.

Qué hacer si recibe un error
- Verifique que el archivo sea .xlsx y que la primera fila contenga las cabeceras esperadas.
- Si el mensaje indica que faltan columnas, abra el archivo y confirme que las cabeceras faltantes existen y no están en hojas diferentes o filas desplazadas.
- Si no puede resolverlo, exporte el archivo de ejemplo y contacte soporte técnico con el mensaje de error y el archivo.

Limitaciones visibles para usuarios
- El módulo es tolerante a columnas extra o reordenadas siempre que las cabeceras requeridas (y sus ocurrencias esperadas) estén presentes.
- Si el proveedor de datos cambia completamente el formato (nombres de columnas diferentes), será necesario adaptar la plantilla o contactar al equipo técnico.

---

## Guía para desarrolladores

Resumen técnico del comportamiento
- El parser lee la primera fila como fila de cabeceras, normaliza cada nombre (Trim + ToUpperInvariant) y construye un mapa de posiciones para cada nombre.
- `requiredColumns` se interpreta como una lista que puede contener nombres repetidos; cada repetición representa una ocurrencia esperada de esa cabecera.
- El validador exige que existan al menos las ocurrencias esperadas. Si faltan ocurrencias mínimas se lanza `ExcelFormatException` con código `REQUIRED_COLUMNS`.
- Si hay más ocurrencias de las esperadas se registra un warning pero no se falla (modo permisivo por posición).

Estructuras y helpers clave
- headerPositions: Dictionary<string, List<int>>
  - Mapea el nombre normalizado de la cabecera a la lista de índices de columna (1‑based) donde aparece.

- GetColumnIndexForOccurrence(string name, int occurrence = 1)
  - Devuelve la columna (índice 1‑based) de la N‑ésima aparición de `name`. Lanza `ExcelFormatException` si la ocurrencia no existe.

- GetCellString(IXLRow row, string name, int occurrence = 1)
  - Devuelve el valor de la celda en la columna determinada por `GetColumnIndexForOccurrence` como string (vacío si nulo).

- BuildHeaderPositions(IXLRow headerRow)
  - Construye el `headerPositions` a partir de la fila de cabeceras.

Ejemplos prácticos
- Ejemplo de `requiredColumns` esperado por el parser:
```csharp
var requiredColumns = new[] {
  "EXPEDIDOR",
  "FECHA",
  "IMPORTADOR", // ocurrencia 1 -> NIT
  "IMPORTADOR", // ocurrencia 2 -> NOMBRE IMPORTADOR
  "PARTIDA ARANCELARIA",
  "EXPORTADOR (PROVEEDOR)"
};
```
- Uso en lectura de fila:
```csharp
var nitImportador = GetCellString(row, "IMPORTADOR", 1);
var nombreImportador = GetCellString(row, "IMPORTADOR", 2);
var partida = GetCellString(row, "PARTIDA ARANCELARIA", 1);
```
- Escritura de normalizaciones:
```csharp
var idxProveedor = GetColumnIndexForOccurrence("EXPORTADOR (PROVEEDOR)", 1);
worksheet.Row(r).Cell(idxProveedor).Value = proveedorNormalizado;
```

Códigos de error
- REQUIRED_COLUMNS — faltan ocurrencias mínimas esperadas en la fila de cabeceras.

Logging y debugging
- Usar `IGestLogLogger` para registrar:
  - Info: archivo procesado, número de filas.
  - Debug (no en producción): contenido de `headerPositions` para analizar mapeos.
  - Warning: cabeceras con más ocurrencias de las esperadas.
  - Error: excepciones que provoquen fallo del parsing.

Integración con ViewModels y UI
- Las excepciones `ExcelFormatException` llegan al ViewModel y deben convertirse en mensajes de usuario en español, por ejemplo:
```csharp
catch (ExcelFormatException ex)
{
    ErrorMessage = $"Error Excel: {ex.Message}";
    await ShowErrorAsync("Error de Formato", ex.Message);
}
```
- No exponer stacktraces al usuario; los detalles técnicos van a los logs.

Notas de migración
- Refactorizar cualquier acceso directo por índice (`row.Cell(n)`) dentro del módulo para usar los helpers `GetCellString` y `GetColumnIndexForOccurrence`.

Checklist mínimo para cambios en el módulo
- Actualizar la documentación en este README si cambia el comportamiento público.
- Incluir logs relevantes y evitar datos sensibles en los mensajes.
- Verificar que las excepciones se transforman en mensajes de usuario en español en los ViewModels.

---

Fecha: Agosto 2025
