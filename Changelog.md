# Gestión de Equipos Informáticos - Cambios para usuarios

Fecha: 21 de enero de 2026

## Mejoras

- Mejora en la exportación de mantenimientos: formato y diseño actualizados (SST-F-83 — Versión 4) para facilitar la lectura y el intercambio.

## Implementación

- Trazabilidad añadida para mantenimientos "No Realizado": los mantenimientos no ejecutados quedan registrados y claramente identificados tanto en la exportación como en el historial de ejecuciones.
- Visual: las filas correspondientes a mantenimientos "No Realizado" se muestran en rojo claro para facilitar su identificación.
- Nuevo: al crear un usuario, el sistema genera automáticamente una contraseña temporal y envía un correo de bienvenida con las credenciales e instrucciones. El usuario deberá cambiar esa contraseña en su primer acceso.


## Arreglos

- Evitado duplicado de registros automáticos "No Realizado" al iniciar el sistema.
- Corrección visual: los items "No Realizado" ahora se distinguen claramente de los atrasados.
- Corregido: fallo que provocaba errores al eliminar usuarios en algunas condiciones; la operación ahora se realiza de forma segura y confiable.
- Corregido: en el diálogo de equipos los desplegables de Marca, Clasificación y "Comprado a" ahora muestran inmediatamente las opciones al abrirse y permiten buscar o añadir rápidamente nuevas entradas.
- **Rediseño visual completo del diálogo "Datos del Equipo":** 
  - ✨ Interfaz modernizada y más intuitiva
  - 📋 Secciones claramente organizadas con iconos: Información Básica, Información de Compra, Clasificación y Proveedor, Observaciones
  - 📏 Campos de entrada más grandes y legibles (altura mejorada a 40px)
  - 📐 Layout organizado en grid de 2 columnas para mejor aprovechamiento del espacio
  - 🎨 Colores más modernos y elegantes en los inputs
  - ✨ Espaciado generoso entre elementos para mejor legibilidad
  - 🎯 Footer con botones de acción claramente diferenciados
  - 💫 Sombras y efectos visuales mejorados

## Notas

- Se recomienda ejecutar una exportación de prueba y una compilación completa para validar colores, merges y trazabilidad end-to-end.
- Pendiente: pruebas de rendimiento con hojas grandes y verificación final de que los registros marcados coincidan con la base de datos.
