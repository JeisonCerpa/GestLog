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
8. **Ubicación de módulos**: Todas las implementaciones o nuevos módulos deben ir dentro de la carpeta `Modules/` siguiendo la estructura recomendada. Sus vistas XAML van dentro de la carpeta `/Views` de cada módulo, no en `Views/Tools/` del nivel superior.
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

- Solo se deben implementar y documentar los permisos para las acciones que están disponibles para el usuario en cada módulo.
- Ejemplo en Gestión de Mantenimientos: únicamente se aplican permisos para Registrar equipo, Editar equipo, Dar de baja equipo y Registrar mantenimiento. No se deben agregar permisos para acciones no presentes en la UI.
- Los botones y comandos deben enlazar `IsEnabled` y `Opacity` a las propiedades de permiso del ViewModel usando el convertidor `BooleanToOpacityConverter`.
- Ejemplo:

```xaml
<Button Content="Registrar equipo" IsEnabled="{Binding CanRegistrarEquipo}" Opacity="{Binding CanRegistrarEquipo, Converter={StaticResource BooleanToOpacityConverter}}" />
<Button Content="Editar equipo" IsEnabled="{Binding CanEditarEquipo}" Opacity="{Binding CanEditarEquipo, Converter={StaticResource BooleanToOpacityConverter}}" />
<Button Content="Dar de baja" IsEnabled="{Binding CanDarDeBajaEquipo}" Opacity="{Binding CanDarDeBajaEquipo, Converter={StaticResource BooleanToOpacityConverter}}" />
<Button Content="Registrar mantenimiento" IsEnabled="{Binding CanRegistrarMantenimiento}" Opacity="{Binding CanRegistrarMantenimiento, Converter={StaticResource BooleanToOpacityConverter}}" />
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

Organiza el código en carpetas por responsabilidad. Estructura orientativa: adapta según la complejidad de tu módulo.

```
Modules/[NombreModulo]/
├── Views/                     # Vistas XAML y code-behind, organizadas por feature
│   ├── [Feature1]/
│   │   ├── [Feature1]View.xaml(.cs)
│   │   ├── [Feature1]Dialog.xaml(.cs)
│   │   └── [SubFeature]/
│   │       └── SubFeatureDialog.xaml(.cs)
│   └── [Feature2]/
│       ├── [Feature2]View.xaml(.cs)
│       └── [Feature2]Dialog.xaml(.cs)
├── ViewModels/                # Una responsabilidad UI por ViewModel
│   ├── [Feature1]/            # ViewModels agrupados por feature
│   │   ├── [Feature1]ViewModel.cs
│   │   ├── [Feature1]DialogViewModel.cs
│   │   └── [SubFeature]/
│   │       └── SubFeatureDialogViewModel.cs
│   └── [Feature2]/
│       ├── [Feature2]ViewModel.cs
│       └── [Feature2]DialogViewModel.cs
├── Services/                  # Una responsabilidad de negocio por Service
│   ├── Data/                  # Servicios de datos y operaciones CRUD
│   │   ├── [Feature1]Service.cs
│   │   ├── [Feature2]Service.cs
│   │   └── ...
│   ├── Cache/                 # Servicios de caché
│   │   └── [CacheService].cs
│   ├── Export/                # Servicios de exportación
│   │   └── [ExportService].cs
│   ├── Autocomplete/          # Servicios de autocompletado (si aplica)
│   │   └── [AutocompleteService].cs
│   └── ServiceCollectionExtensions.cs  # Registro de DI
├── Interfaces/                # Contratos organizados por tipo de servicio
│   ├── Data/                  # Interfaces para servicios de datos
│   │   └── I[Feature]Service.cs
│   ├── Cache/                 # Interfaces para servicios de caché
│   │   └── I[CacheService].cs
│   └── Export/                # Interfaces para servicios de exportación
│       └── I[ExportService].cs
├── Models/                    # DTOs y entidades organizadas por dominio
│   ├── DTOs/
│   │   ├── [Feature1]Dto.cs
│   │   ├── [Feature2]Dto.cs
│   │   └── ...
│   ├── Entities/              # Entidades de base de datos
│   │   ├── [Feature1].cs
│   │   ├── [Feature2].cs
│   │   └── ...
│   └── Enums/                 # Enumeraciones del dominio
│       ├── [Feature1]Estado.cs
│       ├── [Feature2]Tipo.cs
│       └── ...
├── Messages/                  # Mensajes para CommunityToolkit.Mvvm.Messaging
│   ├── [Feature1]UpdatedMessage.cs
│   ├── [Feature1]DeletedMessage.cs
│   └── ...
├── Utilities/                 # Clases utilitarias reutilizables del módulo
│   ├── [Feature1]Utils.cs
│   ├── [Feature2]Utils.cs
│   └── ...
└── Docs/                      # Documentación específica del módulo (opcional)
    ├── README.md
    └── [Feature1]_guide.md
