# Gestión de Equipos Informáticos - Guía para Usuario Normal

Esta guía explica el uso del módulo Gestión de Equipos Informáticos en GestLog. Aquí encontrarás el paso a paso para agregar, editar, dar de baja, importar/exportar y consultar equipos informáticos.

---

## Funcionalidades principales
- Agregar y editar equipos informáticos
- Dar de baja equipos informáticos
- Importar y exportar equipos en formato Excel (.xlsx)
- Consultar estado y detalles de cada equipo

---

## Secciones del módulo

- **Equipos**: Gestión y consulta de todos los equipos informáticos registrados.
- **Cronograma**: Visualización y administración de los mantenimientos programados para los equipos.
- **Historial**: Consulta del historial de intervenciones, mantenimientos y cambios realizados en los equipos.
- **Periféricos**: Gestión y consulta de dispositivos periféricos asociados a los equipos informáticos (ej. impresoras, monitores, teclados, etc.).

---

## Tutoriales

### 1. Agregar nuevo equipo informático
1. Ve a la sección "Equipos" en el módulo de Gestión de Equipos Informáticos.
2. Haz clic en el botón "Nuevo" para agregar un equipo.
3. Completa el formulario con los siguientes campos:
   - Código
   - Código Anydesk
   - Usuario Asignado (ComboBox con filtro)
   - Estado (preseleccionado "Activo")
   - Costo
   - Fecha de compra
   - Sede
   - Observaciones
4. Haz clic en el botón "Obtener campos automáticos" para llenar automáticamente:
   - Nombre del equipo
   - Sistema operativo
   - Serial
   - Marca
   - Procesador
   - Modelo
   - Memoria RAM: Tipo, Capacidad, Marca, Frecuencia, Observaciones (se llena con serial, nombre de pieza o modelo)
   - Discos: Tipo, Capacidad, Marca, Modelo
   - Conexiones de red: Adaptador, IPv4, Gateway, MAC, Máscara
5. En la sección "Asignar periféricos":
   - Visualiza los periféricos asignados y los disponibles.
   - Haz clic en "Asignar" para otorgar el periférico al equipo.
6. Revisa todos los datos y haz clic en "Guardar".
7. El sistema mostrará un mensaje de éxito y el equipo aparecerá en la lista.

### 1.1 Editar equipo informático
1. En la sección "Equipos", busca y selecciona el equipo que deseas modificar.
2. Haz clic en el botón "Editar" junto al equipo.
3. El formulario se llenará automáticamente con los datos actuales del equipo.
4. Puedes modificar los campos manuales:
   - Código
   - Código Anydesk
   - Usuario Asignado (ComboBox con filtro)
   - Estado
   - Costo
   - Fecha de compra
   - Sede
   - Observaciones
5. Si lo requieres, haz clic en el botón "Obtener campos automáticos" para actualizar:
   - Nombre del equipo
   - Sistema operativo
   - Serial
   - Marca
   - Procesador
   - Modelo
   - Memoria RAM: Tipo, Capacidad, Marca, Frecuencia, Observaciones
   - Discos: Tipo, Capacidad, Marca, Modelo
   - Conexiones de red: Adaptador, IPv4, Gateway, MAC, Máscara
6. En la sección "Asignar periféricos":
   - Visualiza los periféricos asignados y los disponibles.
   - Haz clic en "Asignar" para otorgar nuevos periféricos o quitar los que ya no correspondan.
7. Revisa los datos y haz clic en "Guardar".
8. El sistema mostrará un mensaje de éxito y los cambios se reflejarán en la lista de equipos.

### 2. Visualizar equipos informáticos

1. Los equipos se muestran en una tabla (DataGrid) con las siguientes columnas:
   - Código
   - Nombre de equipo
   - Usuario asignado
   - Marca
   - Sede
   - Estado
   - Botón de acción ("Ver detalles")

2. **Estado:**
   - Activo: Verde
   - En mantenimiento: Ámbar
   - En reparación: Naranja/ámbar oscuro
   - Dado de baja: Gris muy claro, opacidad baja, texto tachado
   - Inactivo: Gris medio/oscuro, opacidad 0.85, sin tachado
   - Los colores y estilos ayudan a identificar rápidamente el estado de cada equipo.

