# 📋 Guía de Migración: Auto-Refresh para Todos los Módulos

## 🎯 **PLAN DE ACCIÓN COMPLETO**

### **FASE 1: ViewModels Principales (PRIORIDAD ALTA)**

#### **1.1 EquiposInformaticosViewModel**
```bash
Archivo: ViewModels/Tools/GestionEquipos/EquiposInformaticosViewModel.cs
Impacto: ALTO - Módulo principal de gestión de equipos
Tiempo estimado: 30 minutos
```

**Cambios requeridos:**
- [ ] Cambiar herencia: `ObservableObject` → `DatabaseAwareViewModel`
- [ ] Actualizar constructor: agregar `IDatabaseConnectionService`, `IGestLogLogger`
- [ ] Reemplazar `GestLogDbContext` → `IDbContextFactory<GestLogDbContext>`
- [ ] Implementar `RefreshDataAsync()`
- [ ] Agregar timeout de 1 segundo en consultas
- [ ] Testing de reconexión automática

#### **1.2 UsuarioManagementViewModel**
```bash
Archivo: Modules/Usuarios/ViewModels/UsuarioManagementViewModel.cs  
Impacto: CRÍTICO - Gestión de usuarios del sistema
Tiempo estimado: 45 minutos
```

**Nota especial:** Este ViewModel usa servicios (`IUsuarioService`) en lugar de acceso directo a BD. 
**Estrategia:** Los servicios ya manejan `IDbContextFactory`, solo aplicar auto-refresh al ViewModel.

### **FASE 2: ViewModels de Gestión (PRIORIDAD MEDIA)**

#### **2.1 PersonaManagementViewModel**
```bash
Archivo: Modules/Usuarios/ViewModels/PersonaManagementViewModel.cs
Impacto: MEDIO - Gestión de personas
Tiempo estimado: 30 minutos
```

#### **2.2 AuditoriaManagementViewModel**  
```bash
Archivo: Modules/Usuarios/ViewModels/AuditoriaManagementViewModel.cs
Impacto: BAJO - Consultas de auditoría
Tiempo estimado: 20 minutos
```

### **FASE 3: ViewModels de Configuración (PRIORIDAD BAJA)**

#### **3.1 CatalogosManagementViewModel**
#### **3.2 GestionPermisosRolViewModel**

---

## 🔧 **PLANTILLA DE MIGRACIÓN ESTÁNDAR**

### **PASO 1: Actualizar Imports**
```csharp
// AGREGAR estos imports
using GestLog.ViewModels.Base;
using GestLog.Services.Interfaces;  
using GestLog.Services.Core.Logging;
```

### **PASO 2: Cambiar Herencia**
```csharp
// ANTES
public partial class MiViewModel : ObservableObject

// DESPUÉS
public partial class MiViewModel : DatabaseAwareViewModel
```

### **PASO 3: Actualizar Constructor**
```csharp
// ANTES
public MiViewModel(IGestLogLogger logger, ...)
{
    _logger = logger;
    // ...
}

// DESPUÉS  
public MiViewModel(IGestLogLogger logger, IDatabaseConnectionService databaseService, ...)
    : base(databaseService, logger)
{
    // Solo inicializar campos específicos del ViewModel
    // _logger ya no es necesario - está en la clase base
}
```

### **PASO 4: Implementar RefreshDataAsync**
```csharp
protected override async Task RefreshDataAsync()
{
    try
    {
        _logger.LogInformation("[{ViewModelName}] Refrescando datos automáticamente", nameof(MiViewModel));
        
        // Llamar a tu método de carga existente
        await CargarDatos();
        
        _logger.LogInformation("[{ViewModelName}] Datos refrescados exitosamente", nameof(MiViewModel));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[{ViewModelName}] Error al refrescar datos", nameof(MiViewModel));
        throw; // Re-lanzar para que la clase base maneje
    }
}
```

### **PASO 5: Eliminar Código Duplicado**
```csharp
// ELIMINAR estas propiedades (están en la clase base):
// [ObservableProperty] private string _statusMessage;
// [ObservableProperty] private bool _isLoading;

// ELIMINAR suscripciones manuales a eventos de conexión:
// _databaseService.ConnectionStateChanged += ...

// ELIMINAR métodos Dispose básicos (a menos que tengan lógica específica)
```

### **PASO 6: Actualizar Métodos de Datos**
```csharp
// ANTES (DbContext directo)
private readonly GestLogDbContext _db;
var datos = await _db.MiTabla.ToListAsync();

// DESPUÉS (DbContextFactory con timeout)
private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;

using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
using var dbContext = _dbContextFactory.CreateDbContext();
dbContext.Database.SetCommandTimeout(1);

var datos = await dbContext.MiTabla
    .AsNoTracking()
    .ToListAsync(timeoutCts.Token);
```

