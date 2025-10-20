# Guía: Agregar ComboBox con Filtro sin Errores

## 📋 Descripción General

Esta guía explica cómo implementar un ComboBox editable con filtrado automático y búsqueda en tiempo real sin que el texto se borre al seleccionar elementos de la lista. El patrón se ha implementado exitosamente en `EquipoDialog.xaml` para los campos: **Clasificación**, **Comprado A**, y **Marca**.

## 🔑 Conceptos Clave

### Problema Original
Los ComboBox editables de WPF tienen un comportamiento donde al cambiar el `ItemsSource`, se borra el texto del usuario. Esto ocurre porque:

1. El usuario escribe en el ComboBox (ej: "Mar")
2. El `ItemsSource` se actualiza dinámicamente (filtra resultados)
3. WPF intenta sincronizar el `Text` con el nuevo `ItemsSource`
4. **El texto se borra** porque WPF no encuentra "Mar" en la nueva lista filtrada

### Solución Implementada
Usar **dos colecciones**: una disponible (todas las opciones) y una filtrada (mostrada), con un patrón de preservación de texto sincronizado.

---

## 📁 Archivos Necesarios

Para implementar un ComboBox con filtro, necesitas:

1. **Servicio de Autocompletado** - `*AutocompletadoService.cs`
2. **ViewModel** - Debe implementar `INotifyPropertyChanged`
3. **XAML** - ComboBox con bindings específicos
4. **Registro en DI** - Inyección de dependencias

---

## 🛠️ Paso a Paso: Crear un Servicio de Autocompletado

### Paso 1: Crear el Servicio

Ubicación: `Modules/GestionMantenimientos/Services/MarcaAutocompletadoService.cs`

```csharp
using GestLog.Modules.DatabaseConnection;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionMantenimientos.Services
{
    /// <summary>
    /// Servicio para obtener valores únicos de marca desde la tabla Equipos
    /// con funcionalidades de autocompletado y búsqueda.
    /// </summary>
    public class MarcaAutocompletadoService
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;

        public MarcaAutocompletadoService(IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        /// <summary>
        /// Obtiene todas las marcas únicas ordenadas por frecuencia de uso
        /// </summary>
        public async Task<List<string>> ObtenerTodosAsync()
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var list = await context.Equipos
                .Where(e => !string.IsNullOrEmpty(e.Marca))
                .GroupBy(e => e.Marca!.Trim().ToLower())
                .Select(g => new { Val = g.First().Marca!.Trim(), Cant = g.Count() })
                .OrderByDescending(x => x.Cant)  // Ordenar por frecuencia
                .ThenBy(x => x.Val)               // Luego por nombre
                .Select(x => x.Val)
                .ToListAsync();

            return list;
        }

        /// <summary>
        /// Obtiene las N marcas más utilizadas
        /// </summary>
        public async Task<List<string>> ObtenerMasUtilizadasAsync(int cantidad = 50)
        {
            return (await ObtenerTodosAsync()).Take(cantidad).ToList();
        }

        /// <summary>
        /// Busca marcas que contengan el filtro especificado
        /// </summary>
        public async Task<List<string>> BuscarAsync(string filtro)
        {
            if (string.IsNullOrWhiteSpace(filtro))
                return await ObtenerTodosAsync();

            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var filtroLower = filtro.Trim().ToLower();

            var list = await context.Equipos
                .Where(e => !string.IsNullOrEmpty(e.Marca) && e.Marca!.ToLower().Contains(filtroLower))
                .GroupBy(e => e.Marca!.Trim().ToLower())
                .Select(g => new { Val = g.First().Marca!.Trim(), Cant = g.Count() })
                .OrderByDescending(x => x.Cant)
                .ThenBy(x => x.Val)
                .Select(x => x.Val)
                .ToListAsync();

            return list;
        }
    }
}
```

### Paso 2: Registrar en Inyección de Dependencias

Ubicación: `Startup.UsuariosPersonas.cs`

```csharp
// En la sección de servicios de autocompletado:
services.AddScoped<GestLog.Modules.GestionMantenimientos.Services.MarcaAutocompletadoService>();
```

---

## 📱 Paso 3: Configurar el ViewModel

### Requisito: Implementar INotifyPropertyChanged