```

### **Ejemplo real: GestionMantenimientos**

```
Modules/GestionMantenimientos/
├── Views/
│   ├── Cronograma/
│   │   ├── CronogramaView.xaml(.cs)
│   │   ├── CronogramaDialog.xaml(.cs)
│   │   └── SemanaDetalle/
│   │       └── SemanaDetalleDialog.xaml(.cs)
│   ├── Equipos/
│   │   ├── EquiposView.xaml(.cs)
│   │   ├── EquipoDialog.xaml(.cs)
│   │   └── EquipoDetalleModalWindow.xaml(.cs)
│   └── Seguimiento/
│       ├── SeguimientoView.xaml(.cs)
│       └── SeguimientoDialog.xaml(.cs)
│
├── ViewModels/
│   ├── Cronograma/
│   │   ├── CronogramaViewModel.cs
│   │   ├── SemanaViewModel.cs
│   │   └── SemanaDetalle/
│   │       └── SemanaDetalleViewModel.cs
│   ├── Equipos/
│   │   └── EquiposViewModel.cs
│   └── Seguimiento/
│       ├── SeguimientoViewModel.cs
│       └── RegistrarMantenimientoViewModel.cs
│
├── Services/
│   ├── Data/
│   │   ├── CronogramaService.cs
│   │   ├── EquipoService.cs
│   │   ├── SeguimientoService.cs
│   │   └── MaintenanceService.cs
│   ├── Cache/
│   │   └── EquipoCacheService.cs
│   ├── Export/
│   │   └── CronogramaExportService.cs
│   ├── Autocomplete/
│   │   ├── ClasificacionAutocompletadoService.cs
│   │   ├── CompradoAAutocompletadoService.cs
│   │   └── MarcaAutocompletadoService.cs
│   └── ServiceCollectionExtensions.cs
│
├── Interfaces/
│   ├── Data/
│   │   ├── ICronogramaService.cs
│   │   ├── IEquipoService.cs
│   │   ├── ISeguimientoService.cs
│   │   └── IMantenimientoService.cs
│   ├── Cache/
│   │   └── IEquipoCacheService.cs
│   └── Export/
│       └── ICronogramaExportService.cs
│
├── Models/
│   ├── DTOs/
│   │   ├── CronogramaMantenimientoDto.cs
│   │   ├── EquipoDto.cs
│   │   ├── SeguimientoMantenimientoDto.cs
│   │   ├── MantenimientoSemanaEstadoDto.cs
│   │   └── ...
│   ├── Entities/
│   │   ├── Equipo.cs
│   │   ├── CronogramaMantenimiento.cs
│   │   ├── SeguimientoMantenimiento.cs
│   │   └── ...
│   └── Enums/
│       ├── EstadoSeguimiento.cs
│       ├── EstadoEquipo.cs
│       ├── TipoMantenimiento.cs
│       └── ...
│
├── Messages/                  # Organizados por dominio/feature
│   ├── Equipos/
│   │   ├── EquiposActualizadosMessage.cs
│   │   └── EquiposCambioEstadoMessage.cs
│   ├── Mantenimientos/
│   │   ├── SeguimientosActualizadosMessage.cs
│   │   ├── CronogramasActualizadosMessage.cs
│   │   ├── MantenimientosActualizadosMessage.cs
│   │   └── EjecucionesPlanesActualizadasMessage.cs
│   └── UI/
│       └── UiMessages.cs
│
├── Utilities/
│   ├── EstadoSeguimientoUtils.cs
│   └── EstadoEquipoUtils.cs
│
└── Docs/
    ├── README.md
    └── GestionMantenimientos_guide.md
