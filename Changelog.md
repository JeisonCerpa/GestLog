## [15-01-2026] - Mejoras en Exportación de Cronogramas y Corrección de Duplicados

### Corregidos
1. ✅ Botones duplicados en vista de Seguimientos
   - **Problema**: Los botones de "Actualizar" y "Exportar" estaban duplicados (una vez en cabecera y otra en área de estadísticas)
   - **Solución**: Eliminados los botones duplicados del área de estadísticas, mantenidos solo en cabecera
   - Archivo modificado: `Views/Seguimiento/SeguimientoView.xaml`

2. ✅ Caracteres UTF-8 corrompidos en Gestión de Mantenimientos y Gestión de Equipos Informáticos
   - **EquipoDialog.xaml.cs**: 30+ caracteres corregidos (Lógica, interacción, edición, máxima, código, asincrónica, etc.)
   - **EquipoDetalleModalWindow.xaml.cs**: Agregado mensaje de confirmación UTF-8 correcto para dar de baja equipo
   - **CronogramaExportService.cs**: Sin problemas detectados
   - **HojaVidaExportService.cs**: Sin problemas detectados
   - **DetallesEquipoInformaticoView.xaml.cs** (GestionEquiposInformaticos): Mensaje de confirmación de baja corregido a UTF-8

3. ✅ Duplicación de mantenimientos correctivos en vista semanal
   - **Problema**: Al registrar correctivos en vista de detalle semanal, aparecían duplicados (2x mismo correctivo)
   - **Causa**: Loop adicional en `SemanaViewModel.VerSemanaAsync()` agregaba correctivos ya existentes en la colección `estados`
   - **Solución**: Remover loop duplicado - `GetEstadoMantenimientosSemanaAsync()` ya retorna correctivos
   - Archivo modificado: `ViewModels/Cronograma/SemanaViewModel.cs`

4. ✅ Lógica de KPIs en análisis de cumplimiento por estado
   - Los correctivos ahora se muestran como categoría separada (no se cuentan en "Realizado en Tiempo")
   - El cumplimiento (%) se calcula solo para preventivos (correctivos no tienen tiempo estipulado)
   - Agregar "Correctivo" como fila en tabla de análisis de cumplimiento por estado
   - Color morado (#7E57C2) para identificar correctivos

### Mejorado
5. ✅ Anchos de columnas en hoja de Seguimientos (exportación de Cronogramas)
   - Cambio de anchos fijos a automáticos
   - Uso de `AdjustToContents()` para adaptación dinámica
   - Los textos "Realizado en Tiempo" y "Realizado Fuera de Tiempo" ahora se muestran completos
   - Columna G (Estado): Ancho adaptado automáticamente

6. ✅ Colores de badges en vista de Detalle de Semana
   - Correctivos: Morado (#7E57C2) - Fondo: #F3E5F5
   - Preventivos: Azul (#2196F3) - Fondo: #E3F2FD
   - Mejora visual para diferenciación clara de tipos de mantenimiento
   - Archivo modificado: `Views/Cronograma/SemanaDetalle/SemanaDetalleDialog.xaml`

7. ✅ Ajuste de columnas en DataGrid de Detalle de Semana
   - Cambio de anchos fijos a automáticos (`Width="Auto"`)
   - Aumento de MinWidth para todas las columnas (espaciado mejorado)
   - Nuevo estilo `DataGridCellStyle` con padding de 8px a los lados
   - Eliminación de amontonamiento visual en las celdas
   - Archivo modificado: `Views/Cronograma/SemanaDetalle/SemanaDetalleDialog.xaml`

8. ✅ Creación de SeguimientosExportService para exportación independiente
   - Nuevo archivo: `Services/Export/SeguimientosExportService.cs`
   - Nueva interfaz: `Interfaces/Export/ISeguimientosExportService.cs`
   - Extracción de lógica de exportación del ViewModel hacia servicio dedicado
   - Hoja Excel idéntica a la de CronogramaExportService (logo, KPIs, resumen, análisis por estado)
   - Registro en DI e integración en `SeguimientoViewModel` con inyección de dependencia
   - Archivos modificados: `Services/ServiceCollectionExtensions.cs`, `ViewModels/Seguimiento/SeguimientoViewModel.cs`

9. ✅ Corrección de caracteres UTF-8 en vistas de equipos
   - **Problema**: Mensajes mostraban caracteres corruptos en múltiples archivos
   - **Causa**: Encoding UTF-8 corrupto en archivos fuente de C# (xaml.cs)
   - **Solución**: Reescritura de archivos con encoding UTF-8 BOM correcto
   - **Archivos corregidos**:
     - `Modules/GestionMantenimientos/Views/Equipos/EquipoDialog.xaml.cs`: 30+ caracteres corregidos
     - `Modules/GestionMantenimientos/Views/Equipos/EquipoDetalleModalWindow.xaml.cs`: Mensaje confirmación agregado con UTF-8 correcto
     - `Modules/GestionEquiposInformaticos/Views/Equipos/DetallesEquipoInformaticoView.xaml.cs`: Mensaje confirmación corregido con UTF-8 correcto
   - Mensajes ahora muestran: "¿Está seguro que desea dar de baja el equipo..."

### Archivos Modificados/Creados
- `Modules/GestionMantenimientos/Services/Export/CronogramaExportService.cs` ✅ MODIFICADO
- `Modules/GestionMantenimientos/Services/Export/HojaVidaExportService.cs` ✅ MODIFICADO
- `Modules/GestionMantenimientos/Services/Export/SeguimientosExportService.cs` ✨ CREADO
- `Modules/GestionMantenimientos/Interfaces/Export/ISeguimientosExportService.cs` ✨ CREADO
- `Modules/GestionMantenimientos/ViewModels/Cronograma/SemanaViewModel.cs` ✅ MODIFICADO
- `Modules/GestionMantenimientos/Views/Cronograma/SemanaDetalle/SemanaDetalleDialog.xaml` ✅ MODIFICADO
- `Modules/GestionMantenimientos/Views/Seguimiento/SeguimientoView.xaml` ✅ MODIFICADO
- `Modules/GestionMantenimientos/Views/Equipos/EquipoDialog.xaml.cs` ✅ MODIFICADO
- `Modules/GestionMantenimientos/Views/Equipos/EquipoDetalleModalWindow.xaml.cs` ✅ MODIFICADO
- `Modules/GestionEquiposInformaticos/Views/Equipos/DetallesEquipoInformaticoView.xaml.cs` ✅ MODIFICADO
- `Services/Core/Logging/LoggingService.cs` ✅ MODIFICADO (agregado ISeguimientosExportService)
- `Modules/GestionMantenimientos/Services/ServiceCollectionExtensions.cs` ✅ MODIFICADO (registro DI)