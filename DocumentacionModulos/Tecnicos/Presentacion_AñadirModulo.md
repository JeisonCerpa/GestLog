# Presentación: Cómo añadir un módulo a GestLog (texto para leer)

Este documento es el guion listo para leer en una reunión o enviar por email/Slack. Está escrito en lenguaje sencillo, para que cualquier persona del equipo —incluso novatos— entienda qué hacer, cómo y por qué.

Duración recomendada: 10–20 minutos.

---

Apertura (30–60 segundos)

"Hoy voy a explicar, paso a paso y con palabras sencillas, cómo crear un módulo nuevo en GestLog. Al final sabréis qué carpetas crear, qué código poner en cada archivo, cómo registrar las clases en el contenedor de dependencias (DI), cómo mostrar la tarjeta en Herramientas con permisos, y cómo conectar a la base de datos si hace falta. La guía está en DocumentacionModulos/Tecnicos/0.GuíaCompletaAgregarModulo.md — pero aquí tenéis un texto para leer en la reunión."

Objetivo (15 segundos)

"Objetivo: que cualquiera del equipo pueda crear un módulo autocontenido, probarlo y subir un PR siguiendo un checklist claro." 

Audiencia

- Desarrolladores del equipo, nivel: desde novato hasta avanzado. 

---

1) Qué es un módulo y por qué lo usamos (1 minuto)

- Un módulo agrupa funcionalidad relacionada: vista (UI), ViewModel (datos/acciones), modelos, interfaces y servicios. 
- Ventaja: encapsula todo junto, facilita pruebas, revisiones y mantenimiento. 
- Meta: que el módulo sea independiente y fácil de añadir o quitar.

---

2) Estructura mínima que debes crear (30 segundos)

Crea la carpeta: `Modules/MiModulo` con estas subcarpetas y archivos:
- `ViewModels/MiModuloViewModel.cs`
- `Views/MiModuloView.xaml` (+ .xaml.cs si hace falta)
- `Models/MiModuloModel.cs`
- `Interfaces/IMiModuloService.cs`
- `Services/MiModuloService.cs` (empezamos en memoria)
- `DocumentacionModulos/MiModulo-Usuario.md` (instrucciones para usuarios)
- `README.md` del módulo (dependencias, migraciones si aplica)

Explicación simple: así todo el código del módulo está en un solo lugar.

---

3) Código clave y explicación línea por línea (el núcleo, 5–8 minutos)

A continuación tienes los fragmentos esenciales y lo que significan. Pega estos ejemplos tal cual cuando estés creando el módulo.

A) ViewModel (controla los datos y los comandos)

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

public partial class MiModuloViewModel : ObservableObject
{
    [ObservableProperty]
    private string mensaje = "¡Bienvenido a Mi Módulo!";

    [ObservableProperty]
    private ObservableCollection<MiModuloModel> elementos = new();

    private readonly IMiModuloService _service;

    public MiModuloViewModel(IMiModuloService service)
    {
        _service = service;
        Cargar();
    }

    [RelayCommand]
    private void Cargar()
    {
        Elementos = new ObservableCollection<MiModuloModel>(_service.ObtenerElementos());
    }

    [RelayCommand]
    private void MostrarMensaje()
    {
        Mensaje = "Botón pulsado";
    }
}
```

Explicación en lenguaje sencillo:
- `ObservableObject`: permite avisar a la UI cuando cambian las propiedades.
- `[ObservableProperty]` genera automáticamente la propiedad pública y el aviso de cambio; evita escribir mucho código repetido.
- `ObservableCollection<MiModuloModel>` es una lista que la UI actualiza cuando se modifica.
- El constructor recibe `IMiModuloService` por inyección (no crea el servicio él mismo). Eso facilita pruebas y mantenimiento.
- `[RelayCommand]` convierte métodos en comandos que la vista puede ejecutar (p. ej. al pulsar un botón).

Qué decir en la reunión: "El ViewModel no debe acceder a la base de datos directamente; pide el servicio y usa sus métodos." 

B) Model (datos)

```csharp
public class MiModuloModel
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string Descripcion { get; set; }
}
```

Explicación: clase simple que representa cada fila/elemento.

C) Interfaz del servicio (contrato)

```csharp
using System.Collections.Generic;

public interface IMiModuloService
{
    List<MiModuloModel> ObtenerElementos();
}
```

Explicación: define lo que el ViewModel necesita; permite cambiar la implementación sin tocar el ViewModel.

D) Implementación en memoria (para empezar rápido)

```csharp
using System.Collections.Generic;

public class MiModuloService : IMiModuloService
{
    public List<MiModuloModel> ObtenerElementos()
    {
        return new List<MiModuloModel>
        {
            new MiModuloModel { Id = 1, Nombre = "Ejemplo", Descripcion = "Primer elemento" }
        };
    }
}
```

Explicación: útil para desarrollar UI sin configurar base de datos. Cuando la pantalla esté lista, cambiamos a una versión que use repositorio/DB.

E) Vista XAML (enlace con ViewModel)

```xml
<Window ...>
  <StackPanel>
    <TextBlock Text="{Binding Mensaje}" />
    <Button Content="Cargar" Command="{Binding CargarCommand}" />
    <ListBox ItemsSource="{Binding Elementos}" DisplayMemberPath="Nombre" />
  </StackPanel>