```

### **Reglas de organización por features:**
- ✅ Agrupa vistas por su feature/agregado de dominio (Cronograma, Equipos, Seguimiento)
- ✅ Anida sub-features si tienen relación jerárquica (SemanaDetalle dentro de Cronograma)
- ✅ Mantén orden alfabético dentro de cada nivel
- ✅ Actualiza los namespaces: `GestLog.Modules.[ModuleName].Views.[FeatureName]`
- ✅ Si tienes 3+ servicios del mismo tipo, crea subcarpetas en Services e Interfaces
- ❌ No crees más de 3 niveles de profundidad sin justificación

### **Organización de Messages (Mensajería MVVM)**

Los mensajes para **CommunityToolkit.Mvvm.Messaging** deben organizarse **por dominio/feature**, no en una carpeta plana. Esto mejora la cohesión y facilita el mantenimiento.

**Estructura recomendada:**
```
Messages/
├── [Feature1]/                                # Agrupa mensajes por su dominio
│   ├── [Feature1]UpdatedMessage.cs
│   ├── [Feature1]DeletedMessage.cs
│   └── [Feature1]StateChangedMessage.cs
├── [Feature2]/
│   ├── [Feature2]Message.cs
│   └── ...
└── UI/                                        # Mensajes de UI genéricos (si aplica)
    └── UiMessages.cs
```

**Ejemplo real: GestionMantenimientos**
```
Messages/
├── Equipos/                                   # Mensajes relacionados con equipos
│   ├── EquiposActualizadosMessage.cs
│   └── EquiposCambioEstadoMessage.cs
├── Mantenimientos/                            # Mensajes de mantenimientos
│   ├── SeguimientosActualizadosMessage.cs
│   ├── CronogramasActualizadosMessage.cs
│   ├── MantenimientosActualizadosMessage.cs
│   └── EjecucionesPlanesActualizadasMessage.cs
└── UI/                                        # Mensajes de interfaz (reservado)
    └── UiMessages.cs
```

**Reglas de namespace:**
- ✅ `GestLog.Modules.GestionMantenimientos.Messages.Equipos.EquiposActualizadosMessage`
- ✅ `GestLog.Modules.GestionMantenimientos.Messages.Mantenimientos.CronogramasActualizadosMessage`
- ✅ `GestLog.Modules.GestionMantenimientos.Messages.UI.UiMessages`
- ❌ `GestLog.Modules.GestionMantenimientos.Messages.EquiposActualizadosMessage` (plano)

**Al usar mensajes en ViewModels:**
```csharp
using GestLog.Modules.GestionMantenimientos.Messages.Equipos;
using GestLog.Modules.GestionMantenimientos.Messages.Mantenimientos;

// Registrarse a un mensaje
WeakReferenceMessenger.Default.Register<EquiposActualizadosMessage>(this, async (r, m) => 
{
    await RefreshAsync();
});

