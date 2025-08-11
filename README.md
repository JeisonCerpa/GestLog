# GestLog - Sistema de Gestión Logística

## 🛡️ Permisos y Validaciones
- Los permisos de usuario se gestionan en los ViewModels mediante validaciones previas a cada acción.
- La UI muestra feedback visual según los permisos y el estado de validación.
- Para agregar nuevos permisos, sigue el patrón DaaterProcessor y documenta en copilot-instructions.md.

## 🔑 Persistencia de sesión (Recordar inicio de sesión)
- Si el usuario marca "Recordar sesión" en el login, la información de usuario se guarda cifrada localmente.
- Al iniciar la aplicación, se intenta restaurar la sesión automáticamente usando CurrentUserService.RestoreSessionIfExists().
- El comando de cerrar sesión borra la sesión persistida y actualiza la UI.

## 🚪 Cierre de sesión y navegación
- El botón "Cerrar sesión" en la barra superior ejecuta el comando CerrarSesionAsync del MainWindowViewModel.
- Tras cerrar sesión, se navega automáticamente a la vista de login usando CommunityToolkit.Mvvm.Messaging y el mensaje ShowLoginViewMessage.
- La vista de login se muestra realmente en el MainWindow, reemplazando el contenido principal.

## 🧩 Patrón de mensajería y navegación
- Se utiliza WeakReferenceMessenger para enviar mensajes de navegación entre ViewModels y la ventana principal.
- El mensaje ShowLoginViewMessage desencadena la visualización de la vista de login.

## 📝 Ejemplo de flujo de cierre y restauración de sesión
1. El usuario cierra sesión desde el botón en la barra superior.
2. Se ejecuta el comando asíncrono de cierre de sesión y se borra la sesión persistida.
3. MainWindow navega a la vista de login.
4. Si el usuario tenía "Recordar sesión" activo, al reiniciar la app se restaura automáticamente la sesión.

## 🟢 Actualización reactiva del nombre de usuario en el navbar

Para garantizar que el nombre del usuario autenticado se muestre SIEMPRE en el navbar tras login, restauración de sesión o cambio de usuario:

- El ViewModel principal (`MainWindowViewModel`) se suscribe al mensaje `UserLoggedInMessage` usando CommunityToolkit.Mvvm.Messaging.
- El `LoginViewModel` envía el mensaje tras login exitoso, pasando el objeto `CurrentUserInfo`.
- El handler en `MainWindowViewModel` notifica el cambio de propiedad (`OnPropertyChanged(nameof(NombrePersonaActual))`) y actualiza `IsAuthenticated`.
- El binding en XAML se actualiza automáticamente, sin depender del render ni del estado previo.
- Para restauración de sesión, asegúrate de disparar también la notificación al cargar el usuario desde disco.

**Ejemplo:**
```csharp
// En LoginViewModel
WeakReferenceMessenger.Default.Send(new UserLoggedInMessage(result.CurrentUserInfo));

// En MainWindowViewModel
WeakReferenceMessenger.Default.Register<UserLoggedInMessage>(this, (r, m) => {
    if (m?.Value != null) {
        OnPropertyChanged(nameof(NombrePersonaActual));
        IsAuthenticated = true;
    }
});
```

**Notas:**
- La propiedad `NombrePersonaActual` debe ser calculada y reactiva, nunca asignada directamente.
- Si restauras sesión en `App.xaml.cs`, dispara también la notificación de cambio de usuario.
- Documenta este patrón en copilot-instructions.md y en los módulos que lo usen.

## 🔐 Guía para desarrolladores: Implementación de permisos en módulos

Para agregar permisos en cualquier módulo de GestLog, sigue este patrón general:

1. **Definir el permiso en la base de datos**
   - Inserta el permiso en la tabla `Permisos` con nombre, descripción y módulo.
   - Ejemplo: `MiModulo.AccionPrincipal`

2. **Asignar el permiso a roles**
   - Usa la gestión de roles para asignar el permiso a los roles necesarios.

3. **Validar el permiso en el ViewModel**
   - Declara una propiedad observable para el permiso:
     ```csharp
     [ObservableProperty]
     private bool canAccionPrincipal;
     ```
   - Actualiza la propiedad al iniciar sesión o cambiar usuario:
     ```csharp
     var hasPermission = _currentUser.HasPermission("MiModulo.AccionPrincipal");
     CanAccionPrincipal = hasPermission;
     OnPropertyChanged(nameof(CanAccionPrincipal));
     ```
   - Si la acción depende de otros factores, usa una propiedad calculada:
     ```csharp
     public bool CanEjecutarAccion => CanAccionPrincipal && OtrosRequisitos;
     ```

4. **Refrescar permisos de forma reactiva**
   - Suscríbete a cambios de usuario y roles para recalcular los permisos automáticamente.
   - Usa métodos como `RecalcularPermisos()` y notificaciones de cambio de propiedad.

5. **Enlazar la propiedad en la UI**
   - Usa `{Binding CanAccionPrincipal}` o `{Binding CanEjecutarAccion}` en los controles relevantes (`IsEnabled`, `Visibility`, `Opacity`).

6. **Documentar el permiso**
   - Añade la definición y uso del permiso en el README del módulo y en la documentación técnica.

---

**Recomendaciones:**
- Usa nombres de permisos claros y consistentes: `MiModulo.Accion`
- Centraliza la validación en el ViewModel
- Refresca los permisos al cambiar usuario/rol
- Proporciona feedback visual en la UI

Este patrón garantiza seguridad, mantenibilidad y una experiencia de usuario coherente en toda la aplicación.

## 📚 Documentación adicional
- Consulta copilot-instructions.md para detalles de arquitectura, patrones y reglas de implementación.
- Todos los cambios y patrones deben documentarse en este archivo y en copilot-instructions.md.
