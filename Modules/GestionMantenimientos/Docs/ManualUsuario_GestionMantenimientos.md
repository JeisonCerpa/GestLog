# Manual de Usuario — Módulo Gestión de Mantenimientos

## 1. Introducción
Este manual explica las acciones más comunes del módulo Gestión de Mantenimientos: entrar al módulo, crear y administrar equipos, registrar mantenimientos (programados y correctivos), gestionar seguimientos, exportar información y usar filtros. Las instrucciones están pensadas para el usuario final.

---

## 2. Acceder al módulo
1. Iniciar sesión en la aplicación.
2. En la vista principal abrir el menú "Herramientas".
3. Buscar en la lista o usar la caja de búsqueda dentro de "Herramientas" la opción "Gestión de Mantenimientos".
4. Seleccionar "Gestión de Mantenimientos" para entrar al módulo.
5. Al entrar al módulo verá pestañas en la parte superior: **Equipos**, **Cronogramas** y **Seguimientos**. La pestaña **Equipos** es la que se muestra por defecto.
6. Si no aparece la opción, contacte al administrador (puede faltarle el permiso `Acceder`).

---

## 3. Vista de Equipos
- Diseño y elementos principales en la pestaña **Equipos** (vista por defecto):
  1. Encabezado (header):
     - Icono representativo y título grande "Gestión de Equipos".
     - Subtítulo o descripción corta debajo del título (por ejemplo: "Listado y administración de equipos").
     - Mensaje de estado o notificaciones cortas alineadas a la derecha del header.

  2. Estadísticas rápidas (debajo del header):
     - Bloques con conteos: **Activos**, **En Mantenimiento**, **En Reparación**, **Inactivo**, **Dados de Baja**.
     - Cada bloque muestra el número y una etiqueta, con color distintivo según el estado.

  3. Barra de acciones principal (a la derecha de las estadísticas):
     - Botón **Actualizar** (ícono) — recarga la lista de equipos.
     - Botón **Exportar inteligente** (ícono) — exporta los elementos filtrados si existe un filtro activo, si no exporta todo.
     - Botón **Agregar nuevo equipo** (ícono o circular) — abre el formulario para crear un equipo.
     - Estos botones pueden mostrarse como iconos circulares y están habilitados/ deshabilitados según permisos del usuario.

  4. Filtros y controles rápidos (debajo de estadísticas):
     - Caja de búsqueda/filtrado (texto libre) para buscar por nombre, código o serial.
     - Toggle / switch **Mostrar dados de baja** a la derecha — cuando está activo muestra también los equipos marcados como dados de baja.

  5. Lista principal (DataGrid) — ocupa la mayor parte de la vista:
     - Columnas típicas: **Código**, **Nombre**, **Marca**, **Clasificación**, **Sede**, **Frecuencia Mtto**, **Estado**, y columna de acciones.
     - La columna **Estado** muestra un indicador coloreado (círculo) y el texto del estado.
     - Filas con equipos dados de baja se muestran con texto tachado y opacidad reducida.
     - Selección de fila permite abrir el detalle o ejecutar acciones.

  6. Acciones por fila:
     - Botón **Detalles** abre la vista de detalle del equipo.
     - Al abrir la vista de detalle encontrará las acciones: **Editar**, **Registrar correctivo**, **Ver historial** y **Dar de baja**. Las operaciones de edición y de dar de baja se realizan desde esta vista de detalle.
     - Al dar de baja se solicita confirmación mediante diálogo en la vista de detalle.

- Consejos para el usuario:
  - Pase el cursor sobre los botones para ver una descripción (tooltip) antes de activar una acción.
  - Las acciones sensibles (por ejemplo: "Dar de baja") mostrarán un cuadro de confirmación; revise el mensaje antes de confirmar.
  - Los estados de los equipos se muestran con colores y estilo visual: verde (Activo), ámbar (En mantenimiento), naranja (En reparación), gris (Inactivo) y tachado + opacidad reducida (Dados de baja). Use estas señales para identificar rápidamente la condición del equipo.
  - Si no está seguro de una acción, consulte primero el detalle del equipo antes de proceder.

---

### 3.1 Crear un equipo
1. Ir a la sección "Equipos" si no está ya en ella.
2. Hacer clic en "Agregar nuevo equipo".
3. Se abrirá la subvista/modal "Datos del Equipo" donde debe completar los campos obligatorios (Código, Nombre, Sede) y los opcionales.
4. Revisar y pulsar "Guardar".
5. Tras guardarlo, la ventana se cierra y el equipo aparece en la lista.

