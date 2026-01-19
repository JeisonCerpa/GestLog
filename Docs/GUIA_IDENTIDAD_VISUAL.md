# 🎨 Guía de Identidad Visual - GestLog

> **Documento de referencia** para mantener coherencia visual en toda la aplicación

## 📍 Ubicación de Logos y Activos

Los logos y activos visuales se encuentran en la carpeta **`Assets/`**:

| Archivo | Dimensiones | Aspecto | Uso |
|---------|-------------|---------|-----|
| **logo.png** | 2209 x 571 px | 3.87:1 | Navbar, ventanas principales, headers |
| **Simics.png** | 2124 x 486 px | 4.37:1 | Branding corporativo, exportaciones |
| **PlantillaSIMICS.png** | 1700 x 2200 px | 0.77:1 | Plantilla para reportes y documentos |
| **firma.png** | 644 x 283 px | 2.28:1 | Firma digital, pie de documentos y PDFs |
| **image001.ico** | 225 x 190 px | 1.18:1 | Icono de aplicación, título de ventana |

---

## 🎨 Paleta de Colores Empresariales

### Colores Corporativos (Identidad Visual)

| Color | Hex | RGB | Uso |
|-------|-----|-----|-----|
| **Verde Principal** | `#118938` | rgb(17, 137, 56) | Botones principales, navbar, elementos de marca |
| **Verde Secundario** | `#37AB4E` | rgb(55, 171, 78) | Hover states, elementos complementarios |
| **Gris Claro** | `#9D9D9C` | rgb(157, 157, 156) | Bordes, divisores, elementos sutiles |
| **Gris Medio** | `#706F6F` | rgb(112, 111, 111) | Textos secundarios, labels |
| **Gris Oscuro** | `#504F4E` | rgb(80, 79, 78) | Títulos, textos principales |
| **Negro** | `#1D1D1B` | rgb(29, 29, 27) | Textos críticos, acentos oscuros |
| **Blanco** | `#FFFFFF` | rgb(255, 255, 255) | Fondos, cards, contenido |

### Colores de Apoyo Visual

| Color | Hex | Uso |
|-------|-----|-----|
| **Rojo** | `#C0392B` | Alertas, errores, estados críticos |
| **Off-White** | `#FAFAFA` | Fondo general, áreas de contenido |
| **Ámbar** | `#F59E0B` | Advertencias, estados pendientes |
| **Verde Éxito** | `#10B981` | Estados completados, confirmaciones |
| **Azul Información** | `#3B82F6` | Información, en progreso |

---

## 🎯 Aplicación en Componentes

### 1️⃣ Navbar
```
Fondo: Gradiente Verde #118938 → #37AB4E
Logo: Esquina inferior derecha
Efecto: DropShadowEffect
```

### 2️⃣ Botones
```
Primarios: Fondo #118938, Texto Blanco
Hover: Fondo #37AB4E, Sombra dinámica
CornerRadius: 8px
```

### 3️⃣ Cards
```
Fondo: #FFFFFF
Borde: #9D9D9C (gris claro)
CornerRadius: 8px
Sombra: DropShadowEffect moderada
```

### 4️⃣ SimpleProgressBar
```
Fondo: #FFFFFF
Barra: #118938 (verde principal)
Título: #504F4E (gris oscuro)
Porcentaje: #118938 (verde)
CornerRadius: Redondeados
```

### 5️⃣ Estados de Ejecución (Colores de Apoyo)
```
✅ Ejecutado: Verde #10B981
⏳ Pendiente: Ámbar #F59E0B
🔄 En Progreso: Azul #3B82F6
❌ Atrasado: Rojo #C0392B
```

---

## 🔍 Configuración WPF/XAML

### Importar Recursos
```xml
<!-- En ResourceDictionary o App.xaml -->
<!-- Colores Corporativos -->
<Color x:Key="VerdeEmpresarial">#118938</Color>
<Color x:Key="VerdeSecundario">#37AB4E</Color>
<Color x:Key="GrisClaro">#9D9D9C</Color>
<Color x:Key="GrisMedio">#706F6F</Color>
<Color x:Key="GrisOscuro">#504F4E</Color>
<Color x:Key="Negro">#1D1D1B</Color>
<Color x:Key="Blanco">#FFFFFF</Color>

<!-- Colores de Apoyo Visual -->
<Color x:Key="Rojo">#C0392B</Color>
<Color x:Key="OffWhite">#FAFAFA</Color>
<Color x:Key="Ambar">#F59E0B</Color>
<Color x:Key="VerdeExito">#10B981</Color>
<Color x:Key="AzulInfo">#3B82F6</Color>

<!-- Brushes Corporativos -->
<SolidColorBrush x:Key="VerdeEmpresarialBrush" Color="{StaticResource VerdeEmpresarial}"/>
<SolidColorBrush x:Key="VerdeSecundarioBrush" Color="{StaticResource VerdeSecundario}"/>
<SolidColorBrush x:Key="GrisClaro Brush" Color="{StaticResource GrisClaro}"/>
<SolidColorBrush x:Key="GrisMedioBrush" Color="{StaticResource GrisMedio}"/>
<SolidColorBrush x:Key="GrisOscuroBrush" Color="{StaticResource GrisOscuro}"/>
<SolidColorBrush x:Key="NegroBrush" Color="{StaticResource Negro}"/>
<SolidColorBrush x:Key="BlancoBrush" Color="{StaticResource Blanco}"/>
```

### Botón con Estilos
```xml
<Button 
    Background="{StaticResource VerdeEmpresarialBrush}"
    Foreground="White"
    CornerRadius="8"
    Padding="16,8"
    FontWeight="SemiBold">
    <Button.Effect>
        <DropShadowEffect Color="#000000" Opacity="0.2" BlurRadius="4"/>
    </Button.Effect>
</Button>
```

---

## ✨ Reglas de Consistencia

✅ **Hacer:**
- Usar verde `#118938` para elementos primarios de marca
- Usar verde `#37AB4E` para hover states y elementos complementarios
- Usar escala de grises empresariales (#9D9D9C a #504F4E) para jerarquía visual
- Usar colores de apoyo visual solo para estados específicos (rojo para errores, ámbar para advertencias, etc.)
- Aplicar `CornerRadius="8"` en cards y botones
- Mantener sombras sutiles pero consistentes
- Usar espaciado regular y proporcional

❌ **Evitar:**
- Usar colores que no estén en la paleta corporativa
- Mezclar verdes corporativos con otros tonos de verde
- Cambiar radios de esquinas sin propósito
- Sombras excesivas o inconsistentes
- Usar colores de apoyo para elementos que no requieren énfasis visual

---

## 📞 Documentos Relacionados

- **copilot-instructions.md** - Instrucciones de desarrollo completas
- **Modern-UI-Style-Library.md** - Estilos modernos (referencia)
- **Recursos en Assets/** - Logos y activos visuales

---

*Última actualización: 19 de enero de 2026*
*GestLog © - Todos los derechos reservados*