```csharp
public class EquipoDialogViewModel : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    protected void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }
    
    // ... resto del código
}
```

### Paso 3.1: Agregar Colecciones Filtradas

```csharp
public System.Collections.ObjectModel.ObservableCollection<string> MarcasDisponibles { get; set; } = 
    new System.Collections.ObjectModel.ObservableCollection<string>();

public System.Collections.ObjectModel.ObservableCollection<string> MarcasFiltradas { get; set; } = 
    new System.Collections.ObjectModel.ObservableCollection<string>();
```

### Paso 3.2: Agregar Banderas de Supresión y CancellationToken

```csharp
// Banderas para evitar bucles infinitos
private bool _suppressFiltroMarcaChanged = false;

// CancellationToken para debounce
private System.Threading.CancellationTokenSource? _marcaFilterCts;
```

### Paso 3.3: Crear la Propiedad Filtro con PropertyChanged

```csharp
private string filtroMarca = string.Empty;
public string FiltroMarca
{
    get => filtroMarca;
    set
    {
        // ✅ CRÍTICO: Si está suprimido, actualizar PERO lanzar PropertyChanged
        if (_suppressFiltroMarcaChanged)
        {
            filtroMarca = value ?? string.Empty;
            RaisePropertyChanged(nameof(FiltroMarca)); // ← FORZAR PropertyChanged
            return;
        }

        filtroMarca = value ?? string.Empty;
        RaisePropertyChanged(nameof(FiltroMarca)); // ← SIEMPRE disparar

        // Iniciar debounce de 250ms
        _marcaFilterCts?.Cancel();
        _marcaFilterCts?.Dispose();
        _marcaFilterCts = new System.Threading.CancellationTokenSource();
        var token = _marcaFilterCts.Token;

        _ = DebounceFiltrarMarcasAsync(token);
    }
}
```

### Paso 3.4: Crear Propiedad que Actualice el Filtro

```csharp
public string? Marca 
{ 
    get => Equipo.Marca; 
    set
    {
        Equipo.Marca = value;
        // ✅ Actualizar el filtro para disparar búsqueda
        FiltroMarca = value ?? string.Empty;
    }
}
```

### Paso 3.5: Cargar Datos Iniciales en Constructor

```csharp
public EquipoDialogViewModel(EquipoDto equipo)
{
    Equipo = equipo;
    
    // Inicializar colecciones
    MarcasDisponibles = new System.Collections.ObjectModel.ObservableCollection<string>();
    MarcasFiltradas = new System.Collections.ObjectModel.ObservableCollection<string>();
    
    // Agregar valor inicial si existe
    if (!string.IsNullOrWhiteSpace(Equipo.Marca) && !MarcasDisponibles.Contains(Equipo.Marca))
        MarcasDisponibles.Add(Equipo.Marca);
    if (!string.IsNullOrWhiteSpace(Equipo.Marca) && !MarcasFiltradas.Contains(Equipo.Marca))
        MarcasFiltradas.Add(Equipo.Marca);

    // Cargar marcas desde servicio
    try
    {
        var marcaService = ((App)System.Windows.Application.Current).ServiceProvider?
            .GetService(typeof(MarcaAutocompletadoService)) as MarcaAutocompletadoService;
        
        if (marcaService != null)
        {
            var items = Task.Run(() => marcaService.ObtenerMasUtilizadasAsync(50))
                .GetAwaiter().GetResult();
            
            foreach (var it in items)
            {
                if (!MarcasDisponibles.Contains(it)) MarcasDisponibles.Add(it);
                if (!MarcasFiltradas.Contains(it)) MarcasFiltradas.Add(it);
            }
        }
    }
    catch
    {
        // Ignorar fallos de carga
    }
}
```

### Paso 3.6: Métodos de Filtrado con Debounce

