# 📖 README - Gestión de Equipos Informáticos

## 🏗️ Estructura del Módulo

Este módulo está organizado siguiendo el patrón de separación de responsabilidades (SRP):

```
GestionEquiposInformaticos/
├── Services/                  # Lógica de negocio por tipo
│   ├── Data/                  # CRUD y operaciones de datos
│   ├── Autocomplete/          # Servicios de autocompletado
│   ├── Dialog/                # Servicios de presentación (diálogos)
│   └── ServiceCollectionExtensions.cs
├── Interfaces/                # Contratos espejo de Services
│   ├── Data/
│   ├── Autocomplete/
│   └── Dialog/
├── ViewModels/                # Lógica de presentación por feature
│   ├── Equipos/
│   ├── Cronograma/
│   ├── Mantenimiento/
│   └── Perifericos/
├── Views/                     # Vistas XAML por feature
│   ├── Equipos/
│   ├── Cronograma/
│   ├── Mantenimiento/
│   └── Perifericos/
├── Models/                    # DTOs, Entities, Enums
│   ├── DTOs/
│   ├── Entities/
│   └── Enums/
├── Messages/                  # Mensajería MVVM (opcional)
└── README.md

```

## 🔐 Permisos del Módulo

Este módulo implementa control granular de permisos para todas las acciones disponibles en la UI y lógica de negocio. Los permisos se gestionan por usuario y se consultan mediante la clase `CurrentUserInfo` y el método `HasPermission(string permiso)`. Todas las acciones relevantes están protegidas y reflejadas visualmente en la interfaz.

### Permisos implementados:

- `EquiposInformaticos.AccederModulo` — Permite acceder al módulo de gestión de equipos informáticos.
- `EquiposInformaticos.CrearEquipo` — Permite crear (agregar) un nuevo equipo informático, incluyendo gestión de RAM y discos.
- `EquiposInformaticos.EditarEquipo` — Permite editar los datos de un equipo existente.
- `EquiposInformaticos.DarDeBajaEquipo` — Permite dar de baja un equipo informático.
- `EquiposInformaticos.VerHistorial` — Permite ver el historial de cambios y detalles de equipos.
- `EquiposInformaticos.ExportarDatos` — Permite exportar datos de equipos a diferentes formatos.
- `EquiposInformaticos.AsignarCronograma` — Permite asignar cronogramas de mantenimiento a equipos.
- `EquiposInformaticos.LiberarCronograma` — Permite liberar cronogramas asignados a equipos.
- `Herramientas.AccederEquiposInformaticos` — Permiso adicional para acceder a herramientas específicas del módulo.

### Uso en ViewModel

Cada permiso se expone como una propiedad booleana en el ViewModel principal (`AgregarEquipoInformaticoViewModel` y otros relacionados):

```csharp
public bool CanAccederModulo => _currentUser.HasPermission("EquiposInformaticos.AccederModulo");
public bool CanCrearEquipo => _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");
public bool CanEditarEquipo => _currentUser.HasPermission("EquiposInformaticos.EditarEquipo");
public bool CanDarDeBajaEquipo => _currentUser.HasPermission("EquiposInformaticos.DarDeBajaEquipo");
public bool CanVerHistorial => _currentUser.HasPermission("EquiposInformaticos.VerHistorial");
public bool CanExportarDatos => _currentUser.HasPermission("EquiposInformaticos.ExportarDatos");
public bool CanAsignarCronograma => _currentUser.HasPermission("EquiposInformaticos.AsignarCronograma");
public bool CanLiberarCronograma => _currentUser.HasPermission("EquiposInformaticos.LiberarCronograma");
public bool CanAccederHerramientas => _currentUser.HasPermission("Herramientas.AccederEquiposInformaticos");
```

Estas propiedades se recalculan de forma reactiva al cambiar el usuario o sus roles.

### Uso en la UI

Los controles de la UI (botones, comandos, acciones) enlazan `IsEnabled` y `Opacity` a las propiedades de permiso usando el convertidor `BooleanToOpacityConverter`:

```xaml
<Button Content="Agregar equipo" IsEnabled="{Binding CanCrearEquipo}" Opacity="{Binding CanCrearEquipo, Converter={StaticResource BooleanToOpacityConverter}}" />
<Button Content="Editar equipo" IsEnabled="{Binding CanEditarEquipo}" Opacity="{Binding CanEditarEquipo, Converter={StaticResource BooleanToOpacityConverter}}" />
<Button Content="Ver historial" IsEnabled="{Binding CanVerHistorial}" Opacity="{Binding CanVerHistorial, Converter={StaticResource BooleanToOpacityConverter}}" />
<Button Content="Exportar datos" IsEnabled="{Binding CanExportarDatos}" Opacity="{Binding CanExportarDatos, Converter={StaticResource BooleanToOpacityConverter}}" />
<Button Content="Dar de baja" IsEnabled="{Binding CanDarDeBajaEquipo}" Opacity="{Binding CanDarDeBajaEquipo, Converter={StaticResource BooleanToOpacityConverter}}" />
<!-- ...otros controles... -->
```

### Validación en lógica de negocio

Todos los comandos usan `[RelayCommand(CanExecute = nameof(Can[Accion]))]` para habilitar/deshabilitar acciones según permisos. La lógica de negocio valida los permisos antes de ejecutar cualquier acción sensible.

### Documentación y mantenimiento

- Los permisos están documentados aquí y en `copilot-instructions.md`.
- Para agregar un nuevo permiso:
  1. Defínelo en la base de datos y sistema de autenticación.
  2. Declara la propiedad en el ViewModel.
  3. Enlaza en la UI.
  4. Documenta aquí y en copilot-instructions.md.

---

## 🔧 Registro de Servicios en DI

Todos los servicios del módulo se registran a través de `ServiceCollectionExtensions.cs`:

```csharp
// En Startup.UsuariosPersonas.cs
services.AddGestionEquiposInformaticosServices();
```

### Para agregar un nuevo servicio:

1. **Crea la interfaz** en la carpeta correspondiente:
   - `Interfaces/Data/` para servicios CRUD
   - `Interfaces/Autocomplete/` para autocompletado
   - `Interfaces/Dialog/` para diálogos

2. **Implementa el servicio** en la carpeta correspondiente de `Services/`

3. **Registra en ServiceCollectionExtensions.cs**:
```csharp
public static IServiceCollection AddGestionEquiposInformaticosServices(this IServiceCollection services)
{
    // ...
    services.AddScoped<IMyService, MyService>(); // Nuevo servicio
    // ...
    return services;
}
```

---

*Actualizado: Diciembre 2025*  
*Versión: 2.0 (Refactorización de Services e Interfaces)*