#### 3.1.1 Subvista: Datos del Equipo
- Campos principales:
  - Código (obligatorio, debe ser único).
  - Nombre (obligatorio).
  - Marca (editable/autocompletable).
  - Estado (desplegable).
  - Sede (obligatorio).
  - Fecha de Compra.
  - Precio (moneda COP).
  - Observaciones.
  - Frecuencia Mtto.
  - Clasificación (editable).
  - Comprado a (editable).

- Validaciones visibles y mensajes de error: campos obligatorios marcados con *; código duplicado muestra icono y texto; bordes rojos y mensajes para errores.

- Botones: Guardar (confirma y cierra) y Cancelar (descarta cambios).

- Flujo habitual: completar obligatorios → corregir errores si aparecen → Guardar → ver confirmación "Equipo guardado correctamente".

---

### 3.2 Editar datos de un equipo
1. Seleccionar el equipo en la lista.
2. Hacer clic en "Detalles" para abrir la ficha del equipo.
3. En la vista de Detalle pulsar "Editar" o "Modificar". Se abrirá el mismo formulario "Datos del Equipo" usado para crear equipos, con los campos ya completados.
4. Modificar los campos necesarios y pulsar "Guardar".
5. Tras guardar, la ventana se cierra y los cambios se reflejan en la lista.

---

### 3.3 Dar de baja un equipo
1. Seleccionar el equipo en la lista.
2. Hacer clic en "Detalles" para abrir la ficha del equipo.
3. En la vista de Detalle pulsar "Dar de baja" o "Eliminar" (según permisos).
4. Confirmar en el diálogo emergente.
Resultado: estado del equipo pasa a "Dado de baja" (no aparece en listados activos a menos que se filtre para verlo).

Nota: Si no tienes permiso `Eliminar`, la opción estará deshabilitada.

---

### 3.4 Registrar mantenimiento
1. En la pestaña **Equipos** abra la ficha del equipo haciendo clic en "Detalles".
2. Dentro de la vista de Detalle, pulse el botón "Registrar mantenimiento". Nota: esta acción, cuando se inicia desde la vista Equipos, se usa para registrar mantenimientos correctivos — el formulario se abre con el Tipo de Mantenimiento preseleccionado como "Correctivo" y la frecuencia ajustada automáticamente.
3. Complete el formulario. El modal tiene los siguientes campos (tal como aparecen en la interfaz "Registrar Mantenimiento"):
   - Código: lectura únicamente (prellenado desde la ficha del equipo).
   - Nombre: lectura únicamente (prellenado desde la ficha del equipo).
   - Fecha Realización: selector de fecha (DatePicker). Campo requerido.
   - Tipo Mtto: desplegable con los tipos disponibles (Preventivo, Correctivo, etc.). Campo requerido.
   - Responsable: texto libre para indicar la persona que realizó el trabajo. Campo requerido.
   - Costo (COP): campo numérico/moneda (formateado como COP).
   - Checklist: casillas rápidas para marcar items comunes (Revisión General, Limpieza, Ajustes).
   - Descripción: campo de texto grande para describir el trabajo o la falla. Campo requerido.
   - Observaciones: campo adicional para notas.
   - Frecuencia: desplegable (solo visible cuando el diálogo se abre en modo no restringido; desde "Equipos" el diálogo suele abrirse en modo restringido y esta opción queda oculta).

4. Botones:
   - "Cancelar": cierra el modal sin guardar.
   - "Guardar Cambios": valida y persiste el registro. Tras guardar el diálogo se cierra.

5. Resultado esperado: el mantenimiento queda creado y será visible en la pestaña **Seguimientos**, en el historial de la ficha del equipo y en la vista **Cronograma**.

---

### 3.5 Exportar Hoja de vida (desde Detalles del equipo)
1. Abra la ficha del equipo (Detalles) desde la lista de Equipos.
2. En la ficha localice el botón o el menú "Exportar" / "Hoja de vida".
3. Seleccione el formato deseado (PDF, Excel) y las opciones disponibles (por ejemplo: incluir historial completo, incluir notas).
4. Pulse "Exportar" y guarde el archivo en la ubicación deseada.
Resultado: se genera un documento con la Hoja de vida del equipo que incluye datos básicos, historial de mantenimientos y notas.

