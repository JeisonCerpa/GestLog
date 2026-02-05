# Gestión de Equipos Informáticos - Cambios para usuarios

## 📦 Versión 1.0.46

Fecha: 4 de febrero de 2026

### Implementación

- **Actualización de esquema de base de datos**: Reorganización de tablas con prefijos de módulos (GestionEquiposInformaticos_*, GestionMantenimientos_*, GestionPersonas_*, GestionUsuarios_*) para mejor mantenibilidad y escalabilidad
- Las migraciones de base de datos se han aplicado exitosamente a producción sin afectar la funcionalidad de la aplicación

### Cambios de Base de Datos

- 21 tablas renombradas con prefijos específicos por módulo funcional
- Todas las relaciones y constrains actualizados automáticamente
- Índices y claves primarias adaptadas a nuevos nombres

---

## Versión 1.0.45

Fecha: 21 de enero de 2026

## Mejoras

- Mejora en la exportación de mantenimientos: formato y diseño actualizados (SST-F-83 — Versión 4) para facilitar la lectura y el intercambio.
- Se corrigió y mejoró la exportación: formato más consistente y se añadieron servicios específicos para exportar la Hoja de Vida y los listados de Equipos.
- Los campos Código, Nombre, Marca, Clasificación y "Comprado a" ahora se muestran en MAYÚSCULAS para mayor consistencia visual.
- El campo "Responsable" en los registros de mantenimientos ahora se muestra en MAYÚSCULAS al registrarse para mejorar la consistencia en listados e informes.
- Los campos "Descripción" y "Observaciones" en formularios y reportes ahora aceptan hasta **1000 caracteres**, permitiendo descripciones y notas más completas.

## Implementación

- Trazabilidad añadida para mantenimientos "No Realizado": los mantenimientos no ejecutados quedan registrados y claramente identificados tanto en la exportación como en el historial de ejecuciones.
- Visual: las filas correspondientes a mantenimientos "No Realizado" se muestran en rojo claro para facilitar su identificación.
- Nuevo: al crear un usuario, el sistema genera automáticamente una contraseña temporal y envía un correo de bienvenida con las credenciales e instrucciones. El usuario deberá cambiar esa contraseña en su primer acceso.


## Arreglos

- Evitado duplicado de registros automáticos "No Realizado" al iniciar el sistema.
- Corrección visual: los items "No Realizado" ahora se distinguen claramente de los atrasados.
- Corregido: fallo que provocaba errores al eliminar usuarios en algunas condiciones; la operación ahora se realiza de forma segura y confiable.
- Corregido: en el diálogo de equipos los desplegables de Marca, Clasificación y "Comprado a" ahora muestran inmediatamente las opciones al abrirse y permiten buscar o añadir rápidamente nuevas entradas.
- Mejorado: los desplegables editables convierten automáticamente el texto a MAYÚSCULAS mientras se escribe, facilitando la búsqueda y estandarización de los registros.
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
- Se sugiere informar a los usuarios que los campos "Descripción" y "Observaciones" permiten ahora hasta 1000 caracteres, para aprovechar la mayor capacidad al documentar mantenimientos.
