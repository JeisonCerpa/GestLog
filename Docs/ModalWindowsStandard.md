Estándar para ventanas modales (Agregar / Editar / Dialog)
===========================================================

Propósito
--------
Documento de referencia para diseñar y desarrollar ventanas modales coherentes en GestLog: ventanas de tipo agregar, editar, detalle y dialog. Describe estructura visual, comportamiento (overlay, cierre), accesibilidad y recursos recomendados para mantener consistencia entre vistas como `PlanDetalleModalWindow` y `RegistroEjecucionPlanDialog`.

Principios generales
--------------------
- Visual coherente con la identidad (colores, corner radius, sombras suaves).
- Overlay semitransparente que cubre la ventana padre y evita interacción detrás del modal.
- Ventana centrada con tarjeta (card) como contenedor del contenido.
- Controles accesibles (contraste alto para acciones principales y para botón cerrar "X").
- Evitar animaciones innecesarias al abrir (sin fade por defecto) para respuestas inmediatas.

Anatomía de la ventana
----------------------
1. Window (host):
   - WindowStyle="None"
   - AllowsTransparency="True"
   - Background="Transparent"
   - WindowStartupLocation="CenterOwner"
   - SizeToContent: recomendamos usar Manual cuando se necesita que el overlay cubra la ventana owner y se usará un método como `ConfigurarParaVentanaPadre` que fije Left/Top/Width/Height. En casos simples (diálogos pequeños sin overlay que cubra la aplicación) puede usarse Width/Height o SizeToContent.
   - Topmost: No establecer Topmost="True" en modales estándar. Dejar la propiedad sin especificar (o explícitamente Topmost="False") es la recomendación. Usar Owner + ShowDialog() y, cuando sea necesario, `ConfigurarParaVentanaPadre(owner)` garantiza que el diálogo quede encima de la ventana padre sin forzarlo por encima de otras aplicaciones. Solo usar Topmost para alertas críticas que deben permanecer sobre todas las ventanas del sistema.

2. Overlay (Grid raíz):
   - Debe cubrir todo el área del Window (el Window debe haberse dimensionado para cubrir la ventana padre cuando se espera bloquearla).
   - Background recomendado: #80000000 (50% negro) — igual para todas las modales para consistencia.
   - MouseLeftButtonDown -> cierra el diálogo (comportamiento configurable).
   - No animación de fade por defecto (se removió DoubleAnimation de Opacity en estándares actuales).

3. Panel central (Border/Card):
   - Centrado HorizontalAlignment="Center" VerticalAlignment="Center".
   - MaxWidth / MaxHeight para limitar tamaño en pantallas grandes (ej. MaxWidth=880, MaxHeight=760).
   - CornerRadius entre 6 y 10 px (ej. 8).
   - Fondo: Surface/White.
   - Borde sutil y sombra ligera (DropShadowEffect) para elevar sobre el overlay.
   - MouseLeftButtonDown en el panel debe hacer e.Handled = true para evitar cierre por clic fuera.

4. Header
   - Barra superior con fondo de color primario (gradient opcional) y texto del título a la izquierda.
   - Botón de cierre X en la esquina superior derecha: usar glyph MDL2 (\uE711) con estilo de alto contraste (Foreground blanco, fondo transparente). Tamaño 36x36 y CornerRadius pequeño.
   - El X debe tener ToolTip "Cerrar (Esc)" y Click ligado a cerrar (DialogResult=false / Close()).

5. Cuerpo
   - Estructura en columnas si procede (ej. checklist a la izquierda y tarjeta lateral con información a la derecha).
   - Evitar que controles multilínea expandan la ventana: fijar Height y usar VerticalScrollBarVisibility="Auto" en TextBox/ScrollViewer internos.
   - Wrapping en TextBlocks y TextBoxes donde el texto pueda crecer.

6. Footer / Acciones
   - Botón principal "Guardar" estilizado y prominente (Primary/GuardarButtonStyle), con IsDefault="True" para activarse con Enter.
   - Evitar colocar botón cerrar redundante si ya existe X en header; si hay, usar estilo GhostButton.
   - Confirmar que GuardarCommand (o lógica) cierre el diálogo y notifique al caller (evento OnEjecucionRegistrada o similar).

