# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Common Development Commands

### Building and Running
```bash
# Build in Debug mode (includes development config files)
dotnet build

# Build in Release mode (production config only)
dotnet build --configuration Release

# Run the application
dotnet run

# Publish for deployment
dotnet publish --configuration Release
```

### Environment Configuration
GestLog supports multiple environments controlled by the `GESTLOG_ENVIRONMENT` variable:

```powershell
# Set development environment
[Environment]::SetEnvironmentVariable("GESTLOG_ENVIRONMENT", "Development", "User")

# Set testing environment  
[Environment]::SetEnvironmentVariable("GESTLOG_ENVIRONMENT", "Testing", "User")

# Set production environment (or remove variable)
[Environment]::SetEnvironmentVariable("GESTLOG_ENVIRONMENT", "Production", "User")
```

**Important**: Restart GestLog after changing the environment variable.

- `Development` → loads `config/database-development.json`
- `Testing` → loads `config/database-testing.json`  
- `Production` (default) → loads `config/database-production.json`

### Database Operations
The application uses Entity Framework Core with automatic migration and connection management. Database configuration is environment-specific and loaded automatically at startup.

### Updates and Deployment
GestLog uses **Velopack** for automatic updates:
- Update server: `\\SIMICSGROUPWKS1\Hackerland\Programas\GestLogUpdater`
- Updates check silently in background on app startup
- Only prompts user when updates are actually available
- Supports auto-elevation for installation

## High-Level Architecture

### MVVM Pattern
- **Strict MVVM**: Zero logic in code-behind files
- **CommunityToolkit.Mvvm**: Uses `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`
- **ViewModels**: All UI logic, data binding, and command handling
- **Views**: Pure XAML with bindings to ViewModels

### Dependency Injection
Centralized DI configuration in `LoggingService.InitializeServices()`:
- **Singleton**: Shared services (logger, configuration, database)
- **Transient**: Per-operation services (processors, exporters)
- **Constructor Injection**: All dependencies injected via constructor

### Modular Organization
```
Modules/[ModuleName]/
├── ViewModels/     # UI logic and data binding
├── Services/       # Business logic
├── Models/         # Data models and DTOs  
├── Interfaces/     # Service contracts
└── Views/          # XAML files (in main Views/ folder)
```

### Logging System
- **Serilog** with structured logging
- **IGestLogLogger**: Custom wrapper with operation scopes
- **Multiple sinks**: Console, daily files, error-only files
- **Automatic context**: Machine name, thread ID, application version

### Multi-Environment Support
- Environment detection via `GESTLOG_ENVIRONMENT`
- Separate config files per environment
- Fallback to production if environment not specified
- Development and Testing configs only included in Debug builds

### Database Resilience
Advanced connection management with:
- **Circuit breaker** pattern for failure detection
- **Exponential backoff** for retries
- **Health checks** with adaptive monitoring
- **Network change detection** and automatic reconnection

### Authentication & Session Management
- **Session persistence**: "Recordar sesión" encrypts user data locally
- **Automatic restore**: Checks for saved session on app startup  
- **Reactive UI updates**: Uses `WeakReferenceMessenger` for user state changes
- **Permission system**: Role-based access control in ViewModels

### Update System (Velopack)
- **Silent background checks** on startup
- **Smart elevation**: Only requests admin privileges when installing
- **Delta updates**: Incremental downloads for faster updates
- **Rollback support**: Automatic recovery from failed updates

## Key Development Patterns

### Service Implementation
```csharp
public class MyService : IMyService
{
    private readonly IGestLogLogger _logger;
    private readonly IConfigurationService _config;
    
    public MyService(IGestLogLogger logger, IConfigurationService config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }
    
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginOperationScope("ProcessAsync");
        
        try
        {
            _logger.LogInformation("Starting process");
            // Implementation
            _logger.LogInformation("Process completed successfully");
        }
        catch (SpecificDomainException ex)
        {
            _logger.LogError(ex, "Domain-specific error occurred");
            throw;
        }
    }
}
```

