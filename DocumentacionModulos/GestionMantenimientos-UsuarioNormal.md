# Gestión de Mantenimientos - Guía para Usuario Normal

Esta guía explica cómo usar el módulo de Gestión de Mantenimientos en GestLog. Aquí aprenderás a registrar mantenimientos, consultar el cronograma y dar seguimiento a las intervenciones.

---

## Funcionalidades principales
- Registrar mantenimientos realizados a los equipos
- Consultar el cronograma de mantenimientos programados
- Dar seguimiento y consultar historial de intervenciones
- Visualizar el estado y detalles de cada equipo
- Descargar reportes (si está habilitado)

---

## Secciones del módulo

### 1. Equipos
Consulta el estado actual de cada equipo (Activo, En mantenimiento, En reparación, Dado de baja, Inactivo), filtra y busca equipos, visualiza detalles e historial de mantenimientos. Desde aquí puedes registrar mantenimientos correctivos.

### 2. Cronograma
Visualiza el calendario semanal de mantenimientos programados. Consulta el detalle de cada semana y exporta el cronograma si tienes permisos.

### 3. Seguimiento
Consulta el historial de intervenciones, agrega comentarios, adjunta archivos o fotos (si está habilitado) y descarga reportes de seguimiento.

---

## Sección: Equipos

### 1. Agregar nuevo equipo
1. Ve al módulo "Gestión de Mantenimientos" y selecciona la sección "Equipos".
2. Haz clic en el botón "Nuevo" para agregar un equipo.
3. Se abrirá el formulario de alta de equipo. Completa los siguientes campos:
   - Código
   - Nombre
   - Marca
   - Estado (ejemplo: Activo, En mantenimiento, Dado de baja, etc.)
   - Sede
   - Fecha de compra
   - Precio
   - Observaciones (opcional)
   - Frecuencia de mantenimiento
4. Revisa los datos y haz clic en "Guardar".
5. El sistema mostrará un mensaje de éxito y el equipo aparecerá en la lista.

### 1.1 Editar equipo existente
1. En la sección "Equipos", busca y selecciona el equipo que deseas modificar.
2. Haz clic en el botón "Editar" junto al equipo.
3. Se abrirá el formulario de edición con los mismos campos que al agregar:
   - Código
   - Nombre
   - Marca
   - Estado
   - Sede
   - Fecha de compra
   - Precio
   - Observaciones (opcional)
   - Frecuencia de mantenimiento
4. Realiza los cambios necesarios y haz clic en "Guardar".
5. El sistema mostrará un mensaje de éxito y los cambios se reflejarán en la lista de equipos.

### 2. Dar de baja un equipo
1. En la lista de equipos, busca el equipo que deseas dar de baja.
2. Haz clic en el botón "Dar de baja" (ícono de papelera o texto "Dar de baja").
3. Confirma la acción en el cuadro de diálogo.
4. El equipo aparecerá con opacidad baja y texto tachado, y no podrá recibir nuevos mantenimientos ni ser editado.
5. **Advertencia:** Dar de baja un equipo elimina futuros cronogramas o planes de mantenimiento asociados a ese equipo. Si lo necesitas, el administrador puede reactivar el equipo posteriormente. Verifica que realmente deseas dar de baja el equipo antes de confirmar.

### 3. Importar y exportar equipos
- **Importar:**
  1. Haz clic en el botón "Importar" en la sección de equipos.
  2. Selecciona el archivo Excel (.xlsx) con la lista de equipos.
  3. Revisa el resumen de importación y confirma.
  4. Los equipos se agregarán automáticamente y verás un mensaje de éxito.
- **Exportar:**
  1. Haz clic en el botón "Exportar".
  2. El sistema generará un archivo Excel (.xlsx) con la información actual de los equipos.
  3. Descarga el archivo generado para consultarlo o compartirlo.

### 4. Registrar mantenimiento correctivo
1. En la lista de equipos, identifica el equipo que requiere mantenimiento correctivo.
2. Haz clic en el botón "Registrar mantenimiento" junto al equipo.
3. Se abrirá el formulario de mantenimiento correctivo. Completa los siguientes campos:
   - Código (se muestra automáticamente)
   - Nombre (se muestra automáticamente)
   - Fecha de realización (se preselecciona la fecha actual, puedes modificarla si es necesario)
   - Tipo de mantenimiento (se preselecciona "Correctivo")
   - Descripción del problema y solución
   - Responsable
   - Costo (si aplica)
   - Observaciones (opcional)
4. No es necesario llenar la frecuencia de mantenimiento.
5. Haz clic en "Guardar".
6. El sistema actualizará el historial del equipo y mostrará un mensaje de éxito.

---

## Sección: Cronograma

### 1. Consultar cronograma de mantenimientos
1. Ve al módulo "Gestión de Mantenimientos" y selecciona la sección "Cronograma".
2. Selecciona el año en el ComboBox para visualizar el calendario semanal.
3. Observa el cronograma con los mantenimientos programados para cada equipo.

### 2. Ver detalle de semana y registrar mantenimiento
1. Haz clic en la semana que deseas consultar.
2. Se abrirá el detalle de la semana, mostrando los equipos programados, su estado (pendiente/realizado), frecuencia y responsable.
3. Para cada equipo pendiente, haz clic en el botón "Registrar" para abrir el formulario de mantenimiento.
4. Completa los datos requeridos y guarda el registro. El sistema actualizará el estado a realizado y mostrará el resultado en el detalle de la semana.

### 3. Exportar cronograma
1. Haz clic en el botón "Exportar" en la sección de cronograma.
2. El sistema generará un archivo Excel (.xlsx) con el cronograma del año seleccionado.
3. Descarga el archivo para consultarlo o compartirlo.

---

## Sección: Seguimiento

### 2. Consultar el historial de mantenimientos
1. Ve al módulo "Gestión de Mantenimientos" y selecciona la sección "Seguimiento".
2. Busca el equipo o mantenimiento que deseas consultar usando los filtros disponibles (por nombre, estado, fecha, etc.).
3. Visualiza el historial de intervenciones y mantenimientos realizados, incluyendo fechas, tipo de mantenimiento, responsable, costo y observaciones.

### 3. Descargar reportes de seguimiento
1. Haz clic en el botón "Descargar reporte" en la sección de seguimiento (si está habilitado).
2. Puedes exportar el historial completo o aplicar filtros (por nombre, estado, fecha, etc.) y exportar solo los resultados filtrados.
3. El sistema generará un archivo Excel (.xlsx) con la información seleccionada.
4. Descarga el archivo para consultarlo o compartirlo.

---

## Permisos y feedback visual
- Solo puedes realizar acciones para las que tienes permisos.
- Los botones se habilitan/deshabilitan según tus permisos y se muestran con opacidad baja si no tienes acceso.
- Todos los mensajes y advertencias se muestran en español.
- Si falta configuración, verás advertencias claras en la parte superior.

---

## Recomendaciones
- Verifica siempre los datos antes de registrar información.
- Si tienes dudas sobre errores, revisa los mensajes en pantalla o consulta al soporte.
- No modifiques los datos directamente en la base de datos.
- Si no tienes permisos para alguna acción, comunícate con el administrador.

---

## Aclaración sobre errores
Si experimentas algún error inesperado, el sistema muestra mensajes que no comprendes, o notas comportamientos extraños, comunícate con el administrador o el área de soporte. El módulo puede estar en fase "beta" y estamos atentos a solucionar cualquier problema. El equipo de soporte te ayudará a reproducir el error, explicarte la causa y guiarte en la solución.

---

Última actualización: 29/09/2025
