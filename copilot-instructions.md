````instructions
# 🚀 Instrucciones GitHub Copilot - GestLog

## 🎯 **Contexto**
WPF + .NET 9.0 | **Código**: inglés | **UI**: español (es-CO) | **MVVM** estricto

## 🎨 **Tema Visual**
- **Paleta**: Verde principal `#118938`, verde secundario `#2B8E3F`, grises `#504F4E`, `#706F6F`, `#C0392B`
- **Fuente**: `Segoe UI` (legible y elegante)
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
8. **Backup**: Antes de modificar un archivo, y evitar dañarlo, crear una copia .bak en la misma carpeta para poder compararla si se daña algo (Basicamente crear un archivo .bak inicial, y luego de todas las pruebas y funcione todo al 100%, cuando el usuario diga que funciona ya se puede eliminar, no crear uno en cada cambio, solo 1 inicial)

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
``````
