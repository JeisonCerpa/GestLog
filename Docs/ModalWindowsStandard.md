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
  - Si parent es null -> maximizar Window (WindowState.Maximized) para cubrir pantalla.
  - Si parent.WindowState == Maximized -> poner el diálogo maximizado.
  - En otro caso, usar WindowInteropHelper(parent).Handle para obtener la pantalla (System.Windows.Forms.Screen.FromHandle) y fijar Left/Top/Width/Height del diálogo al bounds de la pantalla del owner. Esto garantiza que el overlay cubra la ventana en todas las resoluciones.

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

Notas finales
-------------
- Se recomienda centralizar colores y estilos en un ResourceDictionary compartido para evitar inconsistencias (por ejemplo `Resources/ModalStyles.xaml`).
- Si se necesita animación, documentarla y aplicar la misma duración y easing en todas las modales.

Historial de cambios (versión inicial)
--------------------------------------
- 2025-10-10: Estándar creado a partir de las modificaciones realizadas en `RegistroEjecucionPlanDialog` y `PlanDetalleModalWindow`. Se unificó el overlay a `#80000000`, se eliminó animación de fade y se estandarizó comportamiento de owner/overlay mediante `ConfigurarParaVentanaPadre`.

Fin del documento.