3. **Botón "Ver detalles":**
   - Haz clic para abrir una ventana con todos los detalles del equipo: periféricos asignados, memoria RAM, discos, conexiones de red, sistema operativo, procesador, modelo, observaciones, etc.

4. **ToggleButton "Ver dados de baja":**
   - Permite alternar la visualización de los equipos dados de baja en la tabla.
   - Al activarlo, se muestran los equipos dados de baja con opacidad baja y texto tachado.
   - Al desactivarlo, los equipos dados de baja se ocultan del DataGrid.

### 3. Importar y exportar equipos informáticos

**Importar equipos:**
1. Haz clic en el botón "Importar" en la sección de equipos.
2. Selecciona el archivo Excel (.xlsx) con la lista de equipos informáticos.
3. Revisa el resumen de importación que muestra los equipos detectados y posibles advertencias de formato.
4. Confirma la importación; los equipos se agregarán automáticamente y verás un mensaje de éxito.
5. Si el archivo tiene errores de formato o columnas faltantes, el sistema mostrará advertencias claras en español.

**Exportar equipos:**
1. Haz clic en el botón "Exportar" en la sección de equipos.
2. Puedes exportar todos los equipos o aplicar filtros (por código, nombre, usuario asignado, estado, sede, etc.) y exportar solo los resultados filtrados.
3. El sistema generará un archivo Excel (.xlsx) con la información actual o filtrada de los equipos informáticos.
4. Descarga el archivo generado para consultarlo, compartirlo o archivarlo.

### 4. Cronograma de mantenimientos

1. En la sección "Cronograma" puedes visualizar y gestionar los planes de mantenimiento para los equipos informáticos.

2. **ComboBox de semanas y años:**
   - Selecciona el año en el ComboBox para filtrar el cronograma por año.
   - Selecciona la semana en el ComboBox para ver los planes programados en ese periodo.
   - El cronograma se actualiza automáticamente según la selección.

3. **Crear plan de mantenimiento:**
   - Haz clic en el botón "Crear plan" o "Agregar mantenimiento".
   - Selecciona el código del equipo para el cual deseas crear el plan.
   - Elige los días de ejecución (de lunes a viernes) usando el selector correspondiente.
   - En la lista de tareas, puedes agregar nuevas tareas o eliminar las existentes según las necesidades del mantenimiento.
   - Completa el formulario con los datos requeridos: tipo de mantenimiento, responsable, observaciones, etc.
   - Guarda el plan; aparecerá en el cronograma de la semana seleccionada.

4. **Gestionar planes de mantenimiento:**
   - Puedes eliminar planes existentes desde el cronograma.
   - Haz clic en el botón correspondiente para eliminar el plan y confirma la acción.
   - También puedes activar o desactivar planes según corresponda usando el botón de estado.
   - El estado de cada plan se muestra con colores y estilos según su tipo y estado (pendiente, realizado, cancelado, etc.).

5. **Exportar cronograma:**
   - Haz clic en el botón "Exportar" para descargar el cronograma filtrado por año y semana en formato Excel (.xlsx).

6. **Botón "Ejecutar plan":**
   - Para cada plan programado en el cronograma, encontrarás el botón "Ejecutar plan".
   - Haz clic en este botón para marcar el plan como realizado y registrar la ejecución.
   - Se abrirá un formulario donde puedes ingresar detalles de la ejecución: fecha real, observaciones, responsable, y cualquier información relevante.
   - Selecciona las tareas que realmente se realizaron durante la ejecución del plan.
   - Haz clic en "Guardar" para registrar la ejecución y las tareas realizadas.
   - Al guardar, el estado del plan cambiará a "realizado" y se actualizará en el cronograma.
   - Si el plan ya fue ejecutado, el botón mostrará un checkmark (✔️). Al hacer clic en el checkmark, se abrirá una ventana con el resumen de la ejecución del plan: fecha, responsable, tareas realizadas y observaciones.

