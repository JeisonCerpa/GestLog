## [15-01-2026] - Mejoras en Exportación de Cronogramas

### Corregidos
1. ✅ Caracteres UTF-8 corrompidos en exportaciones de Gestión de Mantenimientos
   - CronogramaExportService.cs: 8 caracteres corregidos
   - HojaVidaExportService.cs: 24 caracteres corregidos

2. ✅ Lógica de KPIs en análisis de cumplimiento por estado
   - Los correctivos ahora se muestran como categoría separada (no se cuentan en "Realizado en Tiempo")
   - El cumplimiento (%) se calcula solo para preventivos (correctivos no tienen tiempo estipulado)
   - Agregar "Correctivo" como fila en tabla de análisis de cumplimiento por estado
   - Color morado (#7E57C2) para identificar correctivos

### Mejorado
3. ✅ Anchos de columnas en hoja de Seguimientos (exportación de Cronogramas)
   - Cambio de anchos fijos a automáticos
   - Uso de `AdjustToContents()` para adaptación dinámica
   - Los textos "Realizado en Tiempo" y "Realizado Fuera de Tiempo" ahora se muestran completos
   - Columna G (Estado): Ancho adaptado automáticamente

### Archivos Modificados
- `Modules/GestionMantenimientos/Services/Export/CronogramaExportService.cs`
- `Modules/GestionMantenimientos/Services/Export/HojaVidaExportService.cs`