```csharp
/// <summary>
/// Debounce de 250ms antes de filtrar
/// </summary>
private async Task DebounceFiltrarMarcasAsync(System.Threading.CancellationToken token)
{
    try
    {
        await Task.Delay(250, token);
        if (token.IsCancellationRequested) return;
        await FiltrarMarcasAsync(token);
    }
    catch (OperationCanceledException)
    {
        // Ignorar cancelaciones
    }
    catch { }
}

/// <summary>
/// ✅ CRÍTICO: Preservar el texto mientras se actualiza ItemsSource
/// </summary>
private Task FiltrarMarcasAsync(System.Threading.CancellationToken cancellationToken)
{
    try
    {
        var svc = ((App)System.Windows.Application.Current).ServiceProvider?
            .GetService(typeof(MarcaAutocompletadoService)) as MarcaAutocompletadoService;
        
        if (svc == null) return Task.CompletedTask;
        
        var filtroActual = FiltroMarca ?? string.Empty;
        var items = Task.Run(() => svc.BuscarAsync(filtroActual))
            .GetAwaiter().GetResult();
        
        if (cancellationToken.IsCancellationRequested) return Task.CompletedTask;

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                // PASO 1: Guardar el texto ANTES de cambiar ItemsSource
                var textoPreservado = filtroMarca;

                // PASO 2: Limpiar la colección filtrada
                MarcasFiltradas.Clear();

                // PASO 3: Añadir nuevos items
                foreach (var it in items)
                    MarcasFiltradas.Add(it);

                // PASO 4: Forzar que el binding se actualice con el texto original
                _suppressFiltroMarcaChanged = true;
                filtroMarca = textoPreservado;
                RaisePropertyChanged(nameof(FiltroMarca)); // ← CRÍTICO
                _suppressFiltroMarcaChanged = false;
            }
            catch { }
        });
        
        return Task.CompletedTask;
    }
    catch { }
    return Task.CompletedTask;
}
```

---

## 🎨 Paso 4: Configurar XAML

```xaml
<ComboBox x:Name="CmbMarca"
          IsEditable="True" 
          IsTextSearchEnabled="False"
          ItemsSource="{Binding MarcasFiltradas}"
          Text="{Binding FiltroMarca, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
          Grid.Row="2" Grid.Column="1" Margin="0,4"/>
```

### Propiedades Importantes:

| Propiedad | Valor | Razón |
|-----------|-------|-------|
| `IsEditable` | `True` | Permitir escribir texto |
| `IsTextSearchEnabled` | `False` | No usar búsqueda automática de WPF |
| `ItemsSource` | `MarcasFiltradas` | Usar colección filtrada |
| `Text` | `FiltroMarca` (TwoWay) | Binding bidireccional del texto |
| `UpdateSourceTrigger` | `PropertyChanged` | Actualizar en cada tecla |

---

## ⚠️ Errores Comunes y Soluciones

### Error 1: "El texto se borra al seleccionar"
**Causa:** No preservar el texto antes de actualizar `ItemsSource`

**Solución:**
```csharp
// ❌ INCORRECTO
MarcasFiltradas.Clear();
foreach (var it in items) MarcasFiltradas.Add(it);

// ✅ CORRECTO
var textoPreservado = FiltroMarca;  // Guardar PRIMERO
MarcasFiltradas.Clear();
foreach (var it in items) MarcasFiltradas.Add(it);
_suppressFiltroMarcaChanged = true;
FiltroMarca = textoPreservado;      // Restaurar
RaisePropertyChanged(nameof(FiltroMarca));
_suppressFiltroMarcaChanged = false;
```

### Error 2: "PropertyChanged no se dispara cuando está suprimido"
**Causa:** Return sin disparar PropertyChanged en modo suprimido

**Solución:**
```csharp
// ❌ INCORRECTO
if (_suppressFiltroMarcaChanged)
{
    filtroMarca = value ?? string.Empty;
    return;  // No dispara PropertyChanged
}

// ✅ CORRECTO
if (_suppressFiltroMarcaChanged)
{
    filtroMarca = value ?? string.Empty;
    RaisePropertyChanged(nameof(FiltroMarca)); // ← SIEMPRE disparar
    return;
}
```

### Error 3: "La lista se actualiza demasiado rápido"
**Causa:** No usar debounce

**Solución:**
```csharp
// ✅ Usar debounce de 250ms
private async Task DebounceFiltrarMarcasAsync(System.Threading.CancellationToken token)
{
    await Task.Delay(250, token);  // Esperar 250ms
    if (token.IsCancellationRequested) return;
    await FiltrarMarcasAsync(token);
}
```