---

### 3.6 Exportar información
- Controles y comandos principales (resumen técnico corto para referencia):
  - Desde la vista **Equipos** hay un botón "Exportar inteligente" (comando: ExportarInteligenteAsync / ExportarEquiposFiltradosAsync). Exporta la lista visible (o todo si no hay filtros) a un archivo Excel (.xlsx) con el inventario, indicadores básicos y detalles seleccionados.
  - Desde la ficha de un equipo (Detalles) existe la acción "Exportar Hoja de vida" (comando: ExportarHojaVidaEquipo). Genera la Hoja de vida del equipo (historial de mantenimientos y notas) en Excel (.xlsx) y muestra confirmación al terminar.
  - En **Cronogramas** el botón Exportar ejecuta ExportarCronogramasAsync (o el comando asociado) y genera un informe Excel (.xlsx) con el cronograma anual y el detalle de seguimientos (KPIs incluidos).

- Pasos generales para exportar:
  1. Filtrar o seleccionar la vista que desea exportar (opcional).
  2. Pulsar el botón "Exportar" correspondiente.
  3. Seleccionar la ubicación y nombre del archivo en el diálogo de guardado.
  4. Confirmar y esperar a que el sistema genere el archivo; recibirá un mensaje de éxito al finalizar.

- Formatos: principal formato de exportación es Excel (.xlsx). Algunas exportaciones pueden ofrecer PDF según la opción disponible en la interfaz.

---

## 4. Vista Cronograma
La pestaña **Cronogramas** muestra un calendario tipo vista semanal con los mantenimientos programados por semana y permite exportar o revisar el detalle de cada semana.

### 4.1 Acceder a la vista
- Desde el módulo seleccione la pestaña **Cronogramas** en la parte superior.

### 4.2 Elementos principales
- Encabezado: título "Cronograma de Mantenimientos" y subtítulo "Vista general de mantenimientos programados".
- Selector de Año: control para elegir el año que desea ver en el cronograma.
- Botones principales (a la derecha del header):
  - Actualizar — recarga los datos del cronograma.
  - Exportar — genera un informe Excel (.xlsx) con el cronograma del año seleccionado, listado de seguimientos realizados (con estados y KPIs de desempeño), e indicadores resumidos.

#### Leyenda de colores de estados
Las semanas en el calendario usan los siguientes colores para indicar el estado de los mantenimientos:
- **Pendiente** — color claro (aún no iniciado).
- **Ejecutado** — color verde (completado correctamente).
- **Retrasado** — color ámbar/amarillo (pasó la fecha programada pero aún se puede ejecutar).
- **No Realizado** — color rojo (no se completó en la fecha programada).
- **Sin Mtto** — marcador transparente/punteado (semana sin mantenimientos asignados).

### 4.3 Vista semanal (calendario)
- Cada celda representa una semana y muestra:
  - Número de semana (por ejemplo: "Semana 12").
  - Rango de fechas de la semana.
  - Conteo de mantenimientos (por ejemplo: "Mantenimientos: 3").
- Al pasar el cursor sobre una semana verá el título y una lista rápida de los mantenimientos programados, o el mensaje "Sin mantenimientos programados" si la semana no tiene asignado ninguno.
- Botón "Ver Detalles": disponible en las semanas que tienen mantenimientos; permite abrir el detalle completo de la semana para revisar o registrar seguimientos.
- Indicadores visuales:
  - Las tarjetas de semana muestran un distintivo "ACTUAL" en la semana en curso.
  - El color de la semana indica el estado de los mantenimientos (consulte la leyenda de colores en la sección anterior).

#### 4.3.1 Subvista: Detalle de la Semana
Al hacer clic en el botón "Ver Detalles" de una semana se abre la ventana "Detalle de la Semana" que muestra información completa de los mantenimientos programados para esa semana.

**Encabezado de la ventana:**
- Título: "Detalle de la Semana"
- Información de la semana: número de semana y rango de fechas (por ejemplo: "Semana 12: 18 de marzo - 24 de marzo de 2026")

**Tabla de Estados de Mantenimiento:**
La tabla principal muestra todos los mantenimientos programados para la semana con las siguientes columnas:

