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

---

## 🐛 Correcciones de Bugs

- Corregido: Campo "Usuario Asignado" en diálogo de edición de periféricos ahora se preselecciona correctamente con los datos existentes. El problema era un timing incorrecto en la carga de datos (la búsqueda de la persona se hacía antes de cargar la lista de disponibles).
- Corregido: Filtro dinámico del campo "Usuario Asignado" ahora funciona correctamente. Había una definición duplicada de la propiedad que evitaba que el filtro funcionara como autocompletado.

---

## 🎨 Cambios de UI/UX

- 

---

## 📋 Notas de Desarrolladores

- Se cambió el método `BuscarPersonaConEquipoExistente()` a público en `PerifericoDialog.xaml.cs` para permitir su llamada desde el evento `Loaded` después de cargar las personas disponibles.
- El flujo ahora es: Loaded → CargarPersonasConEquipoAsync() → BuscarPersonaConEquipoExistente() (secuencial)
- Se agregó método `GetAvailableYearsAsync()` a `IPlanCronogramaService` e `PlanCronogramaService` para obtener años disponibles en las ejecuciones.
- El método `CargarAñosDisponiblesAsync()` en `HistorialEjecucionesViewModel` carga los años de forma asíncrona en el constructor y maneja fallbacks en caso de error o ausencia de datos.

---

**Última actualización:** [FECHA]  
**Versión:** v[VERSION]