// Enviar un mensaje
WeakReferenceMessenger.Default.Send(new CronogramasActualizadosMessage());
```

**Beneficios:**
- 🎯 **Cohesión**: Mensajes agrupados por responsabilidad
- 🔍 **Navegación**: Fácil localizar mensajes relacionados
- 📦 **Escalabilidad**: Permite agregar más mensajes sin caos
- 🛡️ **Mantenibilidad**: Cambios en un dominio aislados a su carpeta

### **Uso de Utilities**
Centraliza métodos utilitarios reutilizables específicos del módulo: conversiones de enums, mapeo de estados a colores, formateo de datos.

- **Ubicación**: `Modules/[NombreModulo]/Utilities/[NombreUtils].cs`
- **Ejemplo**: `GestionMantenimientos/Utilities/EstadoSeguimientoUtils.cs`
- ✅ Clases estáticas con métodos estáticos para utilidades puras
- ✅ Una clase por responsabilidad (ej: conversiones de estados, colores, etc.)
- ❌ No poner lógica de negocio que debería estar en Services
- ❌ No crear si la funcionalidad cabe mejor en una clase de servicio existente

### **Organización jerárquica de Interfaces**
Las interfaces **DEBEN estar organizadas en la misma estructura jerárquica que sus implementaciones de Services**. Esto mejora la navegación, el mantenimiento y la coherencia del código.

**Estructura esperada:**
```
Interfaces/
├── Data/              # Interfaces para servicios de datos (CRUD, lógica de negocio)
│   ├── IEquipoService.cs
│   ├── ICronogramaService.cs
│   ├── ISeguimientoService.cs
│   └── IMantenimientoService.cs
├── Cache/             # Interfaces para servicios de caché
│   └── IEquipoCacheService.cs
└── Export/            # Interfaces para servicios de exportación
    └── ICronogramaExportService.cs
```

**Corresponde a la estructura de Services:**
```
Services/
├── Data/              # Implementaciones de servicios de datos
│   ├── EquipoService.cs
│   ├── CronogramaService.cs
│   ├── SeguimientoService.cs
│   └── MaintenanceService.cs
├── Cache/             # Implementaciones de servicios de caché
│   └── EquipoCacheService.cs
└── Export/            # Implementaciones de servicios de exportación
    └── CronogramaExportService.cs
```

**Reglas de namespace:**
- ✅ `GestLog.Modules.GestionMantenimientos.Interfaces.Data.IEquipoService`
- ✅ `GestLog.Modules.GestionMantenimientos.Interfaces.Cache.IEquipoCacheService`
- ✅ `GestLog.Modules.GestionMantenimientos.Interfaces.Export.ICronogramaExportService`
- ❌ `GestLog.Modules.GestionMantenimientos.Interfaces.IEquipoService` (plano)

**Al registrar en DI (ServiceCollectionExtensions.cs):**
```csharp
using GestLog.Modules.GestionMantenimientos.Interfaces.Data;
using GestLog.Modules.GestionMantenimientos.Interfaces.Cache;
using GestLog.Modules.GestionMantenimientos.Interfaces.Export;

public static IServiceCollection AddGestionMantenimientosServices(this IServiceCollection services)
{
    // Data Services
    services.AddScoped<IEquipoService, EquipoService>();
    services.AddScoped<ICronogramaService, CronogramaService>();
    services.AddScoped<ISeguimientoService, SeguimientoService>();
    services.AddScoped<IMantenimientoService, MaintenanceService>();

    // Cache Services
    services.AddSingleton<IEquipoCacheService, EquipoCacheService>();

    // Export Services
    services.AddTransient<ICronogramaExportService, CronogramaExportService>();
    
    return services;
}
```

**Al usar interfaces en ViewModels/Services:**
```csharp
using GestLog.Modules.GestionMantenimientos.Interfaces.Data;
using GestLog.Modules.GestionMantenimientos.Interfaces.Cache;

public partial class EquiposViewModel : ObservableObject
{
    private readonly IEquipoService _equipoService;
    private readonly IEquipoCacheService _cacheService;
    
    public EquiposViewModel(IEquipoService equipoService, IEquipoCacheService cacheService)
    {
        _equipoService = equipoService;
        _cacheService = cacheService;
    }
}
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

# ➕ Cómo agregar permisos a nuevos módulos

Para implementar permisos en cualquier módulo futuro de GestLog, sigue estos pasos generales:

