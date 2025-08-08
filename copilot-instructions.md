````````````instructions
```````````instructions
``````````instructions
````instructions
# 🚀 Instrucciones GitHub Copilot - GestLog

## 🎯 **Contexto**
WPF + .NET 9.0 | **Código**: inglés | **UI**: español (es-CO) | **MVVM** estricto

## 🎨 **Tema Visual**
- **Paleta**: Verde principal `#118938`, verde secundario `#2B8E3F`, grises `#504F4E`, `#706F6F`, `#C0392B`
- **Fondo**: Off-white `#FAFAFA` para contenido, cartas blancas `#FFFFFF`
- **Efectos**: `DropShadowEffect` en navbar, cartas y botones con `CornerRadius="8"`
- **Navbar**: Gradiente verde con logo en esquina inferior derecha
- **Botones**: Hover states con colores más oscuros y sombras dinámicas
- **Barra de Progreso**: Control `SimpleProgressBar` con fondo blanco `#FFFFFF`, barra verde `#118938`, bordes redondeados, títulos en gris oscuro `#504F4E`, porcentajes en verde y mensajes de estado personalizables

## ⚡ **Reglas Fundamentales**
1. **SRP**: Una responsabilidad por clase → **Si viola SRP → Refactorizar inmediatamente**
2. **Async**: Siempre para I/O + CancellationToken
3. **DI**: Constructor injection obligatorio
4. **Logging**: IGestLogLogger en todo
5. **MVVM**: Cero lógica en code-behind
6. **Validación**: Antes de procesar
7. **Errores**: Específicos del dominio + mensajes claros en español
8. **Ubicación de módulos**: Todas las implementaciones o nuevos módulos deben ir dentro de la carpeta `Modules/` siguiendo la estructura recomendada. Sus vistas van dentro de la carpeta /Views (Siguiendo su estructura).
9. **Archivos vacíos**: No crear archivos vacíos como `.keep` para mantener carpetas en el repositorio; la gestión de carpetas vacías la maneja el `.gitignore` y las reglas del repositorio.

## 🏗️ **Arquitectura Base**

```csharp
// ✅ ViewModels con CommunityToolkit.Mvvm
public partial class DocumentGenerationViewModel : ObservableObject
{
    private readonly IPdfGeneratorService _pdfService;
    private readonly IGestLogLogger _logger;
    
    [ObservableProperty] private string _selectedFilePath;
    
    [RelayCommand]
    private async Task GenerateAsync(CancellationToken cancellationToken)
    {
        try { /* Implementación */ }
        catch (SpecificException ex) { /* Manejo específico */ }
    }
}

// ✅ DI Registration
ServiceLocator.RegisterSingleton<IGestLogLogger, GestLogLogger>();
ServiceLocator.RegisterTransient<DocumentGenerationViewModel>();
```

## 📋 **Manejo de Errores Específicos**

### **Excepciones por Dominio**
```csharp
// ✅ Excel
public class ExcelFormatException : GestLogException
{
    public ExcelFormatException(string message, string filePath, string expectedFormat) 
        : base(message, "EXCEL_FORMAT_ERROR") { }
}

// ✅ Email
public class EmailSendException : GestLogException
{
    public EmailSendException(string message, string emailAddress, Exception innerException) 
        : base(message, "EMAIL_SEND_ERROR", innerException) { }
}

// ✅ Archivos
public class FileValidationException : GestLogException
{
    public FileValidationException(string message, string filePath, string validationRule) 
        : base(message, "FILE_VALIDATION_ERROR") { }
}
```

### **Validación de Excel**
```csharp
// Validar archivo existe
if (!File.Exists(filePath))
    throw new FileValidationException("El archivo Excel seleccionado no existe", filePath, "FILE_EXISTS");

// Validar formato
if (!Path.GetExtension(filePath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
    throw new ExcelFormatException("El archivo debe ser un Excel (.xlsx)", filePath, ".xlsx");

// Validar columnas requeridas
var requiredColumns = new[] { "EMPRESA", "NIT", "EMAIL", "DIRECCION" };
var missingColumns = requiredColumns.Where(col => !worksheet.Row(1).Cells().Any(c => c.Value.ToString().Contains(col))).ToList();
if (missingColumns.Any())
    throw new ExcelFormatException($"Faltan columnas: {string.Join(", ", missingColumns)}", filePath, "REQUIRED_COLUMNS");
```