Comportamientos y handlers recomendados
--------------------------------------
- Cierre por clic en overlay: Grid.MouseLeftButtonDown -> método que cierra (DialogResult=false; Close()).
- Evitar cierre cuando se clica dentro del panel: Panel.MouseLeftButtonDown -> e.Handled = true.
- Tecla Escape: registrar KeyDown en el Window y cerrar si Key == Key.Escape.
- Owner y cobertura: al mostrar desde un servicio, asignar Owner al Window activo y llamar a un helper `ConfigurarParaVentanaPadre(Window parent)` que:
  - Siempre usar `WindowState = WindowState.Maximized` para cubrir toda la pantalla.
  - Esta es la forma estándar y robusta de WPF para cubrir la pantalla sin problemas de DPI o pantallas múltiples.
  - No calcular bounds manualmente ni usar TransformFromDevice, ya que introducen errores de alineamiento y desbordamientos en pantallas múltiples.
  - El Owner aseguura que el diálogo quede siempre encima de la ventana padre y que se cierre cuando el padre se cierra.

Tamaño y layout
----------------
- Usar MaxWidth/MaxHeight en Border principal para limitar crecimiento.
- Tener en cuenta DPI y scaling; probar en 125%/150% y pantallas múltiples.
- Campos de observaciones largos: TextBox con Height fijo (ej. 160px) y VerticalScrollBarVisibility="Auto" para evitar que la ventana crezca.

Estado y bindings
-----------------
- Unificar observaciones por modal: usar una propiedad en el ViewModel, p. ej. `ObservacionCentral : string` enlazada a la TextBox.
- Mantener bindings clave: Checklist, CompletedItems, TotalItems, PercentComplete, ResumenEnVivo, SemanaISO, CodigoEquipo, ResponsablePlan, GuardarCommand.

Estilos y recursos
------------------
- Definir en recursos compartidos (App.xaml o ResourceDictionary del módulo):
  - Brushes: PrimaryBrush, AccentBrush, SurfaceBrush, LightGrayBrush, BorderBrush.
  - Botones: PrimaryButtonStyle, GhostButton, CloseButtonStyle, GuardarButtonStyle.
  - Efectos: DropShadowEffect para ventana y secciones.
  - Iconos: registrar glyphs MDL2 como recursos reutilizables (DrawingImage, Geometry o estilos) para evitar duplicación. Ejemplo recomendado en un ResourceDictionary `ModalIcons.xaml`:
    - Clave: `Icon_Close_Glyph` = "\uE711" (usar FontFamily="Segoe MDL2 Assets" al mostrar).
    - Alternativa: exponer como `DrawingImage` o `Geometry` para usar en `Image`/`Path` y controlar tintado (Foreground).
    - Uso en XAML: `<TextBlock Text="{StaticResource Icon_Close_Glyph}" FontFamily="Segoe MDL2 Assets"/>` o `<Path Data="{StaticResource Icon_Close_Geometry}" Fill="White"/>`.

Animaciones
-----------
- Política actual: sin animación de fade al mostrar. Las transiciones deben ser opcionales y consistentes; si se añaden, documentar y usar la misma duración para todas las modales.

Accesibilidad
-------------
- Contraste alto para acciones importantes (guardar, cerrar).
- Asegurar navegación por teclado (TabIndex consistente, IsDefault para guardar, focus visual claro).
- ToolTips en iconos (Cerrar) y etiquetas claras en campos.

Implementación en servicios
---------------------------
- Al instanciar un modal desde un servicio: asignar `Owner = ventanaActiva` y, si se desea bloquear cobertura, llamar a `dlg.ConfigurarParaVentanaPadre(owner)` antes de ShowDialog().
- Manejar resultado de ShowDialog() y exponer un valor booleano indicando si se guardó o no.

Checklist para añadir una nueva ventana modal
--------------------------------------------
- [ ] Crear XAML siguiendo la estructura: Grid overlay -> Border card -> Header/Cuerpo/Footer.
- [ ] Usar Background del overlay: `{StaticResource ModalOverlayBrush}` o literal `#80000000`.
- [ ] Añadir CloseButton en header con estilo CloseButton.
- [ ] Implementar Panel_MouseLeftButtonDown -> e.Handled = true y Overlay_MouseLeftButtonDown -> cierre.
- [ ] Registrar KeyDown para Escape en el code-behind.
- [ ] Si la ventana debe bloquear toda la app, en el servicio asignar Owner y llamar ConfigurarParaVentanaPadre.
- [ ] Fijar Height y VerticalScrollBarVisibility en cajas de texto multilinea para evitar crecimiento del Window.
- [ ] Añadir GuardarCommand en ViewModel y exponer evento de éxito para que el dialog retorne DialogResult true.

Secciones añadidas y notas prácticas
====================================

Recursos expuestos por ModalWindowsStandard.xaml
------------------------------------------------
El ResourceDictionary creado (`ModalWindowsStandard.xaml`) incluye (claves relevantes):