- **Equipo**: nombre completo del equipo.
- **Código**: código identificador único del equipo.
- **Sede**: ubicación del equipo (por ejemplo: Bogotá, Medellín).
- **Tipo**: tipo de mantenimiento con insignia de color:
  - 🔧 **Preventivo** — insignia azul clara (mantenimiento preventivo programado).
  - ⚠️ **Correctivo** — insignia naranja (reparación de falla).
- **Frecuencia**: frecuencia de realización del mantenimiento (por ejemplo: Mensual, Trimestral).
- **Estado**: estado actual del mantenimiento con indicador visual:
  - ✅ **Realizado en Tiempo** — color verde (completado antes o en la fecha prevista).
  - ⏱️ **Realizado Fuera de Tiempo** — color verde oscuro (completado pero después de la fecha prevista).
  - ⚠️ **Atrasado** — color rojo (no completado, pasó la fecha programada).
  - ❌ **No Realizado** — color rojo (no se ejecutó en la fecha programada).
  - ⏸️ **Pendiente** — color gris (aún no iniciado).
- **Acción**: botón "Registrar" (disponible solo si el mantenimiento aún está pendiente y el usuario tiene permisos para registrarlo).

**Botón de acción:**
- Botón "Cerrar": cierra la ventana de detalle sin guardar cambios (cualquier cambio se guarda automáticamente al registrar un mantenimiento).

##### 4.3.1.1 Registrar mantenimiento programado (desde Detalle de la Semana)
Esta sección explica cómo registrar la ejecución de un mantenimiento **programado (preventivo)** desde la vista de detalle semanal. 

⚠️ **Importante**: Esta ventana es **SOLO para mantenimientos programados (preventivos)**, no para correctivos. Los correctivos se registran desde la pestaña Equipos (sección 3.4 de este manual).

**Cómo abrir el formulario:**
1. Desde la vista de Detalle de la Semana, haga clic en el botón **"Registrar"** en la columna Acción correspondiente al mantenimiento que desea registrar.
2. Se abre automáticamente la ventana "Registrar Mantenimiento" con los datos del equipo y el mantenimiento ya prellenados.

**Campos del formulario (en orden):**

- **Código**: lectura únicamente (muestra el código del equipo). No se puede modificar.
- **Nombre**: lectura únicamente (muestra el nombre del equipo). No se puede modificar.
- **Fecha Realización**: selector de fecha. Indique cuándo se realizó el mantenimiento. **Requerido**.
- **Tipo Mtto**: tipo de mantenimiento (Preventivo, Correctivo, etc.). Para mantenimientos desde esta vista, vendrá preseleccionado como **Preventivo**. **Requerido**.
- **Responsable**: nombre de la persona que realizó el mantenimiento (por ejemplo: "Juan Pérez"). **Requerido**.
- **Costo**: costo en pesos COP (si aplica). Puede dejarse vacío si no hay costo.
- **Checklist**: casillas para marcar items comunes:
  - Revisión General
  - Limpieza
  - Ajustes
  - Seleccione los que apliquen según el trabajo realizado.
- **Descripción**: campo de texto para describir el trabajo realizado, observaciones o hallazgos. **Requerido**.
- **Observaciones**: notas adicionales (opcional).
- **Frecuencia**: frecuencia de realización (por ejemplo: Mensual, Trimestral). Este campo puede no ser editable dependiendo de la configuración del mantenimiento.

**Botones de acción:**
- **"Guardar Cambios"**: valida el formulario y registra el mantenimiento. Si hay campos requeridos vacíos, aparecerá un mensaje de error indicando cuáles completar.
- **"Cancelar"** o **X (cerrar)**: descarta los cambios sin guardar.

**Resultado esperado:**
Tras hacer clic en "Guardar Cambios", la ventana se cierra y el mantenimiento queda registrado. El estado del mantenimiento en la tabla de detalle semanal cambiará a **"Realizado en Tiempo"** (si está dentro de la fecha programada) o **"Realizado Fuera de Tiempo"** (si se registra después de la fecha prevista). El cambio se refleja inmediatamente en la vista.

---

## 5. Vista de Seguimientos
La pestaña **Seguimientos** muestra el registro completo de todos los mantenimientos (correctivos y programados) que se han realizado. Es una vista de consulta histórica con estadísticas y filtros avanzados.

### 5.1 Acceder a la vista
- Desde el módulo seleccione la pestaña **Seguimientos** en la parte superior.

### 5.2 Elementos principales
- **Encabezado**: título "Seguimiento de Mantenimientos" con subtítulo "Registro y administración de mantenimientos".
- **Selector de Año**: control para elegir el año del cual desea ver los seguimientos registrados.