### Error 4: "El servicio no se encuentra (null)"
**Causa:** No registrado en `Startup.UsuariosPersonas.cs`

**Solución:**
```csharp
// En Startup.UsuariosPersonas.cs agregar:
services.AddScoped<MarcaAutocompletadoService>();
```

### Error 5: "La lista está vacía"
**Causa:** Usar colección equivocada en `ItemsSource`

**Solución:**
```xaml
<!-- ❌ INCORRECTO: Esto no se actualiza -->
ItemsSource="{Binding MarcasDisponibles}"

<!-- ✅ CORRECTO: Usar colección filtrada -->
ItemsSource="{Binding MarcasFiltradas}"
```

---

## 📊 Flujo de Ejecución

```
Usuario escribe "Mar" en el ComboBox
    ↓
FiltroMarca setter se dispara
    ↓
RaisePropertyChanged(nameof(FiltroMarca))
    ↓
DebounceFiltrarMarcasAsync inicia (delay 250ms)
    ↓
Mientras se esperan 250ms, usuario sigue escribiendo
    ↓
Si escribe más, anterior CancellationToken se cancela
    ↓
Nuevo DebounceFiltrarMarcasAsync inicia
    ↓
Después de 250ms sin cambios, ejecuta FiltrarMarcasAsync
    ↓
FiltrarMarcasAsync:
  1. Guarda textoPreservado = "Mar"
  2. Limpia MarcasFiltradas
  3. Llama svc.BuscarAsync("Mar")
  4. Obtiene ["Marca1", "Marca2"]
  5. Añade a MarcasFiltradas
  6. Restaura FiltroMarca = "Mar"
  7. Dispara PropertyChanged
    ↓
ComboBox actualiza ItemsSource pero mantiene Text = "Mar"
    ↓
Dropdown muestra ["Marca1", "Marca2"]
```

---

## ✅ Checklist de Implementación

- [ ] Crear servicio `*AutocompletadoService.cs` con métodos: `ObtenerTodosAsync()`, `ObtenerMasUtilizadasAsync()`, `BuscarAsync(filtro)`
- [ ] Registrar en `Startup.UsuariosPersonas.cs` con `AddScoped`
- [ ] ViewModel implementa `INotifyPropertyChanged`
- [ ] Agregar colecciones: `*Disponibles` y `*Filtradas`
- [ ] Agregar banderas: `_suppress*Changed` y `_*FilterCts`
- [ ] Crear propiedad filtro con PropertyChanged forzado incluso cuando suprimido
- [ ] Implementar método `Debounce*Async()` con delay 250ms
- [ ] Implementar método `Filtrar*Async()` que preserve texto
- [ ] En constructor, cargar datos iniciales
- [ ] En XAML: `ItemsSource="{Binding *Filtradas}"` Y `Text="{Binding Filtro*, ...}"`
- [ ] Compilar y probar

---

## 🧪 Verificación Final

1. **Abrir el diálogo de Equipo**
2. **Escribir en el ComboBox** (ej: "Mar")
   - ✅ El texto debe permanecer visible
   - ✅ La lista se debe filtrar después de 250ms
3. **Seleccionar un item de la lista**
   - ✅ El valor debe guardarse
   - ✅ El diálogo debe permitir guardar
4. **Editar un Equipo existente**
   - ✅ El valor existente debe aparecer
   - ✅ Debe ser editable

---

## 📚 Referencias

- **Archivos de Implementación:**
  - `Views/Tools/GestionMantenimientos/EquipoDialog.xaml.cs`
  - `Views/Tools/GestionMantenimientos/EquipoDialog.xaml`
  - `Modules/GestionMantenimientos/Services/MarcaAutocompletadoService.cs`

- **Patrón Basado En:**
  - `Views/Tools/GestionEquipos/PerifericoDialog.xaml.cs` (MVVM Toolkit)
  - Solución de race conditions en WPF ComboBox

---

## 🤝 Soporte

Si tienes problemas:

1. Verifica que el servicio está registrado en DI
2. Comprueba que `RaisePropertyChanged` se dispara
3. Usa breakpoints en `FiltrarMarcasAsync` para debug
4. Asegúrate que `ItemsSource="{Binding *Filtradas}"` (no `*Disponibles`)

