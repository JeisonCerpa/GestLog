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