- **Estadísticas rápidas** (debajo del header):
  - Bloques con conteos: **Total**, **Pendientes**, **Ejecutados**, **Retrasados**, **Fuera de Tiempo**, **No Realizados**.
  - Cada bloque muestra el número y una etiqueta, con color distintivo según el estado.

- **Botones principales** (a la derecha de las estadísticas):
  - Actualizar — recarga la lista de seguimientos desde la base de datos.
  - Importar — permite importar seguimientos antiguos desde un archivo Excel.
  - Exportar — genera un informe Excel (.xlsx) con el listado de seguimientos (filtrados o todos) incluyendo estadísticas y detalles.

- **Filtros y controles**:
  - Caja de búsqueda/filtrado (texto libre): busca por código, nombre, tipo mantenimiento, responsable, fecha, semana, año o estado. Puede separar varios filtros con punto y coma (;).
  - Selector de fecha "Desde": filtra por fecha de registro desde la fecha seleccionada.
  - Selector de fecha "Hasta": filtra por fecha de registro hasta la fecha seleccionada.
  - Botón "Filtrar": aplica los filtros indicados.
  - Botón "Limpiar Filtros": elimina todos los filtros activos y muestra la lista completa.

- **Tabla principal** (DataGrid): muestra todos los seguimientos registrados con las siguientes columnas:
  - **Código**: código del equipo.
  - **Nombre**: nombre del equipo.
  - **Fecha Realizada**: cuándo se ejecutó el mantenimiento (formato: dd/MM/yyyy).
  - **Tipo Mtto**: tipo de mantenimiento (Preventivo, Correctivo, etc.).
  - **Descripción**: descripción del trabajo realizado.
  - **Responsable**: persona que realizó el mantenimiento.
  - **Costo**: costo del mantenimiento en pesos COP.
  - **Observaciones**: notas adicionales del registro.
  - **Fecha Registro**: cuándo se registró en el sistema (formato: dd/MM/yyyy).
  - **Semana**: número de la semana del año (Semana 1, Semana 2, etc.).
  - **Año**: año del seguimiento.
  - **Estado**: estado actual del mantenimiento con color distintivo:
    - Azul claro — Pendiente (aún no ejecutado).
    - Verde — Ejecutado (completado en la fecha programada).
    - Ámbar/Amarillo — Retrasado (pasó la fecha pero aún se puede ejecutar).
    - Naranja — Fuera de Tiempo (ejecutado después de la fecha programada).
    - Rojo — No Realizado (no se completó en la fecha programada).

- **Interacción con filas**: puede hacer clic en una fila para seleccionarla y revisar sus detalles completos si es necesario.

### 5.3 Cómo usar los filtros
1. **Filtro de texto**: escriba en la caja de búsqueda valores como "Código equipo", "Nombre equipo", "Preventivo", "Juan Pérez" u otro campo. Para múltiples criterios, sepárelos con punto y coma (;).
   - Ejemplo: `bomba; Preventivo; Juan` — busca registros que contengan "bomba" O "Preventivo" O "Juan".

2. **Filtro de fecha**: use los selectores "Desde" y "Hasta" para acotar el rango de fechas de registro. Haga clic en el selector y elija la fecha deseada.

3. **Aplicar filtros**: pulse el botón "Filtrar" para ejecutar la búsqueda con los criterios indicados. La tabla se actualizará mostrando solo los registros que coinciden.

4. **Limpiar filtros**: pulse "Limpiar Filtros" para eliminar todos los criterios y volver a ver la lista completa.

### 5.4 Exportar seguimientos
1. Opcionalmente, aplique los filtros deseados para exportar solo ciertos registros (si no aplica filtros, se exportan todos).
2. Pulse el botón **"Exportar"** (ícono ⭱).
3. Seleccione la ubicación y nombre del archivo en el diálogo de guardado.
4. Confirme y espere a que el sistema genere el archivo Excel (.xlsx).

**Contenido del export**:
- Listado completo de seguimientos con todas las columnas (Código, Nombre, Fecha, Tipo, Descripción, Responsable, Costo, etc.).
- Indicadores resumidos: total, pendientes, ejecutados, retrasados, fuera de tiempo, no realizados.
- El archivo incluye información histórica y estadísticas de desempeño (KPIs).

*Fin del manual.*
