# Ventanas modales en GestLog

Guía práctica para crear y abrir diálogos modales que se vean bien en toda la aplicación.

Este documento reemplaza la idea anterior de depender de un archivo global de estilos para modales. Hoy lo importante es el patrón de apertura, el owner correcto y un overlay que cubra toda la ventana padre.

## Objetivo visual

Un modal correcto debe:

- abrir sobre la ventana activa o la ventana principal
- cubrir toda la pantalla visible del padre con un overlay oscuro
- centrar el card del formulario dentro de la ventana modal
- bloquear la interacción con el contenido de fondo
- cerrar con botón, tecla Escape o acción del ViewModel

## Patrón recomendado

### 1) Ventana transparente con overlay completo

La ventana modal debe usar:

- `WindowStyle="None"`
- `AllowsTransparency="True"`
- `Background="Transparent"`
- `ShowInTaskbar="False"`
- `WindowState="Maximized"` o una configuración equivalente que cubra todo el owner
- `WindowStartupLocation="CenterOwner"` cuando el modal depende del padre

El contenido raíz debe ser un `Grid` que ocupe todo el espacio y tenga un fondo semitransparente, por ejemplo:

```xaml
<Grid Background="#A0000000">
    <!-- card centrado -->
</Grid>
```

Ese fondo es el overlay. Si el overlay no cubre toda la pantalla, normalmente el problema no está en el `Border` central, sino en cómo se creó la ventana o quién es el owner.

### 2) El card va centrado y no controla el overlay

El panel central debe ser un `Border` o tarjeta con:

- `HorizontalAlignment="Center"`
- `VerticalAlignment="Center"`
- `Margin` para respirar visualmente
- tamaño adaptativo, preferiblemente con límites razonables

El `Border` del card no debe ser el responsable de cubrir la pantalla. Eso lo hace el `Grid` raíz.

## Cómo abrir correctamente un modal

### Desde un servicio de diálogo

La forma más estable es abrir el modal usando la ventana activa como owner:

```csharp
var owner = Application.Current.Windows
    .OfType<Window>()
    .FirstOrDefault(w => w.IsActive)
    ?? Application.Current.MainWindow;

var window = new TipoDocumentoModalWindow(vm)
{
    Owner = owner
};

window.ShowDialog();
```

### Desde un ViewModel

Si el modal se abre desde un ViewModel, el owner también debe venir de la ventana activa o de `Application.Current.MainWindow`.

La regla es simple: **no asumir que `MainWindow` siempre es el owner correcto**. En varias pantallas eso no basta, sobre todo cuando el usuario llegó desde otra ventana intermedia.

## Caso de PersonaRegistroWindow

El modal de Personas funciona bien porque sigue un patrón simple:

- ventana sin bordes
- overlay completo
- owner correcto
- apertura con `ShowDialog()`

Ese es el patrón que conviene replicar para otros catálogos.

## Qué evitar

- no usar `SizeToContent` junto con `WindowState="Maximized"`
- no depender de tamaños fijos para cubrir el overlay
- no abrir el modal sin owner
- no usar una ventana con `Topmost="True"` para simular modal
- no poner la lógica del overlay dentro del card central
- no depender de estilos externos desactualizados si ya no forman parte del flujo actual

## Cierre del modal

El modal debe cerrarse por cualquiera de estas vías:

- botón Cancelar o Cerrar
- tecla Escape
- evento/comando del ViewModel cuando el guardado fue exitoso

Ejemplo de cierre simple:

```csharp
private void BtnCerrar_Click(object sender, RoutedEventArgs e)
{
    Close();
}
```

## Recomendación para el ViewModel

Si el guardado debe cerrar la ventana, el ViewModel puede exponer un evento como `SolicitarCerrarModal` o una acción equivalente. El código-behind suscribe ese evento y cierra la ventana.

La idea es que el ViewModel no se encargue del tamaño ni de la posición del modal, sólo del flujo funcional.

## Checklist rápido

- `Owner` asignado antes de `ShowDialog()`
- modal transparente y sin borde nativo
- `Grid` raíz con overlay oscuro
- card centrado dentro del overlay
- botón cerrar visible y funcional
- Escape para cerrar
- sin dependencia de `ModalWindowsStandard.xaml`

## Nota final

Si un modal se abre en la esquina superior izquierda, el problema suele ser uno de estos:

1. no tiene owner correcto
2. la ventana no está maximizada o no ocupa todo el área
3. el `Grid` raíz no está expandiéndose
4. el contenido se está renderizando con un tamaño no esperado por `SizeToContent`

En esta base de código, el comportamiento correcto es: **owner correcto + ventana modal transparente + overlay completo + card centrado**.

**Última actualización**: 2026-04-10
