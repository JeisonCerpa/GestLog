# 📚 Documentación Técnica - GestLog

Esta carpeta contiene la documentación técnica oficial del software GestLog. Cada archivo documenta sistemas y componentes específicos implementados en el proyecto.

## 📁 Documentación de Sistemas

### `ASYNC_SYSTEM.md`
**Sistema Asíncrono Completo**  
Documentación del sistema async/await, arquitectura de servicios, patrones de implementación y guías de uso para operaciones no bloqueantes.

### `CANCELLATION_SYSTEM.md`
**Sistema de Cancelación de Procesos**  
Implementación de CancellationToken, manejo de operaciones de larga duración y arquitectura de cancelación de tareas.

### `DEPENDENCY_INJECTION_STANDARDIZATION.md`
**Estandarización de Inyección de Dependencias**  
Configuración del contenedor DI, registro de servicios y patrones de implementación para la gestión de dependencias.

### `ERROR_HANDLING_TESTING_GUIDE.md`
**Guía de Testing para Manejo de Errores**  
Instrucciones de pruebas, validación de funcionalidades y casos de test para el sistema de manejo de errores.

### `SIMPLE_PROGRESS_BAR_GUIDE.md`
**Guía del Componente de Progreso**  
Documentación del control personalizado SimpleProgressBar, implementación de animaciones y uso en interfaces de usuario.

### `SISTEMA_RESILIENCIA_IMPLEMENTADO.md`
**Sistema de Resiliencia de Base de Datos**  
Documentación completa del sistema de resiliencia empresarial implementado con Circuit Breaker, Exponential Backoff y Network Monitoring.

## 📄 Archivos de Configuración

### `email-configuration-examples.json`
**Ejemplos de Configuración de Email**  
Plantillas y ejemplos de configuración SMTP para diferentes proveedores de email y casos de uso.

---

## 🎯 Estructura de la Documentación

La documentación está organizada por sistemas funcionales:

- **Sistemas Core**: Async, DI, Error Handling
- **Componentes UI**: Progress Bar, Controles personalizados  
- **Infraestructura**: Resiliencia de BD, Configuraciones
- **Testing**: Guías de pruebas y validación

## 📝 Convenciones

- **Formato**: Markdown (.md) para documentación técnica
- **Idioma**: Técnico en inglés, UI en español
- **Estructura**: README principal + documentos específicos por sistema
- **Versionado**: Actualización con cada cambio significativo

## 🎯 Propósito

Esta documentación está dirigida a:
- **Desarrolladores** que trabajen en el mantenimiento del código
- **Arquitectos** que necesiten entender los sistemas implementados
- **QA** que requieran validar funcionalidades específicas
- **Nuevos miembros del equipo** que necesiten onboarding técnico

---

# 🔐 Permisos por Módulo

Todo módulo nuevo debe definir y validar sus propios permisos de acceso y operación. Los permisos se gestionan por usuario y se consultan mediante la clase `CurrentUserInfo` y el método `HasPermission(string permiso)`.

**Ejemplo de permisos:**
- `Herramientas.AccederDaaterProccesor` (acceso al módulo DaaterProccesor)
- `DaaterProccesor.ProcesarArchivos` (procesar archivos en DaaterProccesor)

**Implementación recomendada:**
- Los ViewModels deben exponer propiedades como `CanAccess[Modulo]` y `Can[Accion]` para el binding en la UI.
- Los comandos deben usar `[RelayCommand(CanExecute = nameof(Can[Accion]))]` para habilitar/deshabilitar acciones según permisos.
- La visibilidad y navegación en la UI debe estar condicionada por los permisos del usuario.

**¿Cómo agregar permisos a un módulo nuevo?**
1. Definir los permisos en la base de datos y en el sistema de autenticación.
2. Agregar las validaciones en el ViewModel:
   ```csharp
   public bool CanAccessMiModulo => _currentUser.HasPermission("Herramientas.AccederMiModulo");
   public bool CanProcesarMiModulo => _currentUser.HasPermission("MiModulo.Procesar");
   ```
3. Exponer los permisos en la UI:
   - Usar `{Binding CanAccessMiModulo}` para visibilidad.
   - Usar `{Binding CanProcesarMiModulo}` para habilitar botones y comandos.
4. Registrar el ViewModel en el contenedor DI con `CurrentUserInfo` inyectado.
5. Validar la navegación y mostrar mensajes de acceso denegado si el usuario no tiene permisos.

**Documentar los permisos:**
- Documenta los permisos requeridos por cada módulo en su README correspondiente.
- Ejemplo:
  - **Permisos requeridos:**
    - `Herramientas.AccederMiModulo`
    - `MiModulo.Procesar`
- Explica cómo se validan y cómo se deben agregar nuevos permisos siguiendo el patrón de DaaterProccesor.

## 🛡️ Permisos y Validación de Acciones en la UI

### Patrón de permisos (Gestión de Cartera)

- Los botones "Generar documentos" y "Enviar documentos automáticamente" se deshabilitan y se ven atenuados (opacity=0.5) cuando faltan entradas requeridas (Excel, carpeta, SMTP, etc.).
- El ViewModel expone propiedades como `CanGenerateDocuments` y `CanSendAutomatically` que validan permisos y configuración.
- En XAML, enlaza `IsEnabled` y `Opacity` de los botones a estas propiedades usando el convertidor `BooleanToOpacityConverter`.
- Ejemplo:

```xaml
<Button Content="Generar" IsEnabled="{Binding CanGenerateDocuments}" Opacity="{Binding CanGenerateDocuments, Converter={StaticResource BooleanToOpacityConverter}}" />
<Button Content="Enviar" IsEnabled="{Binding CanSendAutomatically}" Opacity="{Binding CanSendAutomatically, Converter={StaticResource BooleanToOpacityConverter}}" />
```

- Para agregar un nuevo permiso:
  1. Declara la propiedad bool en el ViewModel consultando CurrentUserInfo.HasPermission("Permiso")
  2. Usa esa propiedad en el método CanExecute del comando
  3. Enlaza la propiedad en la UI (IsEnabled/Opacity/Visibility)
  4. Documenta el permiso en el README y copilot-instructions.md

### Validación y mensajes
- Si falta configuración, el ViewModel expone mensajes claros (ej: `DocumentStatusWarning`) que se muestran en la UI.
- Los controles se deshabilitan y muestran feedback visual cuando no se puede ejecutar la acción.

---
*Actualizado: Agosto 2025**
