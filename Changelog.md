## v1.3.3

- Gestión de Cartera — el BCC y el CC que se guardan en la configuración de correo son los que se usan al enviar. Antes, al cambiar el BCC (por ejemplo de un correo de pruebas al real), la pantalla mostraba el nuevo pero los correos seguían saliendo con copia oculta al anterior hasta reiniciar el programa.
- Se corrige el error "El subproceso que realiza la llamada no puede obtener acceso a este objeto" al guardar la configuración de correo, que dejaba la configuración cargada a medias aunque el log dijera que todo salió bien.
- Nuevo botón "Borrar configuración" en la ventana de configuración de correo: elimina de una vez el servidor, puerto, usuario, BCC, CC y la contraseña guardada en Windows, para volver a configurar desde cero.
- Al cerrar la ventana de configuración, los datos se releen del archivo guardado en lugar de copiarse campo por campo, que era la causa de que el BCC y el CC se quedaran atrás.

## v1.3.2

- Al cambiar la fecha de realización de un mantenimiento ya guardado, el registro se mueve a la semana que le corresponde en el cronograma. Antes la fecha se guardaba pero el mantenimiento se quedaba en la semana anterior.
- Se corrige el error "No se encontró el seguimiento a actualizar" al guardar cambios desde "Detalles de registro" cuando se abría desde el detalle de semana del cronograma.
- Agregar un seguimiento desde la vista de Seguimientos vuelve a funcionar: antes fallaba siempre con "Error al agregar seguimiento".
- Los mensajes de validación se muestran tal como son (por ejemplo "Solo se permite registrar mantenimientos en semanas anteriores o la actual") en lugar de un "Error al..." genérico.

## v1.3.1

- Los mantenimientos correctivos se registran en la fecha de realización que escribe la persona, no en la de hoy. Antes el registro quedaba archivado en la semana en que se guardaba, aparecía como "Pendiente" y desaparecía de la hoja de vida del equipo.
- La hoja de vida del equipo muestra la fecha de realización del mantenimiento, no la fecha en que se guardó el registro.
- Un correctivo ya no se pierde en el detalle de semana cuando el equipo tiene además un preventivo programado esa misma semana: el cronograma y el detalle muestran el mismo número de mantenimientos.
- "Detalles de registro" permite editar la fecha de realización, el responsable, el costo y las observaciones. Al cambiar la fecha, el mantenimiento se mueve a la semana correcta del cronograma. Antes el costo y las observaciones se descartaban al guardar sin avisar.
- Eliminar un mantenimiento borra solo ese registro. Antes borraba el historial completo de mantenimientos del equipo.

## v1.3.0

- Auditoría automática en todos los módulos: cada creación, modificación y eliminación queda registrada con quién la hizo, cuándo y qué cambió exactamente (por ejemplo "Sede: 'Administrativa - Barranquilla' → 'Taller - Barranquilla'"). Cubre equipos informáticos, periféricos, mantenimientos correctivos, equipos y mantenimientos ejecutados, cronogramas, vehículos y sus planes, y todo Identidad y Catálogos (usuarios, personas, cargos, roles, permisos y tipos de documento).
- Nueva pantalla "Auditoría" en Identidad y Catálogos: consulta del historial con filtros por tipo de registro, usuario, rango de fechas y búsqueda de texto. Cada evento se abre en una ventana de detalle con la información completa y opción de copiarla.
- Los registros identifican el equipo por su código y su nombre, no por el identificador interno de la base de datos, y usan los nombres de campo del negocio ("Sistema operativo", "Comprado a", "Usuario asignado").
- Solo se registran los campos que cambiaron realmente: un valor vacío que pasa a nulo ya no aparece como modificación.
- Las contraseñas nunca se escriben en el historial: el restablecimiento queda como acción propia, sin exponer datos cifrados.
- Los cronogramas generados automáticamente por el sistema no ensucian el historial: solo se registra quién los modifica después. Las importaciones masivas se resumen en una sola entrada en lugar de una por fila.
- Registrar mantenimientos en el cronograma ya no está limitado a la semana actual y la anterior: se puede registrar cualquier semana pasada. Por el momento todo registro queda como "Realizado en tiempo".

## v1.2.11