1. **Definir el permiso en la base de datos**
   - Agrega el permiso en la tabla `Permisos` con un nombre único, descripción y módulo correspondiente.
   - Ejemplo: `MiModulo.AccionPrincipal`

2. **Asignar el permiso a roles**
   - Usa la gestión de roles para asignar el permiso a los roles que lo requieran.

3. **Validar el permiso en el ViewModel**
   - Declara una propiedad observable:
     ```csharp
     [ObservableProperty]
     private bool canAccionPrincipal;
     ```
   - En el método de inicialización o al cambiar usuario:
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
   - Añade la definición y uso del permiso en el README del módulo y en este archivo.

---

**Patrón recomendado:**
- Permisos por acción y módulo: `MiModulo.Accion`
- Validación centralizada en el ViewModel
- Refresco reactivo al cambiar usuario/rol
- Feedback visual en la UI

Esto asegura que los permisos sean consistentes, seguros y fáciles de mantener en toda la aplicación.

# 🌎 Configuración de Entorno (Development, Testing, Production)

GestLog soporta múltiples entornos de ejecución para facilitar el desarrollo, pruebas y despliegue seguro en producción. El entorno determina qué archivo de configuración de base de datos se carga automáticamente.

## ¿Cómo funciona?
- El entorno se detecta usando la variable de entorno `GESTLOG_ENVIRONMENT`.
- Según el valor, se carga el archivo correspondiente:
  - `Development` → `config/database-development.json`
  - `Testing` → `config/database-testing.json`
  - `Production` (o no definida) → `config/database-production.json`
- Si el archivo no existe, se usan valores predeterminados de producción.

## 🔄 Cambiar de entorno en tu máquina

### Opción 1: PowerShell
```powershell
# Para entorno de desarrollo
[Environment]::SetEnvironmentVariable("GESTLOG_ENVIRONMENT", "Development", "User")
# Para entorno de pruebas
[Environment]::SetEnvironmentVariable("GESTLOG_ENVIRONMENT", "Testing", "User")
# Para producción (o eliminar variable)
[Environment]::SetEnvironmentVariable("GESTLOG_ENVIRONMENT", "Production", "User")
```
Reinicia GestLog después de cambiar el entorno.
 
## 🚀 Buenas prácticas para despliegue
- **Producción:** Solo incluye `database-production.json` en el instalador/folder final.
- **Desarrollo/Testing:** Incluye los archivos de configuración necesarios.
- **Nunca** modifiques el código para cambiar de entorno; solo usa la variable y reinicia la app.

## 🧑‍💻 Notas para desarrolladores
- Puedes cambiar de entorno en cualquier momento usando la variable y reiniciando GestLog.
- Documenta el entorno que usas en tus PRs si es relevante.
- Si tienes dudas, consulta este archivo o pregunta al equipo.

## 🎯 ¿Development y Production en la misma máquina?
**¡SÍ!** Ambos entornos pueden funcionar perfectamente en la misma máquina:

### ✅ **Development** (con variable configurada)
- `dotnet run` desde VS Code ✅
- Debugging (F5) en VS Code ✅  
- Task "run-dev" de VS Code ✅
- Terminal PowerShell que hereda variables ✅

### ✅ **Production** (sin variable o ejecutable directo)
- Ejecutable publicado (`.exe`) ✅
- Acceso directo del escritorio ✅
- Instalador y aplicación instalada ✅
- Ejecutar desde Explorer ✅

**Configuración actual:**
- Variable `GESTLOG_ENVIRONMENT="Development"` configurada para tu usuario
- Archivos VS Code con tasks específicos por entorno
- Sistema de fallback automático a Production

## 🔄 Sistema de Actualización Automática con Velopack

GestLog incluye un sistema robusto de actualización automática usando Velopack que maneja la elevación de privilegios de forma inteligente.