- Brushes
  - `PrimaryBrush` (verde corporativo #118938)
  - `PrimaryLightBrush` (verde secundario)
  - `AccentBrush`, `AccentLightBrush`
  - `SurfaceBrush` (blanco de tarjeta)
  - `LightGrayBrush`, `BorderBrush`, `BorderLightBrush`
  - `TextPrimaryBrush`, `TextMutedBrush`
  - `ErrorBrush`, `WarningBrush`

- Effects
  - `WindowShadow`, `SectionShadow`, `HeaderShadow` (DropShadowEffect optimizados)

- Text Styles
  - `HeaderTextStyle`, `SubHeaderTextStyle`, `SectionTitleStyle`, `LabelTextStyle`, `ValueTextStyle`, `StatusText`

- Controls / Styles
  - `CorporateDataGrid`, `DataGridHeader`
  - `CloseButton` (botón X de header)
  - `PrimaryButtonStyle`, `DangerButtonStyle` (botones estándar)
  - `HeaderActionButtonStyle` (botones de header: fondo blanco, texto verde, Padding aumentado y MinWidth)
  - `StatusBadge` (badge con DataTriggers en vistas que lo usen)

Uso recomendado de `HeaderActionButtonStyle`
-------------------------------------------
- Diseñado para botones en el header sobre fondos primarios (gradiente verde).
- Proporciona mayor contraste (fondo blanco, texto/ícono en `PrimaryBrush`) y espacio interior (Padding aumentado y `MinWidth`) para evitar texto pegado a bordes.
- Ejemplo de uso en XAML:

```xaml
<Button Style="{StaticResource HeaderActionButtonStyle}" Click="BtnEditar_Click"> 
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="" FontFamily="Segoe MDL2 Assets"/>
        <TextBlock Text="Editar"/>
    </StackPanel>
</Button>
```

Visibilidad condicional / permisos
----------------------------------
- Para ocultar/mostrar acciones según permisos, enlazar `Visibility` a una propiedad booleana del ViewModel usando `BooleanToVisibilityConverter` o a una propiedad `CanEditarEquipo`.
- Ejemplo: `Visibility="{Binding CanEditarEquipo, Converter={StaticResource BooleanToVisibilityConverter}}"`.

Ejemplo práctico: asignar Owner y cubrir overlay correctamente
-------------------------------------------------------------
- Implementar en el code-behind un método `ConfigurarParaVentanaPadre(Window parent)` que:

```csharp
public void ConfigurarParaVentanaPadre(System.Windows.Window? parentWindow)
{
    if (parentWindow == null) return;
    
    this.Owner = parentWindow;
    this.ShowInTaskbar = false;

    try
    {
        // Guardar referencia a la pantalla actual del owner
        var interopHelper = new System.Windows.Interop.WindowInteropHelper(parentWindow);
        var screen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);
        _lastScreenOwner = screen;

        // Para un overlay modal, siempre maximizar para cubrir toda la pantalla
        // Esto evita problemas de DPI, pantallas múltiples y posicionamiento
        this.WindowState = WindowState.Maximized;
    }
    catch
    {
        // Fallback: maximizar en pantalla principal
        this.WindowState = WindowState.Maximized;
    }
}
```

- Llamar antes de `ShowDialog()`: `dialog.ConfigurarParaVentanaPadre(owner);`
- **Importante**: No calcular Left/Top/Width/Height manualmente. Usar `WindowState.Maximized` es más simple, robusto y evita problemas con:
  - Conversiones DPI incorrectas
  - Desbordamientos a pantallas secundarias
  - Problemas de posicionamiento en pantallas múltiples
- Para detectar cambios de pantalla del owner, guardar referencia a `Screen` y comparar `DeviceName` en `LocationChanged` y `SizeChanged` events.

Sugerencias para rendimiento y virtualización
--------------------------------------------
- Si las secciones del modal contienen listas grandes (RAM, Discos, Periféricos, Conexiones), usar virtualización (`VirtualizingStackPanel`, `VirtualizingPanel.IsVirtualizing=True`, `VirtualizationMode=Recycling`) o cambiar a `ListBox`/`DataGrid` con virtualización activada. Esto evita el bloqueo visual al abrir el modal.
- Si se usan ItemsControl con DataTemplates complejos, probar con 100+ ítems para validar performance.

Política de sombras y contraste
------------------------------
- Mantener sombras muy suaves (opacidad < 0.15) para no afectar legibilidad.
- Botón de cierre `CloseButton` debe tener Foreground blanco cuando el header es color primario; si se cambia el header a fondos claros, usar `CloseButton` con `Foreground={StaticResource TextPrimaryBrush}` (o crear variación si se necesita).

Sincronización con cambios de pantalla (multi-monitor)
-----------------------------------------------------
- Implementar handlers en `Loaded()` para detectar cambios de Owner:

```csharp
private void PerifericoDetalleView_Loaded(object? sender, RoutedEventArgs e)
{
    if (this.Owner != null)
    {
        // Si el Owner se mueve/redimensiona, mantener sincronizado
        this.Owner.LocationChanged += Owner_SizeOrLocationChanged;
        this.Owner.SizeChanged += Owner_SizeOrLocationChanged;
    }
}

private void Owner_SizeOrLocationChanged(object? sender, System.EventArgs e)
{
    if (this.Owner == null) return;

    this.Dispatcher.Invoke(() =>
    {
        try
        {
            // Siempre maximizar para mantener el overlay cubriendo toda la pantalla
            this.WindowState = WindowState.Maximized;
            
            // Detectar si el Owner cambió de pantalla
            var interopHelper = new System.Windows.Interop.WindowInteropHelper(this.Owner);
            var currentScreen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);

            // Si cambió de pantalla, actualizar la referencia (para logs/diagnostics)
            if (_lastScreenOwner == null || !_lastScreenOwner.DeviceName.Equals(currentScreen.DeviceName))
            {
                _lastScreenOwner = currentScreen;
            }
        }
        catch
        {
            // En caso de error, asegurar que la ventana está maximizada
            this.WindowState = WindowState.Maximized;
        }
    });
}
```

- Beneficio: el overlay se mantiene perfectamente sincronizado si el usuario mueve la ventana entre pantallas o cambia su tamaño.
- No es necesario recalcular posiciones; `WindowState.Maximized` se ajusta automáticamente.

Comportamiento de edición y acciones destructivas
------------------------------------------------
- No colocar acciones destructivas (Dar de baja) en el header por motivos de UX. Deben estar en el flujo de edición o dentro de un menú contextual.
- En la vista Detalles implementada se ha movido el botón "Dar de baja" al flujo de edición: el editor (`AgregarEquipoInformaticoView`) debe exponer esa acción con confirmación y razonamiento (modal de confirmación `MessageBox` o dialog propio).

Pruebas y validación (QA)
-------------------------
- Probar las modales en 3 configuraciones: 100% DPI, 125% y 150% en monitores primarios y secundarios.
- Probar con Owner normal y Owner maximizado.
- Probar apertura de modal con listas pequeñas y listas grandes (>=100 ítems) para validar virtualización.
- Validar que overlay bloquea interacción en la ventana detrás (intentar click en controles del Owner).
- Validar accesibilidad: navegar por teclado, Tab order, Escape cierra la ventana, Enter activa botón por defecto si aplica.
- **IMPORTANTE**: Probar movimiento de Owner entre pantallas (si es multi-monitor) para validar que el overlay se mantiene sincronizado.

Checklist ampliado (para incluir antes de crear nueva modal)
-----------------------------------------------------------
- [ ] Usar `ModalWindowsStandard.xaml` y referenciar sus claves en `Window.Resources`.
- [ ] Incluir `Overlay_MouseLeftButtonDown` y `Panel_MouseLeftButtonDown` handlers.
- [ ] Registrar `KeyDown` o `PreviewKeyDown` para Escape.
- [ ] Configurar Owner y, si es necesario, llamar `ConfigurarParaVentanaPadre(owner)`.
- [ ] Usar `HeaderActionButtonStyle` para botones del header que requieran contraste o `PrimaryButtonStyle` en el cuerpo/footer.
- [ ] Evitar animaciones por defecto; si se añaden, documentarlas en el md.
- [ ] Documentar cualquier nueva clave de recurso que se agregue al ResourceDictionary.

Notas finales y mantenimiento
----------------------------
- Cada vez que se modifique `ModalWindowsStandard.xaml` debe actualizarse este documento con las nuevas claves (añadir en la sección "Recursos expuestos").
- Mantener este documento bajo revisión por UI/UX y QA para asegurar coherencia visual y accesibilidad.

Historial de cambios (versión inicial)
--------------------------------------
- 2025-10-10: Estándar creado a partir de las modificaciones realizadas en `RegistroEjecucionPlanDialog` y `PlanDetalleModalWindow`. Se unificó el overlay a `#80000000`, se eliminó animación de fade y se estandarizó comportamiento de owner/overlay mediante `ConfigurarParaVentanaPadre`.
- 2025-10-24: Actualización crítica - se reemplazó el cálculo manual de bounds con `WindowState.Maximized`. Esto resuelve problemas de DPI múltiple, pantallas múltiples y desbordamientos de overlay. Se removió toda conversión con `TransformFromDevice` que introducía errores de alineamiento.

Fin del documento.
