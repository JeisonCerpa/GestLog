## [15-01-2026] - Mejoras en Exportación de Cronogramas y Corrección de Duplicados

### Corregidos
1. ✅ Caracteres UTF-8 corrompidos en exportaciones de Gestión de Mantenimientos
   - CronogramaExportService.cs: 8 caracteres corregidos
   - HojaVidaExportService.cs: 24 caracteres corregidos

2. ✅ Duplicación de mantenimientos correctivos en vista semanal
   - **Problema**: Al registrar correctivos en vista de detalle semanal, aparecían duplicados (2x mismo correctivo)
   - **Causa**: Loop adicional en `SemanaViewModel.VerSemanaAsync()` agregaba correctivos ya existentes en la colección `estados`
   - **Solución**: Remover loop duplicado - `GetEstadoMantenimientosSemanaAsync()` ya retorna correctivos
   - Archivo modificado: `ViewModels/Cronograma/SemanaViewModel.cs`

3. ✅ Lógica de KPIs en análisis de cumplimiento por estado
   - Los correctivos ahora se muestran como categoría separada (no se cuentan en "Realizado en Tiempo")
   - El cumplimiento (%) se calcula solo para preventivos (correctivos no tienen tiempo estipulado)
   - Agregar "Correctivo" como fila en tabla de análisis de cumplimiento por estado
   - Color morado (#7E57C2) para identificar correctivos

### Mejorado
4. ✅ Anchos de columnas en hoja de Seguimientos (exportación de Cronogramas)
   - Cambio de anchos fijos a automáticos
   - Uso de `AdjustToContents()` para adaptación dinámica
   - Los textos "Realizado en Tiempo" y "Realizado Fuera de Tiempo" ahora se muestran completos
   - Columna G (Estado): Ancho adaptado automáticamente

5. ✅ Colores de badges en vista de Detalle de Semana
   - Correctivos: Morado (#7E57C2) - Fondo: #F3E5F5
   - Preventivos: Azul (#2196F3) - Fondo: #E3F2FD
   - Mejora visual para diferenciación clara de tipos de mantenimiento
   - Archivo modificado: `Views/Cronograma/SemanaDetalle/SemanaDetalleDialog.xaml`

### Archivos Modificados
- `Modules/GestionMantenimientos/Services/Export/CronogramaExportService.cs`
- `Modules/GestionMantenimientos/Services/Export/HojaVidaExportService.cs`
- `Modules/GestionMantenimientos/ViewModels/Cronograma/SemanaViewModel.cs`
- `Modules/GestionMantenimientos/Views/Cronograma/SemanaDetalle/SemanaDetalleDialog.xaml` ✅ NUEVO