### **Características principales:**
- **Detección automática** de actualizaciones en segundo plano
- **Descarga incremental** usando archivos delta para actualizaciones más rápidas
- **Auto-elevación inteligente** - solo solicita privilegios de administrador cuando es necesario aplicar actualizaciones
- **Cierre controlado** de la aplicación durante actualizaciones
- **Rollback automático** en caso de errores

### **Flujo de actualización:**
1. **Verificación silenciosa** - La app verifica actualizaciones al inicio sin mostrar UI
2. **Descarga automática** - Si hay actualizaciones, se descargan en segundo plano
3. **Solicitud de permisos** - Solo cuando va a aplicar la actualización, solicita privilegios de administrador
4. **Aplicación segura** - Cierra la aplicación de forma controlada y aplica la actualización
5. **Reinicio automático** - Inicia la nueva versión automáticamente

### **Configuración del servidor:**
- **Servidor de actualizaciones**: `\\SIMICSGROUPWKS1\Hackerland\Programas\GestLogUpdater`
- **Manifiestos**: `RELEASES`, `releases.win.json`
- **Paquetes**: `.nupkg` (full y delta)

### **Seguridad:**
- ✅ **Principio de menor privilegio** - Solo solicita admin cuando es necesario
- ✅ **Validación de origen** - Verifica integridad de las actualizaciones
- ✅ **Proceso controlado** - Maneja errores y permite continuar la ejecución
- ✅ **No ejecuta como admin por defecto** - Mejora la seguridad general

### **Para desarrolladores:**
- Las actualizaciones se manejan automáticamente
- El servicio `VelopackUpdateService` está registrado en DI
- Los logs detallan todo el proceso de actualización
- En caso de problemas de permisos, se guía al usuario

---

## 🔐 Detección de Problemas de Acceso al Servidor de Actualizaciones

**⚠️ PROBLEMA COMÚN:** Equipos sin credenciales de acceso a la carpeta `\\SIMICSGROUPWKS1\Hackerland\Programas\GestLogUpdater` no pueden actualizar.

### **¿Cómo detectar si hay problema de permisos?**

El servicio `VelopackUpdateService.CheckForUpdatesAsync()` ahora retorna un objeto `UpdateCheckResult` estructurado:

```csharp
public class UpdateCheckResult
{
    public bool HasUpdatesAvailable { get; set; }        // ¿Hay actualizaciones?
    public bool HasAccessError { get; set; }             // ¿Hay error de permisos/red?
    public string StatusMessage { get; set; }            // Mensaje descriptivo
    public string? ErrorType { get; set; }               // Tipo: UnauthorizedAccess, IOAccess, Network, etc.
    public Exception? InnerException { get; set; }       // Excepción completa para logs
}
```

### **Uso correcto en ViewModels:**
```csharp
[RelayCommand]
private async Task CheckUpdatesAsync()
{
    try
    {
        var result = await _velopackService.CheckForUpdatesAsync();
        
        if (result.HasAccessError)
        {
            // ❌ Error de permisos/acceso a red - INVESTIGAR DESPUÉS
            _logger.LogWarning($"Acceso denegado al servidor: {result.ErrorType}");
            ErrorMessage = result.StatusMessage;
            // Mostrar UI con mensaje amigable al usuario
            return;
        }

        if (result.HasUpdatesAvailable)
        {
            // ✅ Actualizaciones disponibles
            await NotifyAndPromptForUpdateAsync();
        }
        else
        {
            // ℹ️ Ya tienes la versión más reciente
            StatusMessage = "Ya tiene la versión más reciente";
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error inesperado al verificar actualizaciones");
    }
}
```

### **Errores específicos que se detectan:**
| ErrorType | Causa | Solución |
|-----------|-------|----------|
| `UnauthorizedAccess` | Permisos insuficientes | Verificar credenciales de dominio, permisos de carpeta |
| `IOAccess` | Ruta UNC inaccesible | Verificar conectividad a red, ruta correcta |
| `Network` | Problema de red/conectividad | Verificar conexión a red, ping al servidor |
| (otro) | Error desconocido | Revisar logs detallados, contactar admin |

