# 📋 CHANGELOG - GestLog v1.0.42

**Fecha de Liberación:** 05/01/2026  
**Anterior:** v1.0.41  
**Siguiente:** v1.0.43

---

## 📝 Release notes (resumen para usuarios)

- Se corrigió un error que en algunos casos mostraba registros duplicados en el Historial de Ejecuciones.
- Mayor estabilidad al cargar el Historial: se evitaron cargas dobles y condiciones de carrera.
- Correcciones menores: exportación CSV y actualización segura de equipos.

---

## 📋 Notas de Desarrolladores (detalladas)

- Cambios principales:
  - Se añadió serialización de cargas en `HistorialEjecucionesViewModel` (uso de `SemaphoreSlim`) y deduplicación por `EjecucionId` para prevenir duplicados visuales.
  - Se aplicó `AsSplitQuery()` a consultas que incluyen múltiples colecciones para mitigar `MultipleCollectionIncludeWarning` y evitar productos cartesianos.
  - En `Startup.Database.cs` la advertencia `MultipleCollectionIncludeWarning` se eleva a excepción en `Development` para facilitar diagnóstico; en entornos no-Development se ignora para reducir ruido en logs.
  - Corrección de compilación: se arregló la redeclaración `CS0136` de `equipoRecargado`.
  - Corrección en `EscapeCsv` para manejo correcto de saltos de línea.

- Archivos modificados (resumen):
  - `Modules/GestionEquiposInformaticos/ViewModels/Mantenimiento/HistorialEjecucionesViewModel.cs`
  - `Modules/GestionEquiposInformaticos/Views/Mantenimiento/HistorialEjecucionesView.xaml.cs`
  - `Modules/GestionEquiposInformaticos/ViewModels/Equipos/EquiposInformaticosViewModel.cs`
  - `Modules/GestionEquiposInformaticos/ViewModels/Equipos/DetallesEquipoInformaticoViewModel.cs`
  - `Modules/GestionEquiposInformaticos/ViewModels/Equipos/AgregarEquipoInformaticoViewModel.cs`
  - `Startup.Database.cs`

- Verificación sugerida:
  1. Abrir Gestión de Equipos → Historial.
  2. Seleccionar Año = 2026 → confirmar que aparece exactamente 1 registro si en BD hay 1.
  3. Pulsar 'Actualizar' o cambiar año varias veces rápidamente → confirmar que no aparecen duplicados.
  4. Ejecutar la app en `Development` y verificar que no se produce `MultipleCollectionIncludeWarning` (si aparece, ahora saltará como excepción con stack trace).

- Estado:
  - Compilación local: exitosa.
  - Pendiente: validar comportamiento en entornos reales y preparar PR/commit si se desea.

---

**Última actualización:** 05/01/2026  
**Versión:** v1.0.42
