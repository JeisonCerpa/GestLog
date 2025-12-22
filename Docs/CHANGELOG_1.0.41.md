# 📋 CHANGELOG - GestLog v[VERSION]

**Fecha de Liberación:** [COMPLETAR FECHA]  
**Anterior:** v1.0.X  
**Siguiente:** v1.0.X

---

## ✨ Nuevas Características

- 

---

## 🔧 Mejoras y Optimizaciones

- Mejorado: Combobox de años en vista de Historial ahora muestra solo años que existen en los datos. En lugar de años hardcodeados (últimos 4 años), se cargan dinámicamente desde la base de datos. Si no hay datos, usa fallback a años por defecto.
- Mejorado: Exportaciones a Excel ahora incluyen filtros automáticos en los encabezados de TODAS las hojas:
  - Periféricos: Filtros en hoja "Periféricos"
  - Equipos: Filtros en hojas "Equipos", "RAM", "Discos" y "Conexiones"
  - Datos Consolidados: Filtros en hojas "GenDesc" y "SpecProd_Interes"
  - Los usuarios pueden usar los filtros desplegables para ordenar y filtrar datos fácilmente.

---

## 🐛 Correcciones de Bugs

- Corregido: Campo "Usuario Asignado" en diálogo de edición de periféricos ahora se preselecciona correctamente con los datos existentes. El problema era un timing incorrecto en la carga de datos (la búsqueda de la persona se hacía antes de cargar la lista de disponibles).
- Corregido: Filtro dinámico del campo "Usuario Asignado" ahora funciona correctamente. Había una definición duplicada de la propiedad que evitaba que el filtro funcionara como autocompletado.

---

## 🎨 Cambios de UI/UX

- Mejorado: Exportaciones a Excel ahora muestran estados y sedes formateados correctamente:
  - Estados: "DadoDeBaja" → "Dado de Baja", "EnMantenimiento" → "En Mantenimiento", etc.
  - Sedes: "AdministrativaBarranquilla" → "Administrativa - Barranquilla"
  - Estados con colores: Verde (En Uso), Gris (Almacenado), Rojo (Dado de Baja)
  - Textos separados por mayúsculas para mejor legibilidad

---

## 📋 Notas de Desarrolladores

- Se cambió el método `BuscarPersonaConEquipoExistente()` a público en `PerifericoDialog.xaml.cs` para permitir su llamada desde el evento `Loaded` después de cargar las personas disponibles.
- El flujo ahora es: Loaded → CargarPersonasConEquipoAsync() → BuscarPersonaConEquipoExistente() (secuencial)
- Se agregó método `GetAvailableYearsAsync()` a `IPlanCronogramaService` e `PlanCronogramaService` para obtener años disponibles en las ejecuciones.
- El método `CargarAñosDisponiblesAsync()` en `HistorialEjecucionesViewModel` carga los años de forma asíncrona en el constructor y maneja fallbacks en caso de error o ausencia de datos.
- Los filtros automáticos en Excel se agregaron usando `SetAutoFilter()` de ClosedXML en:
  - `PerifericoExportService.cs`: Rango desde A1 hasta última fila/columna de periféricos
  - `EquiposInformaticosViewModel.cs`:
    - Hoja "Equipos": Rango desde A1 hasta número de equipos + 1
    - Hoja "RAM": Rango desde A1 hasta última fila con datos
    - Hoja "Discos": Rango desde A1 hasta última fila con datos
    - Hoja "Conexiones": Rango desde A1 hasta última fila con datos  - `ExcelExportService.cs`: 
    - Hoja "GenDesc": Rango desde A1 hasta última fila/columna
    - Hoja "SpecProd_Interes": Rango desde A1 hasta fila 1000/columna 10 (para soportar datos futuros)
- Se agregaron métodos de formateo para mejorar la presentación en Excel:
  - `FormatearEstado()` en `PerifericoExportService.cs`: Convierte estados enum a texto legible
  - `FormatearSedeEnum()` en `PerifericoExportService.cs`: Formatea sedes con separadores " - "
  - `FormatearEstadoEquipo()` en `EquiposInformaticosViewModel.cs`: Convierte estados de equipo a texto legible
  - `SepararPorMayusculas()`: Método auxiliar que separa texto por mayúsculas automáticamente

---

**Última actualización:** [FECHA]  
**Versión:** v[VERSION]