### **Logs detallados para diagnóstico:**
Cuando hay un error de acceso, el logger registra:
- 🔒 **Ruta del servidor** donde ocurrió el error
- 👤 **Tipo de error** (UnauthorizedAccess, IOException, NetworkError)
- 📝 **Mensaje detallado** de la excepción
- 📚 **Stack trace completo** para debugging

**Buscar en logs:** `ERROR-PERMISOS` o `ERROR-IO` para identificar rápidamente problemas de acceso.

### **¿Por qué es importante esta distinción?**
Antes: "No hay actualizaciones disponibles" ❌ (podría ser un problema de permisos oculto)

Ahora: "❌ Acceso denegado al servidor. Verifique permisos de usuario." ✅ (información clara para resolver)

---

## Colores y semántica de estados en Gestión de Equipos Informáticos

- **Activo**: Verde (#2B8E3F)
- **En mantenimiento**: Ámbar (#F9B233)
- **En reparación**: Naranja/ámbar oscuro (#A85B00)
- **Dado de baja**: Gris muy claro (#EDEDED), opacidad baja, texto tachado. Representa equipos fuera de uso definitivo.
- **Inactivo**: Gris medio/oscuro (#9E9E9E), opacidad 0.85, sin tachado. Representa equipos guardados pero reutilizables.

### Decisión
Se diferencia visual y semánticamente "Dado de baja" (gris muy claro, opacidad baja, tachado) de "Inactivo" (gris medio/oscuro, opacidad 0.85, sin tachado) para evitar confusión y mejorar la experiencia de usuario.

### Ubicación
- Converter: EstadoToColorConverter.cs
- Estilos: EquiposInformaticosView.xaml

---

## 📋 Cambios Recientes en Refactorización de Módulos

### **Diciembre 2025 - Refactorización de GestionMantenimientos**

Se realizó una refactorización completa del módulo `GestionMantenimientos` siguiendo patrones SRP:

#### **Cambios aplicados:**
1. **Services refactorizados** en 4 subcarpetas:
   - `Data/` - Servicios CRUD (4 servicios)
   - `Export/` - Servicios de exportación (2 servicios)
   - `Autocomplete/` - Servicios de autocompletado (3 servicios)
   - `Cache/` - Servicios de caché (1 servicio)

2. **Interfaces espejo** creadas en estructura idéntica a Services:
   - `Interfaces/Data/` - 4 interfaces
   - `Interfaces/Export/` - 1 interfaz
   - `Interfaces/Cache/` - 1 interfaz

3. **Models organizados** por tipo:
   - `Models/DTOs/` - 4 DTOs
   - `Models/Entities/` - 5 entidades
   - `Models/Enums/` - 5 enumeraciones
   - `Models/Exceptions/` - 1 excepción de dominio

4. **ViewModels organizados** por feature:
   - `ViewModels/Cronograma/` - 3 ViewModels
   - `ViewModels/Equipos/` - 2 ViewModels
   - `ViewModels/Seguimiento/` - 2 ViewModels
   - `ViewModels/Mantenimiento/` - 1 ViewModel

5. **Messages refactorizados** en 3 subcarpetas por dominio:
   - `Messages/Equipos/` - Mensajes de equipos
   - `Messages/Mantenimientos/` - Mensajes de mantenimientos
   - `Messages/UI/` - Mensajes de interfaz (reservado)
   - ✅ Todos los usings actualizados en 13 archivos
   - ✅ Compilación exitosa: 0 errores

#### **Patrón a seguir para otros módulos:**
Use esta refactorización como referencia para reorganizar otros módulos existentes. La estructura proporciona:
- ✅ Claridad en responsabilidades (SRP)
- ✅ Mantenimiento facilitado
- ✅ Escalabilidad sin caos
- ✅ Namespaces jerárquicos y consistentes
- ✅ DI centralizado (ServiceCollectionExtensions.cs)

---
Última actualización: 11/12/2025
