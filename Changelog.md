# Gestión de Equipos Informáticos - Cambios para usuarios

Fecha: 21 de enero de 2026

## Mejoras

- Mejora en la exportación de mantenimientos: formato y diseño actualizados (SST-F-83 — Versión 4) para facilitar la lectura y el intercambio.

## Implementación

- Trazabilidad añadida para mantenimientos "No Realizado": los mantenimientos no ejecutados quedan registrados y claramente identificados tanto en la exportación como en el historial de ejecuciones.
- Visual: las filas correspondientes a mantenimientos "No Realizado" se muestran en rojo claro para facilitar su identificación.

## Arreglos

- Evitado duplicado de registros automáticos "No Realizado" al iniciar el sistema.
- Corrección visual: los items "No Realizado" ahora se distinguen claramente de los atrasados.

## Notas

- Se recomienda ejecutar una exportación de prueba y una compilación completa para validar colores, merges y trazabilidad end-to-end.
- Pendiente: pruebas de rendimiento con hojas grandes y verificación final de que los registros marcados coincidan con la base de datos.