### **Validación de Email**
```csharp
// Validar configuración SMTP
if (string.IsNullOrEmpty(_smtpConfig.Server))
    throw new ConfigurationException("No se ha configurado el servidor SMTP", "SmtpServer");

// Validar destinatario
if (!IsValidEmail(recipient))
    throw new EmailSendException($"Email '{recipient}' no es válido", recipient, null);

// Manejar errores SMTP específicos
catch (SmtpException ex)
{
    var userMessage = ex.StatusCode switch
    {
        SmtpStatusCode.MailboxBusy => "El servidor está ocupado. Intente más tarde",
        SmtpStatusCode.MailboxUnavailable => $"Email '{recipient}' no existe",
        SmtpStatusCode.TransactionFailed => "Error de autenticación. Verifique credenciales",
        _ => "Error enviando email. Verifique configuración SMTP"
    };
    throw new EmailSendException(userMessage, recipient, ex);
}
```

### **Manejo en ViewModels**
```csharp
[RelayCommand]
private async Task ProcessAsync(CancellationToken cancellationToken)
{
    try
    {
        IsProcessing = true;
        ErrorMessage = string.Empty;
        
        var result = await _service.ProcessAsync(SelectedFile, cancellationToken);
        await ShowSuccessAsync($"Procesados {result.Count} elementos");
    }
    catch (ExcelFormatException ex)
    {
        ErrorMessage = $"Error Excel: {ex.Message}";
        await ShowErrorAsync("Error de Formato", ex.Message);
    }
    catch (EmailSendException ex)
    {
        ErrorMessage = $"Error Email: {ex.Message}";
        await ShowErrorAsync("Error de Envío", ex.Message);
    }
    catch (OperationCanceledException)
    {
        ErrorMessage = "Operación cancelada";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error");
        ErrorMessage = "Error inesperado";
        await ShowErrorAsync("Error", "Contacte soporte técnico");
    }
    finally { IsProcessing = false; }
}
```

## 🛡️ Permisos y Feedback Visual en la UI

- Los botones de generación y envío automático en Gestión de Cartera usan las propiedades `CanGenerateDocuments` y `CanSendAutomatically` del ViewModel.
- En XAML, enlaza `IsEnabled` y `Opacity` de los botones a estas propiedades usando el convertidor `BooleanToOpacityConverter`.
- Ejemplo:

```xaml
<Button Content="Generar" IsEnabled="{Binding CanGenerateDocuments}" Opacity="{Binding CanGenerateDocuments, Converter={StaticResource BooleanToOpacityConverter}}" />
<Button Content="Enviar" IsEnabled="{Binding CanSendAutomatically}" Opacity="{Binding CanSendAutomatically, Converter={StaticResource BooleanToOpacityConverter}}" />
```

- Si falta configuración (Excel, carpeta, SMTP), el ViewModel expone mensajes claros (`DocumentStatusWarning`) que se muestran en la UI.
- Para agregar un permiso:
  1. Declara la propiedad bool en el ViewModel consultando CurrentUserInfo.HasPermission("Permiso")
  2. Usa esa propiedad en el método CanExecute del comando
  3. Enlaza la propiedad en la UI
  4. Documenta el permiso en README y copilot-instructions.md

---

## 🔐 Permisos por Módulo

- Todo módulo nuevo debe definir y validar sus propios permisos de acceso y operación.
- Los permisos se gestionan por usuario y se consultan mediante la clase `CurrentUserInfo` y el método `HasPermission(string permiso)`.
- Ejemplo de permisos:
  - `Herramientas.AccederDaaterProccesor` (acceso al módulo DaaterProccesor)
  - `DaaterProccesor.ProcesarArchivos` (procesar archivos en DaaterProccesor)
- Los ViewModels deben exponer propiedades como `CanAccess[Modulo]` y `Can[Accion]` para el binding en la UI.
- Los comandos deben usar `[RelayCommand(CanExecute = nameof(Can[Accion]))]` para habilitar/deshabilitar acciones según permisos.
- La visibilidad y navegación en la UI debe estar condicionada por los permisos del usuario.

## ➕ ¿Cómo agregar permisos a un módulo nuevo?

1. **Definir los permisos en la base de datos y en el sistema de autenticación.**
2. **Agregar las validaciones en el ViewModel:**
   ```csharp
   public bool CanAccessMiModulo => _currentUser.HasPermission("Herramientas.AccederMiModulo");
   public bool CanProcesarMiModulo => _currentUser.HasPermission("MiModulo.Procesar");
   ```
3. **Exponer los permisos en la UI:**
   - Usar `{Binding CanAccessMiModulo}` para visibilidad.
   - Usar `{Binding CanProcesarMiModulo}` para habilitar botones y comandos.
4. **Registrar el ViewModel en el contenedor DI con `CurrentUserInfo` inyectado.**
5. **Validar la navegación y mostrar mensajes de acceso denegado si el usuario no tiene permisos.**