- Gestión de Cartera — "Probar configuración" ahora prueba de verdad: se conecta al servidor de correo y valida usuario y contraseña sin enviar ningún correo. Antes solo revisaba que los campos no estuvieran vacíos y daba "correcto" aunque la contraseña fuera incorrecta.
- Mensajes de error claros al enviar correos: contraseña o usuario incorrectos, destinatarios o BCC rechazados, y puerto/SSL incompatibles (la causa de la falla "Syntax error, command unrecognized" con el puerto 465) se explican en español en vez del mensaje técnico del servidor.
- Archivos abiertos: si el Excel de estado de cartera o el de clientes está abierto en Excel, se avisa explícitamente antes de procesar. Al generar los PDF, las empresas cuyo archivo no se pudo escribir (por estar abierto en un visor) ahora se reportan en el resumen; antes se saltaban en silencio y el usuario no se enteraba.
- Configuración de correo de Cartera unificada: servidor, puerto, SSL, usuario, contraseña, BCC y CC se guardan y leen desde un único lugar. Al reabrir la ventana de configuración todos los campos se repueblan, incluida la contraseña, que antes aparecía vacía.
- Gestión de Roles rediseñada con vista maestro-detalle y tema visual centralizado.
- Se eliminó la pantalla "Asignación de Permisos", redundante con la gestión de roles.

## v1.2.10

- Corrección para sedes con red inestable: ya no aparecen diálogos de error repetidos ("A Task's exception(s) were not observed... host no accesible"). Los errores de tareas en segundo plano ahora se registran en el log sin interrumpir al usuario; el estado de la conexión se sigue viendo en el indicador de la barra superior.

## v1.2.9

- Importación de Seguimientos corregida: ahora se respeta la columna Semana del archivo. Antes, si la fecha corregida caía en otra semana calendario, se creaba un registro duplicado en esa semana y el original quedaba sin actualizar.
- Las filas sin Fecha Realización que antes se descartaban en silencio ahora aparecen como ignoradas con su razón, tanto en el resumen de la importación como en los logs.

## v1.2.8

- Exportación de Seguimientos legible en pantalla: la columna Nombre ya no se ensancha sin límite (texto ajustado) y ahora se muestra la Sede de cada equipo.
- El archivo exportado ahora es directamente importable: puede exportar, corregir los datos en Excel y volver a importar el mismo archivo (los encabezados, fechas y el bloque de indicadores se reconocen automáticamente).
- Importación: si una fila "Realizado en tiempo" no tiene Fecha Realización, se usa su Fecha Registro en lugar de descartarla.
- Al exportar, la aplicación pregunta si desea abrir el archivo Excel generado.
- Al importar se muestra una barra de progreso sobre la tabla.
- "Descargar plantilla" ahora genera la plantilla con el mismo formato del export (12 columnas con Sede y una fila de ejemplo).
- La tabla de seguimientos en pantalla ahora incluye la columna Sede.

## v1.2.7

- Exportación de Seguimientos: nueva tabla "Cumplimiento por Sede" con conteos por estado, % cumplido/incumplido y costo total por sede (en el export de seguimientos y en la hoja de seguimientos del cronograma).
- Indicadores de cumplimiento corregidos: los pendientes ya no cuentan en el denominador y los atrasados ahora cuentan como incumplidos (antes se sumaban como realizados fuera de tiempo). El % de cumplimiento puede bajar respecto a reportes anteriores; el valor anterior estaba inflado.
- Se agregó la tarjeta "Incumplimiento" junto a "Cumplimiento" y el estado Atrasado ahora aparece con fila propia en el análisis por estado.

## v1.2.6

- Consolidación: se ampliaron los rangos de validación de FOB por tonelada (hasta 2000) para láminas, rollos, ángulos, canales y vigas, y se ajustó el manejo de las columnas de peso neto y valor FOB.
- Exportación de Seguimientos: las columnas Descripción y Observaciones ahora ajustan el texto a un ancho fijo (sin ensancharse), y al desplazarse se mantienen visibles el encabezado (filas 1 a 3) y las columnas Equipo y Nombre.

## v1.2.5

- Se mejoró el inicio de sesión y la comprobación de actualizaciones para que la aplicación abra más rápido, sin esperas innecesarias.