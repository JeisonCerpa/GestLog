# Plantilla visual: Vistas con DataGrid (reutilizable)

Plantilla reutilizable para vistas tipo listado con DataGrid en GestLog. Basada en `PerifericosView.xaml` y `EquiposInformaticosView.xaml`, esta guía sirve como referencia y plantilla para crear nuevas vistas que sigan la misma estética y comportamiento.

## Resumen rápido
- Paleta principal: Primary #118938, Secondary #2B8E3F, TextPrimary #504F4E, TextSecondary #706F6F, Surface #FFFFFF, Border #E2E8F0, Error #C0392B.
- Estructura: Grid raíz con 3 filas — Header (Auto), Estadísticas (Auto), Contenido (Star).
- Objetivo: apariencia coherente entre vistas: mismo header, barra de estadísticas, card con DataGrid y controles de acción.

---

## Header
- Contenedor: `Border` con fondo en gradiente (LinearGradientBrush de Primary → Secondary).
- Padding: `24,16`.
- Margin: `16,16,16,0`.
- CornerRadius: `8,8,0,0`.
- Icono: caja blanca con CornerRadius `8`, Width/Height `56`, icono emoji con FontSize `26` (FontFamily `Segoe UI Emoji`).
- Tipografías:
  - Título (`HeaderTextStyle`): Segoe UI, SemiBold, FontSize = 24, Foreground = White.
  - Subtítulo (`SubHeaderTextStyle`): Segoe UI, FontSize = 14, Foreground = White, Opacity = 0.9.