### ViewModel Pattern
```csharp
public partial class MyViewModel : ObservableObject
{
    private readonly IMyService _service;
    private readonly IGestLogLogger _logger;
    
    [ObservableProperty]
    private bool isProcessing;
    
    [ObservableProperty]  
    private string errorMessage = string.Empty;
    
    public MyViewModel(IMyService service, IGestLogLogger logger)
    {
        _service = service;
        _logger = logger;
    }
    
    [RelayCommand]
    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsProcessing = true;
            ErrorMessage = string.Empty;
            
            await _service.ProcessAsync(cancellationToken);
        }
        catch (SpecificDomainException ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            await ShowErrorDialog("Error Title", ex.Message);
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
```

### Domain-Specific Error Handling
Create specific exception types for different domains:
```csharp
public class ExcelFormatException : GestLogException
{
    public ExcelFormatException(string message, string filePath, string expectedFormat) 
        : base(message, "EXCEL_FORMAT_ERROR") { }
}
```

### Permission Management
```csharp
// In ViewModels
[ObservableProperty]
private bool canAccessModule;

[ObservableProperty]
private bool canPerformAction;

public void InitializePermissions()
{
    var hasModuleAccess = _currentUser.HasPermission("Module.Access");
    CanAccessModule = hasModuleAccess;
    CanPerformAction = hasModuleAccess && _currentUser.HasPermission("Module.Action");
    
    OnPropertyChanged(nameof(CanAccessModule));
    OnPropertyChanged(nameof(CanPerformAction));
}
```

```xml
<!-- In XAML -->
<Button Content="Action" 
        IsEnabled="{Binding CanPerformAction}" 
        Opacity="{Binding CanPerformAction, Converter={StaticResource BooleanToOpacityConverter}}" />
```

### UI Theming
- **Primary colors**: Green `#118938`, secondary `#2B8E3F`
- **Backgrounds**: Off-white `#FAFAFA`, cards white `#FFFFFF`
- **Effects**: `DropShadowEffect`, `CornerRadius="8"`
- **Progress bars**: Custom `SimpleProgressBar` control
- **Converters**: `BooleanToOpacityConverter` for permission-based visibility

## Important Implementation Notes

### Session Persistence Flow
1. User checks "Recordar sesión" → credentials encrypted locally
2. App startup → `CurrentUserService.RestoreSessionIfExists()` 
3. If restored → navigate to HomeView, update navbar
4. Logout → clear encrypted session, navigate to LoginView

### Permission System Implementation
1. Define permission in database: `"Module.Action"`
2. Declare ViewModel property: `[ObservableProperty] private bool canAction;`
3. Initialize: `CanAction = _currentUser.HasPermission("Module.Action");`
4. Bind in XAML: `IsEnabled="{Binding CanAction}"`
5. Document in README.md and copilot-instructions.md

### Messaging and Navigation
- Use `WeakReferenceMessenger` for cross-component communication
- `ShowLoginViewMessage` triggers navigation to login
- `UserLoggedInMessage` updates navbar and authentication state
- Always notify property changes for reactive UI updates

### User Feedback Rules
- **UI messages**: Always in Spanish (es-CO)
- **Technical logs**: Can be in English  
- **Domain exceptions**: Specific error types with clear Spanish messages
- **Auto-refresh**: Use `OnPropertyChanged(nameof(PropertyName))` for immediate UI updates

### Documentation Maintenance
- Update `README.md` for user-facing changes and permission documentation
- Update `copilot-instructions.md` for development patterns and architectural decisions
- Both files must stay synchronized with implementation changes

## Technology Stack

### Core Framework
- **.NET 9.0** with WPF
- **C# 13** with nullable reference types enabled
- **CommunityToolkit.Mvvm** for MVVM implementation

### Data Access
- **Entity Framework Core 9.0** with SQL Server
- **ClosedXML** for Excel processing
- **iText7** for PDF generation

### Infrastructure  
- **Serilog** with structured logging
- **Microsoft.Extensions.DependencyInjection** for IoC
- **Velopack** for application updates

### UI Libraries
- **Microsoft.Xaml.Behaviors.Wpf** for behaviors
- **Ookii.Dialogs.Wpf** for native dialogs

This architecture ensures maintainability, testability, and a consistent development experience across all modules in the GestLog application.