## 📖 Documentar los permisos

- Documenta los permisos requeridos por cada módulo en su README correspondiente.
- Ejemplo:
  - **Permisos requeridos:**
    - `Herramientas.AccederMiModulo`
    - `MiModulo.Procesar`
- Explica cómo se validan y cómo se deben agregar nuevos permisos siguiendo el patrón de DaaterProccesor.

## 🔑 Persistencia de sesión (Recordar inicio de sesión)

- Si el usuario marca "Recordar sesión" en el login, la información de CurrentUserInfo se guarda cifrada localmente.
- Al iniciar la aplicación, se intenta restaurar la sesión automáticamente usando CurrentUserService.RestoreSessionIfExists().
- El comando de cerrar sesión borra la sesión persistida y actualiza la UI.
- Ejemplo:

```csharp
// En LoginViewModel
[RelayCommand]
private async Task LoginAsync(CancellationToken cancellationToken = default)
{
    // ...
    if (result.Success && result.CurrentUserInfo != null)
        _currentUserService.SetCurrentUser(result.CurrentUserInfo, RememberMe);
}

[RelayCommand]
public async Task CerrarSesionAsync()
{
    await _authenticationService.LogoutAsync();
    _currentUserService.ClearCurrentUser();
    // Limpiar campos y mensajes
}
```

- Documenta el patrón en README y asegúrate de que la restauración de sesión se llame en App.xaml.cs al iniciar.

## 📖 Documentación Técnica

- **README.md**: Documentación general del módulo.
- **copilot-instructions.md**: Instrucciones específicas para GitHub Copilot.

---

## 🎯 Tecnologías Principales

- **.NET 9.0 + WPF**
- **CommunityToolkit.Mvvm** - `[ObservableProperty]`, `[RelayCommand]`
- **ClosedXML** - Excel
- **iText7** - PDF
- **IGestLogLogger** - Logging obligatorio

## 🚫 Anti-Patrones

```csharp
// ❌ NO hacer
public void ProcessData() { File.ReadAllText(path); } // Síncrono
catch (Exception ex) { Console.WriteLine("Error"); } // Log genérico
public string FilePath { get; set; } // Sin validación
private void Button_Click() { LoadData(); } // Lógica en code-behind
```

## 📁 Estructura de Módulos

```
Modules/[NombreModulo]/
├── ViewModels/          # Una responsabilidad UI por ViewModel
├── Services/            # Una responsabilidad de negocio por Service
├── Models/              # DTOs y entidades
└── Interfaces/          # Contratos
```

## 💡 Mensajes de Usuario

**Todos los mensajes al usuario en español:**
```csharp
// ✅ UI en español
ErrorMessage = "El archivo seleccionado no existe";
[Required(ErrorMessage = "Debe seleccionar un archivo Excel")]
MessageBox.Show("Operación completada exitosamente", "Éxito");

// ✅ Logs técnicos pueden ser en inglés
_logger.LogDebug("Processing Excel file: {FilePath}", filePath);
```

---

## ⚡ Reglas Rápidas

1. **SRP**: Una responsabilidad por clase
2. **Async**: Siempre para I/O
3. **DI**: Constructor injection
4. **Logging**: IGestLogLogger obligatorio
5. **Español**: UI y mensajes de usuario
6. **MVVM**: No lógica en code-behind
7. **Validación**: Antes de procesar
8. **Cancelación**: CancellationToken en operaciones largas
9. **Backup**: Crear copia .bak antes de modificar archivos críticos

**Si viola SRP → Refactorizar inmediatamente**

---

*Actualizado: Junio 2025*

## 🟢 Patrón: Actualización reactiva del nombre de usuario en el navbar

Para garantizar que el nombre del usuario autenticado se muestre SIEMPRE en el navbar tras login, restauración de sesión o cambio de usuario:

1. El ViewModel principal (`MainWindowViewModel`) se suscribe al mensaje `UserLoggedInMessage` usando CommunityToolkit.Mvvm.Messaging.
2. El `LoginViewModel` envía el mensaje tras login exitoso, pasando el objeto `CurrentUserInfo`.
3. El handler en `MainWindowViewModel` notifica el cambio de propiedad (`OnPropertyChanged(nameof(NombrePersonaActual))`) y actualiza `IsAuthenticated`.
4. El binding en XAML se actualiza automáticamente, sin depender del render ni del estado previo.
5. Para restauración de sesión, asegúrate de disparar también la notificación al cargar el usuario desde disco.

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
- Documenta este patrón en README.md y en los módulos que lo usen.

---
````````````