</Window>
```

Explicación simple: la vista no contiene lógica; sólo muestra propiedades y ejecuta comandos del ViewModel.

---

4) Registrar las clases en DI (qué editar y por qué, 1 minuto)

En `Startup.*.cs` o `Program.cs` añade:

```csharp
services.AddTransient<IMiModuloService, MiModuloService>();
services.AddTransient<MiModuloViewModel>();
```

Por qué:
- `AddTransient` crea una instancia nueva cada vez; es lo usual para ViewModels y servicios sin estado.
- Si más adelante el servicio usa DbContext, cambia a `AddScoped` para el servicio y el repositorio, y crea un scope cuando resuelvas el ViewModel desde la UI.

Qué decir: "Registrar en DI le dice a la app cómo construir las clases. No crees instancias con new en la UI: pide al contenedor." 

---

5) Añadir la tarjeta (card) en Herramientas y control de permisos (2 minutos)

Pasos concretos:
1. Define el permiso: `Herramientas.AccederMiModulo`.
2. En `HerramientasViewModel` añade una propiedad booleana que indique si el usuario puede ver la card:

```csharp
[ObservableProperty]
private bool canAccessMiModulo;

private void RecalcularPermisos()
{
    CanAccessMiModulo = _currentUser.HasPermission("Herramientas.AccederMiModulo");
}
```

3. En `HerramientasView.xaml` añade la card (o botón) copiando otra ya existente, por ejemplo:

```xml
<Button Content="Mi Módulo"
        Click="BtnMiModulo_Click"
        Visibility="{Binding CanAccessMiModulo, Converter={StaticResource BooleanToVisibilityConverter}}" />
```

4. Implementa el manejador `BtnMiModulo_Click` siguiendo el patrón del proyecto:

```csharp
using var scope = serviceProvider.CreateScope();
var vm = scope.ServiceProvider.GetRequiredService<MiModuloViewModel>();
var view = new Modules.MiModulo.Views.MiModuloView { DataContext = vm };
_mainWindow.NavigateToView(view, "Mi Módulo");
```

Explicación sencilla:
- Se crea un scope para respetar `Scoped` (si tus servicios usan DbContext).
- Se resuelve el ViewModel con DI y se asigna como DataContext de la vista.

Qué decir: "Copiad el patrón usado por otras tarjetas ya existentes en `Views/Tools/*` para asegurar consistencia." 

---

6) Si tenéis que usar base de datos: repositorio, DbContext y migraciones (2–3 minutos)

Resumen básico:
- Crea la entidad `MiModuloEntity` similar al Model.
- Añade `DbSet<MiModuloEntity> MiModulo` en `AppDbContext`.
- Registra `AppDbContext` en DI con la cadena de conexión en appsettings.json:

```csharp
services.AddDbContext<AppDbContext>(options =>
  options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
```

- Implementa `MiModuloRepository` que use AppDbContext y tenga métodos async (ObtenerAsync, GuardarAsync, EliminarAsync).
- Cuando todo esté listo, crear migración y aplicar:
  - `dotnet ef migrations add MiModulo_Initial -s GestLog.csproj -p GestLog.csproj`
  - `dotnet ef database update -s GestLog.csproj -p GestLog.csproj`

Explicación para el equipo: "Empezad en memoria. Cuando la interfaz esté estable, añadid la persistencia con repositorio y migraciones." 

---

7) Transacciones y manejo de errores (breve)

- Si haces varias operaciones que deben fallar juntas, usa transacción con `_db.Database.BeginTransactionAsync()` y `CommitAsync()/RollbackAsync()`.
- Siempre capturar excepciones, registrar con `ILogger` y mostrar un mensaje amable al usuario (no el stack trace).

---

8) Checklist final que deben revisar antes de abrir PR (leer en voz alta)

- [ ] Carpeta `Modules/MiModulo` creada con archivos.
- [ ] Servicios y ViewModels registrados en DI.
- [ ] Permiso `Herramientas.AccederMiModulo` creado y comprobado.
- [ ] Card añadida en `HerramientasView` y flag en `HerramientasViewModel`.
- [ ] Documentación de usuario en `DocumentacionModulos/MiModulo-Usuario.md`.
- [ ] README del módulo con dependencias y pasos para migraciones.

Usad este checklist como parte de la descripción del PR.

---

9) Errores comunes y cómo solucionarlos (rápido)

- "La card no aparece": comprobar que `CanAccessMiModulo` devuelve true y que el permiso está registrado.
- "DbContext disposed": olvidaste crear scope o registraste DbContext con lifetime incorrecto.
- "Bindings no funcionan": verificar DataContext y nombres de propiedades (propiedades deben ser públicas y levantarse con OnPropertyChanged).

---

Cierre y llamada a la acción (30 segundos)

- "Si queréis, puedo crear ahora el módulo MVP en `Modules/MiModulo` y abrir un PR con el checklist completo. También puedo generar un script PowerShell para crear plantillas automáticamente. ¿Cuál preferís?"

---

Notas para quien presenta

- Habla despacio y usa ejemplos en pantalla (mostrar el archivo de la guía mientras explicas).
- Si alguien no entiende un término (DI, DbContext, Scoped), detente y da una explicación simple y rápida.
- Invita a que alguien del equipo haga el primer PR con supervisión.

---

Fin del guion.

Si quieres, lo convierto en un archivo PPTX con estas diapositivas o creo el módulo MVP ahora. Indica la opción y la hago.