### 5. Historial de mantenimientos y ejecuciones

1. En la sección "Historial" puedes consultar todo lo realizado sobre los equipos informáticos: mantenimientos, ejecuciones de planes y cambios relevantes.
2. El historial muestra una lista con los registros de cada acción, incluyendo:
   - Fecha
   - Equipo
   - Responsable
   - Tipo de mantenimiento o acción
   - Estado (realizado, pendiente, cancelado, etc.)
   - Observaciones
3. Para cada ejecución de plan, puedes ver el resumen detallado: fecha de ejecución, responsable, tareas realizadas y observaciones.
4. El historial es solo de consulta; no se pueden modificar ni eliminar registros desde esta sección.
5. Puedes aplicar filtros por equipo, fecha, responsable o estado para encontrar registros específicos.
6. Si necesitas exportar el historial, utiliza el botón "Exportar" para descargar los registros filtrados o completos en formato Excel (.xlsx).

### 6. Gestión de periféricos

La sección "Periféricos" permite administrar todos los dispositivos periféricos asociados a los equipos informáticos (impresoras, monitores, teclados, mouse, etc.).

#### 6.1 Inventario de periféricos
1. Accede a la sección "Periféricos" desde el menú principal del módulo.
2. Se muestra una tabla (DataGrid) con los periféricos registrados, incluyendo:
   - Código
   - Nombre
   - Tipo (impresora, monitor, teclado, etc.)
   - Marca
   - Modelo
   - Estado (Disponible, Asignado, Dado de baja)
   - Equipo asignado (si aplica)
   - Botones de acción (Ver detalles, Editar, Eliminar, Asignar/Desasignar)
3. Puedes filtrar la lista por tipo, estado, marca, modelo o equipo asignado.

#### 6.2 Agregar nuevo periférico
1. Haz clic en el botón "Nuevo periférico" para agregar uno nuevo.
2. Completa el formulario con los siguientes campos:
   - Código
   - Nombre
   - Tipo
   - Marca
   - Modelo
   - Estado (preseleccionado "Disponible")
   - Observaciones
3. Revisa los datos y haz clic en "Guardar". El periférico aparecerá en el inventario.

#### 6.3 Editar periférico
1. Busca el periférico en la tabla y haz clic en el botón "Editar".
2. El formulario se llenará con los datos actuales del periférico.
3. Modifica los campos necesarios y haz clic en "Guardar" para actualizar la información.

#### 6.4 Eliminar periférico
1. Haz clic en el botón "Eliminar" junto al periférico que deseas dar de baja.
2. Confirma la acción en la ventana emergente. Advertencia: Esta acción es irreversible y el periférico no podrá ser asignado a equipos.
3. El periférico se marcará como "Dado de baja" y aparecerá con opacidad baja y texto tachado en el inventario.

#### 6.5 Asignar y desasignar periféricos a equipos
1. En la columna "Equipo asignado" o desde el botón "Asignar", selecciona el equipo informático al que deseas asociar el periférico.
2. Solo los periféricos con estado "Disponible" pueden ser asignados.
3. Al asignar, el estado del periférico cambia a "Asignado" y se muestra el equipo correspondiente.
4. Para desasignar, haz clic en el botón "Desasignar"; el periférico volverá a estado "Disponible".
5. Advertencia: No se puede eliminar un periférico que esté asignado a un equipo. Primero debe ser desasignado.

#### 6.6 Exportar inventario de periféricos
1. Haz clic en el botón "Exportar" para descargar el inventario filtrado o completo en formato Excel (.xlsx).
2. Puedes aplicar filtros antes de exportar para obtener solo los periféricos de interés.

#### Recomendaciones y advertencias
- Revisa siempre el estado antes de asignar o eliminar periféricos.
- Los periféricos dados de baja no pueden ser asignados ni editados.
- Utiliza los filtros para encontrar rápidamente periféricos por tipo, estado o equipo asignado.
- Antes de eliminar un periférico, asegúrate de que no esté asignado a ningún equipo.
- La exportación permite respaldar y compartir el inventario fácilmente.