## Barra de estadísticas
- Contenedor: `Border` con Background = `SurfaceBrush` y BorderBrush = `BorderBrush`.
- Padding: `20`. Margin horizontal: `16,0`.
- BorderThickness: `1,0,1,1`.
- Layout interno: Grid con columnas iguales para cada contador.
- Tipografías:
  - Número (`StatsTextStyle`): Segoe UI Bold 20pt, centrado.
  - Etiqueta (`StatsLabelStyle`): Segoe UI 14pt, Medium, Foreground = TextSecondary (#706F6F).
- Uso: adaptar los contadores a la entidad de la vista (por ejemplo: activos, en mantenimiento, en reparación, inactivos, dados de baja).
- Acciones rápidas al extremo derecho: botones circulares (Actualizar / Exportar / Importar / Agregar).

## Botones (estilos comunes)
- `PrimaryButtonStyle` (base): Background = PrimaryBrush (#118938), Foreground = White, FontSize = 14, Padding = 16,8, BorderThickness = 0, CornerRadius = 6.
- `CircularButtonStyle` (basado en Primary): Width/Height = 56, FontSize = 26, BorderBrush = #E5E5E5, BorderThickness = 2, CornerRadius = 28.
- `SecondaryButtonStyle` (gris para acciones de fila): Background = TextSecondaryBrush (#706F6F), basado en Primary.
- Botones de fila: tamaño recomendado Width=88, Height=32.

## Filtros y toggle "Dados de baja" (o equivalente)
- Filtro principal: TextBox Width=300, Padding=8, BorderBrush = `{StaticResource BorderBrush}` y botón de acción junto al filtro (`PrimaryButtonStyle`).
- Toggle para mostrar elementos dados de baja/desactivados/inactivos: ToggleButton estilizado tipo switch, Width=110, Height=32, knob 24x24, vinculado a propiedad booleana del ViewModel.

## DataGrid (plantilla)
- Propiedades globales recomendadas:
  - AutoGenerateColumns = False
  - CanUserAddRows = False
  - CanUserDeleteRows = False
  - SelectionMode = Single
  - RowHeaderWidth = 0
  - GridLinesVisibility = Horizontal
  - HorizontalGridLinesBrush = {StaticResource BorderBrush} (#E2E8F0)
  - AlternatingRowBackground = #F9F9F9
  - FontSize = 13
  - RowHeight = 40
  - **RowStyle = {StaticResource DataGridRowEstadoStyle}** (para opacidad condicional)
- Columnas sugeridas (ajustar a la entidad):
  - Código: Width = 1* (con **ElementStyle = {StaticResource DataGridTextBlockStyle}**)
  - Nombre/Descripción: Width = 2* (con **ElementStyle**)
  - Marca/Tipo: Width = 1* (con **ElementStyle**)
  - Asignación/Usuario: Width = 2* (con **ElementStyle**)
  - Ubicación/Sede: Width = 2* (con **ElementStyle**)
  - Estado: Width = 1* (usar converter para color + texto, también con **ElementStyle**)
  - Acciones: Width = 100 (botón "Detalles" con `SecondaryButtonStyle`)
- Estado visual: mostrar un `Ellipse` (14x14) con color según estado y texto a la derecha con margen `8,0,0,0`.

### Estilos para elementos dados de baja/inactivos
**IMPORTANTE**: Usar converters en lugar de múltiples DataTriggers para mayor robustez.

#### 1. Registrar converters en Resources:
```xml
<UserControl.Resources>
    <conv:EstadoToBoolConverter x:Key="EstadoToDadoDeBajaConverter" TargetEstado="DadoDeBaja"/>
    <conv:EstadoToOpacityConverter x:Key="EstadoToOpacityConverter"/>
    <conv:EstadoToColorConverter x:Key="EstadoToColorConverter"/>
</UserControl.Resources>
```

#### 2. Estilo para TextBlock (tachado en dados de baja):
```xml
<Style x:Key="DataGridTextBlockStyle" TargetType="TextBlock">
    <Setter Property="VerticalAlignment" Value="Center"/>
    <Style.Triggers>
        <DataTrigger Binding="{Binding Estado, Converter={StaticResource EstadoToDadoDeBajaConverter}}" Value="True">
            <Setter Property="TextDecorations" Value="Strikethrough"/>
        </DataTrigger>
    </Style.Triggers>
</Style>
```

#### 3. Estilo para DataGridRow (opacidad condicional):
```xml
<Style x:Key="DataGridRowEstadoStyle" TargetType="DataGridRow">
    <Setter Property="Opacity" Value="{Binding Estado, Converter={StaticResource EstadoToOpacityConverter}}"/>
</Style>
```

**Valores de opacidad automáticos**:
- "Dado de baja" → 0.5
- "Inactivo" → 0.75
- Otros estados → 1.0

#### 4. Aplicar estilos al DataGrid:
```xml
<DataGrid RowStyle="{StaticResource DataGridRowEstadoStyle}">
    <DataGrid.Columns>
        <DataGridTextColumn Header="Código" Binding="{Binding Codigo}" 
                            Width="1*" ElementStyle="{StaticResource DataGridTextBlockStyle}"/>
        <DataGridTextColumn Header="Nombre" Binding="{Binding Nombre}" 
                            Width="2*" ElementStyle="{StaticResource DataGridTextBlockStyle}"/>
        <!-- Aplicar ElementStyle a todas las columnas de texto -->
    </DataGrid.Columns>
</DataGrid>
```

### Ordenamiento por Estado (mejor práctica)
Para ordenar de forma consistente por el estado mostrado, lo mejor es exponer una propiedad calculada en la entidad o ViewModel que represente el orden lógico del estado (p. ej. `EstadoOrden` int). Luego indique `SortMemberPath` en la columna de Estado para que el DataGrid ordene por esa propiedad numérica en lugar del texto visual.

Ejemplo mínimo (entidad):
```csharp
[NotMapped]
public int EstadoOrden
{
    get
    {
        var s = (Estado ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", "");
        return s switch
        {
            "activo" => 0,
            "enmantenimiento" => 1,
            "enreparacion" => 2,
            "inactivo" => 3,
            "dadodebaja" => 4,
            _ => 5,
        };
    }
}
```
Y en XAML (columna Estado):
```xml
<DataGridTemplateColumn Header="Estado" SortMemberPath="EstadoOrden" ...>
    <!-- template -->
</DataGridTemplateColumn>
```

### Mapeo de estados entre dominio y presentación (recomendado)
La presentación (colores, opacidad, tachado y orden) debe basarse en un conjunto reducido de categorías de negocio (por ejemplo: Activo, En Mantenimiento, En Reparación, Inactivo, DadoDeBaja). Cada módulo/entidad puede tener nombres distintos para sus estados (p.ej. `EnUso`, `AlmacenadoFuncionando`, `DadoDeBaja` en Periféricos). Recomendación: mapear los estados del dominio a las categorías de presentación mediante una función centralizada (mapper o converter) y exponer propiedades calculadas en la entidad o ViewModel.

Beneficios:
- Consistencia visual entre vistas distintas
- Facilidad para ordenar y filtrar por la misma lógica
- Mantenimiento centralizado (añadir variantes en un único lugar)

#### Ejemplo 1 — Entidad basada en enum (Periféricos)
```csharp
// En PerifericoEquipoInformaticoEntity (enum EstadoPeriferico)
[NotMapped]
public int EstadoOrden => Estado switch
{
    EstadoPeriferico.EnUso => 0,                 // Activo (verde)
    EstadoPeriferico.AlmacenadoFuncionando => 1, // Almacenado (ámbar)
    EstadoPeriferico.EnReparacion => 2,          // En reparación
    EstadoPeriferico.Inactivo => 3,              // Inactivo
    EstadoPeriferico.DadoDeBaja => 4,            // Dado de baja
    _ => 5,
};

[NotMapped]
public string EstadoNormalized => Estado switch
{
    EstadoPeriferico.EnUso => "Activo",
    EstadoPeriferico.AlmacenadoFuncionando => "En Mantenimiento",
    EstadoPeriferico.EnReparacion => "En Reparación",
    EstadoPeriferico.Inactivo => "Inactivo",
    EstadoPeriferico.DadoDeBaja => "Dado de baja",
    _ => Estado.ToString(),
};
```

#### Ejemplo 2 — Entidad basada en strings (EquiposInformaticos)
```csharp
// En EquipoInformaticoEntity (Estado string)
[NotMapped]
public int EstadoOrden
{
    get
    {
        var s = (Estado ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", "");
        return s switch
        {
            "activo" or "enuso" => 0,
            "enmantenimiento" or "enmantenimiento" => 1,
            "enreparacion" or "enreparación" => 2,
            "inactivo" => 3,
            "dadodebaja" or "dado" => 4,
            _ => 5,
        };
    }
}

[NotMapped]
public string EstadoNormalized
{
    get
    {
        var s = (Estado ?? string.Empty).Trim().ToLowerInvariant();
        return s.Contains("dado") ? "Dado de baja"
             : s.Contains("inactivo") ? "Inactivo"
             : s.Contains("mantenimiento") ? "En Mantenimiento"
             : s.Contains("reparacion") ? "En Reparación"
             : s.Contains("uso") ? "Activo"
             : Estado ?? string.Empty;
    }
}
```

#### Centralizar mapeos (mejor práctica)
Crear una clase helper o converter reutilizable que haga el mapeo (string/enum → presentación). Ejemplo simplificado:
```csharp
public static class EstadoPresentationMapper
{
    public static int ToOrden(string? estado) { /* normalizar y mapear */ }
    public static string ToNormalized(string? estado) { /* normalizar y mapear */ }
    public static int ToOrden(Enum estadoEnum) { /* mapear por tipo de enum */ }
}
```
Usar este mapper desde la entidad, ViewModel o converters (EstadoToColorConverter puede delegar en él).

#### XAML: usar las propiedades normalizadas
- `SortMemberPath="EstadoOrden"` para orden lógico.
- Bind del texto puede usar `EstadoNormalized` o `Estado` (convertido) para consistencia.
- El `Ellipse` puede seguir vinculándose a `Estado` si `EstadoToColorConverter` centraliza el mapeo.

---

### Converters esenciales
Los siguientes converters deben estar disponibles en el proyecto y registrados en los recursos de la vista:

1. **EstadoToColorConverter** (`Converters/EstadoToColorConverter.cs`)
   - Convierte estado → color del indicador visual (Ellipse)
   - Normaliza automáticamente strings (trim, lowercase)
   - Mapeo: Activo=#2B8E3F, En Mantenimiento=#F9B233, En Reparación=#A85B00, Dado de baja=#EDEDED, Inactivo=#9E9E9E

2. **EstadoToBoolConverter** (`Converters/EstadoToBoolConverter.cs`)
   - Convierte estado → bool (para detectar estados específicos)
   - Configurable con propiedad `TargetEstado`
   - Uso: aplicar TextDecorations (strikethrough) a equipos dados de baja

3. **EstadoToOpacityConverter** (`Converters/EstadoToOpacityConverter.cs`)
   - Convierte estado → valor de opacidad (0.5, 0.75, 1.0)
   - Lógica: Dado de baja=0.5, Inactivo=0.75, Otros=1.0
   - Uso: aplicar transparencia a filas del DataGrid según estado

4. **BooleanToOpacityConverter** (`Converters/BooleanToOpacityConverter.cs`)
   - Convierte bool → opacidad (1.0 o 0.5)
   - Uso: deshabilitar visualmente botones sin permisos

### Estructura del ViewModel
El ViewModel debe seguir esta estructura básica para máxima coherencia:

```csharp
public partial class XViewModel : DatabaseAwareViewModel
{
    private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
    private readonly ICurrentUserService _currentUserService;
    
    // Colección principal
    public ObservableCollection<XEntity> ListaX { get; set; } = new();
    
    // Vista filtrable
    [ObservableProperty]
    private ICollectionView? xView;
    
    // Propiedades de filtro
    [ObservableProperty]
    private string filtroX = string.Empty;
    
    [ObservableProperty]
    private bool showDadoDeBaja = false;
    
    // Contadores para estadísticas (recalculados automáticamente)
    [ObservableProperty]
    private int xActivos;
    
    [ObservableProperty]
    private int xEnMantenimiento;
    
    [ObservableProperty]
    private int xInactivos;
    
    [ObservableProperty]
    private int xDadosBaja;
    
    // Permisos
    [ObservableProperty]
    private bool canCrearX;
    
    [ObservableProperty]
    private bool canEditarX;
    
    [ObservableProperty]
    private bool canExportarDatos;
    
    // Constructor
    public XViewModel(...)
    {
        // Inicializar CollectionView con filtro
        XView = CollectionViewSource.GetDefaultView(ListaX);
        if (XView != null)
            XView.Filter = new Predicate<object>(FiltrarX);
        
        // Suscribir a cambios para recalcular estadísticas
        ListaX.CollectionChanged += (s, e) => RecalcularEstadisticas();
    }
    
    // Filtro dinámico
    private bool FiltrarX(object obj)
    {
        if (obj is not XEntity x) return false;
        
        // Ocultar dados de baja si toggle desactivado
        if (!ShowDadoDeBaja && EsDadoDeBaja(x.Estado))
            return false;
        
        // Aplicar filtro de búsqueda
        if (string.IsNullOrWhiteSpace(FiltroX)) return true;
        
        // Implementar lógica de búsqueda...
        return true;
    }
    
    // Recalcular estadísticas
    private void RecalcularEstadisticas()
    {
        XActivos = ListaX.Count(x => EsEstado(x.Estado, "Activo"));
        XEnMantenimiento = ListaX.Count(x => EsEstado(x.Estado, "En Mantenimiento"));
        XInactivos = ListaX.Count(x => EsEstado(x.Estado, "Inactivo"));
        XDadosBaja = ListaX.Count(x => EsDadoDeBaja(x.Estado));
    }
    
    // Helper: comparación normalizada de estado
    private bool EsEstado(string? estado, string target)
    {
        return estado?.Trim().Equals(target, StringComparison.OrdinalIgnoreCase) ?? false;
    }
    
    private bool EsDadoDeBaja(string? estado)
    {
        var normalizado = estado?.Trim().ToLowerInvariant().Replace(" ", "") ?? "";
        return normalizado == "dadodebaja";
    }
    
    // Refrescar vista cuando cambian filtros
    partial void OnFiltroXChanged(string value)
    {
        Application.Current?.Dispatcher.Invoke(() => XView?.Refresh());
    }
    
    partial void OnShowDadoDeBajaChanged(bool value)
    {
        Application.Current?.Dispatcher.Invoke(() => XView?.Refresh());
    }
}
```

**Puntos clave del ViewModel**:
- Heredar de `DatabaseAwareViewModel` para auto-refresh
- Usar `ObservableCollection` para la colección principal
- Usar `ICollectionView` para filtrado dinámico
- Recalcular estadísticas en `CollectionChanged`
- Refrescar vista cuando cambien criterios de filtro
- Usar `AsNoTracking()` al cargar datos para evitar tracking de EF Core

## Estilos, espaciado y sombras
- Cards: Padding = 20; CornerRadius contenido = `0,0,8,8`.
- BorderThickness = `1,0,1,1` para integrar header con contenido.
- Sombra: DropShadowEffect BlurRadius=8, ShadowDepth=2, Opacity=0.18 para el `CardBorderStyle` cuando aplique.

## Accesibilidad y UX
- ToolTips en iconos y botones.
- Opacidad condicional y `IsEnabled` para botones según permisos (BooleanToOpacityConverter).
- Cursor = Hand en botones interactivos y feedback visual en hover/pressed.

## Buenas prácticas
- Extraer brushes/styles a `ResourceDictionary` compartido para máxima reutilización.
- **Usar converters en lugar de múltiples DataTriggers** para comparaciones de estado (ver sección DataGrid).
- Mantener proporción de columnas (1*,2*, etc.) para coherencia entre vistas.
- Aplicar `ElementStyle={StaticResource DataGridTextBlockStyle}` a todas las columnas de texto del DataGrid.
- Aplicar `RowStyle={StaticResource DataGridRowEstadoStyle}` al DataGrid para opacidad condicional.
- Normalizar valores de estado: preferir enums o normalización consistente en BD; si se usan strings, los converters manejan variaciones automáticamente.
- Recalcular estadísticas automáticamente suscribiéndose a `ObservableCollection.CollectionChanged`.
- Usar `AsNoTracking()` en queries EF Core para evitar datos stale en listas.
- Implementar filtros con `ICollectionView.Filter` y refrescar con `Refresh()` cuando cambien criterios.
- Reutilizar esta plantilla como base al crear nuevas vistas tipo DataGrid.

## Referencias relacionadas
- `Docs/Fix-Estilos-Estado-Equipos.md` - Documentación detallada del sistema de estilos por estado
- `Docs/Modern-UI-Style-Library.md` - Paleta de colores y estilos del proyecto
- `Views/Tools/GestionEquipos/EquiposInformaticosView.xaml` - Implementación de referencia
- `Views/Tools/GestionEquipos/PerifericosView.xaml` - Implementación de referencia

## Checklist para nueva vista con DataGrid
- [ ] Header con gradiente Primary→Secondary, icono y tipografías correctas
- [ ] Barra de estadísticas con contadores apropiados a la entidad
- [ ] Botones circulares de acción (Actualizar, Exportar, Importar, Agregar)
- [ ] Filtro de búsqueda con TextBox Width=300
- [ ] Toggle para mostrar/ocultar elementos dados de baja o inactivos
- [ ] DataGrid con propiedades: FontSize=13, RowHeight=40, GridLinesVisibility=Horizontal
- [ ] Registrar converters en Resources: `EstadoToColorConverter`, `EstadoToBoolConverter`, `EstadoToOpacityConverter`
- [ ] Crear estilos: `DataGridTextBlockStyle`, `DataGridRowEstadoStyle`, `PrimaryButtonStyle`, `SecondaryButtonStyle`, `CircularButtonStyle`
- [ ] Aplicar `ElementStyle` a todas las columnas de texto
- [ ] Aplicar `RowStyle` al DataGrid
- [ ] Columna de Estado con Ellipse + texto usando `EstadoToColorConverter`
- [ ] Columna de Acciones con botón "Detalles" Width=100
- [ ] ViewModel: propiedades observables para contadores, filtro, toggle
- [ ] ViewModel: implementar `RecalcularEstadisticas()` y suscribir a `CollectionChanged`
- [ ] ViewModel: implementar `ICollectionView.Filter` para filtrado dinámico
- [ ] Comandos: `CargarCommand`, `AgregarCommand`, `VerDetallesCommand`, `ExportarCommand`, etc.
- [ ] Permisos: vincular `IsEnabled` y `Opacity` de botones a propiedades `Can...` del ViewModel

---

Archivo: `Docs/DataGridView-Template.md` — plantilla reutilizable para vistas con DataGrid en GestLog.