---

## ✅ **CHECKLIST DE MIGRACIÓN POR VIEWMODEL**

### **EquiposInformaticosViewModel**
- [ ] ✅ Cambiar herencia a `DatabaseAwareViewModel`
- [ ] ✅ Actualizar constructor con `IDatabaseConnectionService`
- [ ] ✅ Cambiar `GestLogDbContext` → `IDbContextFactory<GestLogDbContext>`
- [ ] ✅ Implementar `RefreshDataAsync()`
- [ ] ✅ Agregar timeout 1s en `CargarEquiposAsync()`
- [ ] ✅ Eliminar propiedades duplicadas (`StatusMessage`, `IsLoading`)
- [ ] ✅ Testing: Verificar auto-refresh funciona
- [ ] ✅ Testing: Verificar timeout ultrarrápido
- [ ] ✅ Validar que no hay memory leaks

### **UsuarioManagementViewModel**
- [ ] Cambiar herencia a `DatabaseAwareViewModel`
- [ ] Actualizar constructor  
- [ ] Implementar `RefreshDataAsync()` llamando servicios existentes
- [ ] Validar que servicios usan timeout correcto
- [ ] Testing completo

### **PersonaManagementViewModel**
- [ ] Mismos pasos que UsuarioManagementViewModel

### **AuditoriaManagementViewModel**  
- [ ] Mismos pasos base
- [ ] Considerar si las consultas de auditoría necesitan auto-refresh

---

## 🚀 **COMANDOS DE TESTING**

### **Verificar Compilación Después de Cada ViewModel:**
```powershell
cd "e:\Softwares\GestLog"
dotnet build --configuration Debug --verbosity minimal
```

### **Testing Manual de Auto-Refresh:**
1. Abrir módulo con ViewModel migrado
2. Desconectar base de datos / red
3. Verificar que falla en ~1 segundo (no 30+ segundos)
4. Reconectar base de datos / red  
5. Verificar que se refresca automáticamente sin intervención
6. Confirmar mensaje "Datos actualizados automáticamente"

### **Testing de Memory Leaks:**
1. Abrir y cerrar módulo varias veces
2. Verificar logs - debe aparecer "[ViewModelName] ViewModel disposed correctamente"
3. Usar herramientas de memory profiling si disponible

---

## ⚡ **ORDEN DE IMPLEMENTACIÓN RECOMENDADO**

### **Semana 1:**
- [x] ✅ PerifericosViewModel (COMPLETADO)
- [ ] 🔄 EquiposInformaticosViewModel 

### **Semana 2:**  
- [ ] 🔄 UsuarioManagementViewModel
- [ ] 🔄 PersonaManagementViewModel

### **Semana 3:**
- [ ] 🔄 AuditoriaManagementViewModel
- [ ] 🔄 CatalogosManagementViewModel  
- [ ] 🔄 GestionPermisosRolViewModel

### **Validación Final:**
- [ ] Testing completo de todos los módulos
- [ ] Verificar experiencia de usuario fluida
- [ ] Documentar casos especiales encontrados
- [ ] Training para equipo de desarrollo

---

## 🎯 **MÉTRICAS DE ÉXITO**

### **Técnicas:**
- ✅ Timeout promedio: < 2 segundos (antes: 30+ segundos)
- ✅ Auto-refresh: 100% automático, 0% intervención manual
- ✅ Memory leaks: 0 detected
- ✅ Compilación: Sin errores ni warnings

### **Experiencia de Usuario:**
- ✅ Sin bloqueos prolongados
- ✅ Feedback visual inmediato  
- ✅ Reconexión transparente
- ✅ Consistencia entre módulos

### **Mantenimiento:**
- ✅ Código duplicado eliminado
- ✅ Patrón consistente aplicado
- ✅ Logging estandarizado
- ✅ Testing simplificado

---

## 📞 **SOPORTE Y TROUBLESHOOTING**

### **Problemas Comunes:**

#### **Error: "No se pudo encontrar IDatabaseConnectionService"**
**Solución:** Verificar que el servicio esté registrado en DI container

#### **Error: "RefreshDataAsync no implementado"**  
**Solución:** Es método abstracto, debe implementarse en cada ViewModel

#### **Auto-refresh no funciona**
**Solución:** Verificar que DatabaseConnectionService detecta cambios de estado

#### **Timeout muy lento aún**
**Solución:** Verificar configuración global en Startup.Database.cs (debe ser 1 segundo)

---

**🎯 RESULTADO FINAL: Toda la aplicación GestLog tendrá auto-refresh automático, experiencia fluida y código mantenible.**
