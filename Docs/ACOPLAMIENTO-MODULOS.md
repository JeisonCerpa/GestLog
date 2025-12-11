# ⚠️ Acoplamiento entre Módulos - Refactorización Pendiente

**Última actualización:** 11 de diciembre de 2025

## 🎯 Objetivo
Documentar el acoplamiento innecesario identificado entre módulos de GestLog para su refactorización futura y desacoplamiento completo.

---

## 📊 Estado Actual

### **Compilación y Funcionalidad**
- ✅ **0 errores de compilación**
- ✅ **Aplicación funcional**
- ⚠️ **Acoplamiento estructural presente** (no impide funcionamiento, pero viola SRP)

---

## 🔴 Acoplamiento Identificado

### **1. GestionEquiposInformaticos → GestionMantenimientos**

#### **Problema:**
ViewModels de `GestionEquiposInformaticos` tienen usings de `GestionMantenimientos.Interfaces.Data` cuando no deberían depender directamente de otro módulo.

#### **Archivos afectados:**
```
Modules/GestionEquiposInformaticos/ViewModels/
├── CronogramaDiarioViewModel.cs
├── HistorialEjecucionesViewModel.cs
└── PerifericosViewModel.cs
```

#### **Usings problemáticos:**
```csharp
using GestLog.Modules.GestionMantenimientos.Interfaces.Data;
using GestLog.Modules.GestionMantenimientos.Messages.Mantenimientos;
using GestLog.Modules.GestionMantenimientos.Messages.Equipos;
```

#### **Impacto:**
- 🔗 **Acoplamiento fuerte** entre módulos
- 📦 **No reutilizable** - GestionEquiposInformaticos depende de GestionMantenimientos
- 🔄 **Cambios en cascada** - Modificar GestionMantenimientos afecta GestionEquiposInformaticos
- ❌ **Viola SRP** - Los módulos deben ser independientes

---

### **2. GestionEquipos → GestionMantenimientos.Messages**

#### **Problema:**
Archivos en `GestionEquipos` (nivel superior) usan mensajes de `GestionMantenimientos` directamente.

#### **Archivos afectados:**
```
ViewModels/Tools/GestionEquipos/
├── AgregarEquipoInformaticoViewModel.cs
├── DetallesEquipoInformaticoViewModel.cs

Views/Tools/GestionEquipos/
└── PerifericoDialog.xaml.cs

Services/Equipos/
└── EquipoEstadoService.cs
```

#### **Usings problemáticos:**
```csharp
using GestLog.Modules.GestionMantenimientos.Messages.Equipos;
using GestLog.Modules.GestionMantenimientos.Messages.Mantenimientos;
```

#### **Impacto:**
- 🔗 **Acoplamiento débil → fuerte** con el tiempo
- 📦 **Módulos a nivel superior dependen de módulos internos**
- 🔄 **Caminos de acceso confusos** - No está claro quién depende de quién

---

### **3. Referencias Cruzadas Generales**

#### **Patrón problemático:**
```
Nivel Superior (Views/ViewModels/Services)
    ↓ (depende de)
Modules/GestionMantenimientos
    ↓ (depende de)
Modules/GestionEquiposInformaticos (circular)
```

**Esto crea un grafo de dependencias complejo y difícil de mantener.**

---

## 💡 Soluciones Propuestas

### **Opción A: Eventos/Mensajería Global (Recomendado)**

**Concepto:** Desacoplar módulos usando un bus de mensajes centralizado.

```
Módulo A  →  [Bus de Mensajes]  ←  Módulo B
                    ↑
                (neutro)
```

**Implementación:**
```csharp
// En el bus global (nivel raíz, no en módulos)
public class GlobalMessaging
{
    // Mensajes que cualquier módulo puede enviar/escuchar
    public class EquipoActualizadoMessage { }
    public class MantenimientoRegistradoMessage { }
    public class EstadoEquipoChangedMessage { }
}

// En GestionEquipos
WeakReferenceMessenger.Default.Send(new EquipoActualizadoMessage());

// En GestionMantenimientos (sin referencia a GestionEquipos)
WeakReferenceMessenger.Default.Register<EquipoActualizadoMessage>(this, (r, m) => {
    RefreshMaintenance();
});
```

**Ventajas:**
- ✅ Módulos completamente desacoplados
- ✅ Fácil de agregar nuevos módulos
- ✅ Bus centralizado y testeable

---

### **Opción B: Interfaces Compartidas (Alternativa)**

**Concepto:** Crear interfaces en una carpeta `SharedInterfaces/` neutral que ambos módulos implementan.

```
SharedInterfaces/
├── IEquipoService.cs
├── IMantenimientoService.cs
└── IEquipoStateChangeNotifier.cs

Modules/GestionMantenimientos/
├── Services/
│   └── MantenimientoService.cs (implementa IMantenimientoService)

Modules/GestionEquiposInformaticos/
├── Services/
│   └── EquipoSyncService.cs (usa IMantenimientoService)
```

**Ventajas:**
- ✅ Dependencias hacia interfaces, no implementaciones
- ✅ Más fácil que reescribir todo
- ⚠️ Requiere crear carpeta compartida a nivel raíz

---

### **Opción C: Adapters/Facades (Más seguro)**

**Concepto:** Crear adapters en módulos que consumen servicios de otros módulos.

```
Modules/GestionEquiposInformaticos/
├── Adapters/
│   └── MantenimientoServiceAdapter.cs  ← Encapsula uso de GestionMantenimientos

Modules/GestionMantenimientos/
├── Interfaces/
│   └── IMantenimientoService.cs
```

```csharp
// En el Adapter
public class MantenimientoServiceAdapter : IMaintenanceNotifier
{
    private readonly IMantenimientoService _maintenanceService;
    
    public async Task NotifyEquipoActualizado(int equipoId)
    {
        // Encapsula el uso del servicio externo
        await _maintenanceService.SyncEquipoAsync(equipoId);
    }
}
```

**Ventajas:**
- ✅ Cambios futuros en GestionMantenimientos aislados
- ✅ Menos invasivo que Opción B
- ⚠️ Requiere mantener adapters

---

## 🗂️ Estructura Propuesta - Opción A (Recomendada)

```
Modules/
├── GestionMantenimientos/
│   ├── Interfaces/Data/
│   ├── Services/Data/
│   ├── ViewModels/
│   └── Messages/ ← Solo INTERNOS del módulo
│
├── GestionEquiposInformaticos/
│   ├── Interfaces/Data/
│   ├── Services/Data/
│   ├── ViewModels/
│   └── (NO importa GestionMantenimientos)
│
└── SharedMessaging/ ← ✨ NUEVO
    └── GlobalMessages.cs
        ├── EquipoActualizadoMessage
        ├── MantenimientoRegistradoMessage
        ├── EstadoEquipoChangedMessage
        └── ...
```

**Resultado:**
- ✅ Módulos completamente independientes
- ✅ Comunicación vía bus de mensajes global
- ✅ Fácil de extender y testear

---

## 📋 Plan de Refactorización

### **Fase 1: Auditoría Completa** (PENDIENTE)
- [ ] Mapear TODAS las dependencias inter-módulos
- [ ] Crear grafo de dependencias
- [ ] Identificar ciclos o referencias circulares
- [ ] Documentar en este archivo

### **Fase 2: Decisión de Estrategia** (PENDIENTE)
- [ ] Evaluar Opciones A, B, C con el equipo
- [ ] Elegir la más adecuada para la arquitectura actual
- [ ] Documentar decisión y justificación

### **Fase 3: Implementación** (PENDIENTE)
- [ ] Crear `SharedMessaging/` o `SharedInterfaces/` según estrategia
- [ ] Refactorizar GestionEquiposInformaticos
- [ ] Refactorizar GestionEquipos
- [ ] Actualizar usings y namespaces
- [ ] Validar compilación: 0 errores

### **Fase 4: Validación** (PENDIENTE)
- [ ] Ejecutar aplicación completa
- [ ] Pruebas funcionales de flujos afectados
- [ ] Documentar cambios en copilot-instructions.md
- [ ] Actualizar diagramas de arquitectura

---

## 📊 Matriz de Dependencias Actual

| Módulo/Carpeta | Depende de | Tipo | Criticidad |
|---|---|---|---|
| GestionEquiposInformaticos.CronogramaDiarioViewModel | GestionMantenimientos.Interfaces.Data | Acoplamiento fuerte | 🔴 Alta |
| GestionEquiposInformaticos.HistorialEjecucionesViewModel | GestionMantenimientos.Messages | Acoplamiento medio | 🟡 Media |
| GestionEquiposInformaticos.PerifericosViewModel | GestionMantenimientos.Messages | Acoplamiento medio | 🟡 Media |
| GestionEquipos.AgregarEquipoViewModel | GestionMantenimientos.Messages | Acoplamiento débil | 🟡 Baja-Media |
| GestionEquipos.DetallesEquipoViewModel | GestionMantenimientos.Messages | Acoplamiento débil | 🟡 Baja-Media |
| GestionEquipos.EquipoEstadoService | GestionMantenimientos.Messages | Acoplamiento medio | 🟡 Media |

---

## 🔬 TAREA: Auditoría Completa de Acoplamientos

### **Estado: ⏳ POR INVESTIGAR**

Se necesita ejecutar una auditoría exhaustiva para identificar **TODOS los acoplamientos cruzados** entre módulos del proyecto. Esta información es crítica antes de iniciar la refactorización.

### **Qué investigar:**

#### **1. Todas las referencias entre Módulos**
- [ ] Buscar todos los `using GestLog.Modules.*` desde archivos fuera de ese módulo
- [ ] Documentar cada referencia encontrada
- [ ] Clasificar por criticidad (alta, media, baja)

#### **2. Módulos conocidos con acoplamiento**
- [ ] **GestionEquiposInformaticos** → ¿Qué más importa de otros módulos?
- [ ] **GestionEquipos** → ¿Qué dependencias tiene?
- [ ] **Otros módulos** → Revisar si tienen interdependencias

#### **3. Servicios compartidos**
- [ ] Identificar servicios que se usan en múltiples módulos
- [ ] Detectar si hay duplicación de lógica
- [ ] Mapear dependencias de interfaces

#### **4. Mensajes cruzados**
- [ ] ¿Hay módulos que usan mensajes de otros módulos?
- [ ] ¿Existen ciclos de mensajería?

#### **5. Utilities y Helpers**
- [ ] ¿Hay clases compartidas o utilitarias acopladas?
- [ ] ¿Se reutilizan en múltiples módulos?

### **Resultados de la Auditoría** (PENDIENTE LLENAR)

```
📁 ACOPLAMIENTOS ENCONTRADOS:
├── 🔴 CRITICIDAD ALTA (Refactorizar primero)
│   └── [Agregar aquí]
│
├── 🟡 CRITICIDAD MEDIA (Refactorizar después)
│   └── [Agregar aquí]
│
└── 🟢 CRITICIDAD BAJA (Refactorizar al final)
    └── [Agregar aquí]
```

### **Comandos PowerShell para la Auditoría**

Ejecuta estos comandos en PowerShell para obtener resultados:

#### **1. Buscar todos los usings entre Módulos**
```powershell
# Búsqueda global de usings de módulos fuera de ese módulo
$modules = @("GestionMantenimientos", "GestionEquiposInformaticos", "DaaterProccesor")
foreach ($module in $modules) {
    Write-Host "`n=== Referencias hacia $module ===" -ForegroundColor Yellow
    Get-ChildItem -Path "e:\Softwares\GestLog" -Filter "*.cs" -Recurse |
      Select-String "using GestLog.Modules.$module" |
      Where-Object { $_.Path -notmatch "\\$module\\" } |
      ForEach-Object { 
        $file = $_.Path -replace 'e:\\Softwares\\GestLog\\', ''
        Write-Host "$file : $($_.Line.Trim())" -ForegroundColor Cyan
      }
}
```

#### **2. Acoplamientos por archivo específico**
```powershell
# Mostrar todos los usings de un archivo para ver sus dependencias
$file = "e:\Softwares\GestLog\Modules\GestionEquiposInformaticos\ViewModels\CronogramaDiarioViewModel.cs"
Get-Content $file | Select-String "^using" | 
  Where-Object { $_ -match "GestLog.Modules" } |
  ForEach-Object { Write-Host $_.Line -ForegroundColor Green }
```

#### **3. Matriz de acoplamientos (resumen)**
```powershell
# Generar reporte de cuántas referencias tiene cada módulo
$path = "e:\Softwares\GestLog"
$modules = @("GestionMantenimientos", "GestionEquiposInformaticos", "DaaterProccesor")

foreach ($module in $modules) {
    $refs = Get-ChildItem -Path $path -Filter "*.cs" -Recurse |
      Select-String "using GestLog.Modules.$module" |
      Where-Object { $_.Path -notmatch "\\$module\\" } |
      Measure-Object
    
    Write-Host "📦 $module : $($refs.Count) referencias externas" -ForegroundColor Magenta
}
```

#### **4. Detectar ciclos (A→B→A)**
```powershell
# Si GestionMantenimientos usa algo de GestionEquiposInformaticos Y vice versa
$gestionMant = Get-ChildItem -Path "e:\Softwares\GestLog\Modules\GestionMantenimientos" -Filter "*.cs" -Recurse |
  Select-String "using GestLog.Modules.GestionEquiposInformaticos" | Measure-Object

$gestionEquip = Get-ChildItem -Path "e:\Softwares\GestLog\Modules\GestionEquiposInformaticos" -Filter "*.cs" -Recurse |
  Select-String "using GestLog.Modules.GestionMantenimientos" | Measure-Object

if ($gestionMant.Count -gt 0 -AND $gestionEquip.Count -gt 0) {
    Write-Host "⚠️ CICLO DETECTADO: GestionMantenimientos ↔ GestionEquiposInformaticos" -ForegroundColor Red
}
```

### **Formato para documentar hallazgos**

Cuando encuentres un acoplamiento, docúmentalo así:

```markdown
### **Hallazgo #X: [Descripción del acoplamiento]**

**Módulo origen:** [Dónde está el código]  
**Módulo destino:** [A qué módulo importa]  
**Tipo:** [Acoplamiento fuerte/medio/débil]  
**Criticidad:** 🔴 Alta / 🟡 Media / 🟢 Baja  

**Archivos afectados:**
- `ruta/archivo1.cs` → línea X
- `ruta/archivo2.cs` → línea Y

**Usings problemáticos:**
\`\`\`csharp
using GestLog.Modules.X.Y.Z;
\`\`\`

**Impacto:** [Explicar consecuencias]

**Solución propuesta:** [Qué hacer para desacoplarlo]
```

---

## 🔍 Cómo Investigar Acoplamientos

### **Comando para encontrar usings problemáticos:**
```powershell
# Buscar all imports de GestionMantenimientos fuera del módulo
Get-ChildItem -Path "e:\Softwares\GestLog" -Filter "*.cs" -Recurse |
  Select-String "using GestLog.Modules.GestionMantenimientos" |
  Where-Object { $_.Path -notmatch "GestionMantenimientos" } |
  Format-Table Path, LineNumber

# Resultado esperado: Solo debe haber usings en archivos dentro de GestionMantenimientos
```

### **Buscar en archivos específicos:**
```powershell
# GestionEquipos
Select-String "using GestLog.Modules.GestionMantenimientos" `
  -Path "e:\Softwares\GestLog\ViewModels\Tools\GestionEquipos\*.cs"

# GestionEquiposInformaticos
Select-String "using GestLog.Modules.GestionMantenimientos" `
  -Path "e:\Softwares\GestLog\Modules\GestionEquiposInformaticos\**\*.cs"
```

---

## 📚 Referencias

- **copilot-instructions.md** - Patrones de organización de módulos
- **GestionMantenimientos** - Ejemplo de refactorización completa
- **MVVM Messaging** - CommunityToolkit.Mvvm.Messaging para comunicación

---

## 🎯 Próximos Pasos

1. **Leer este documento** antes de cualquier refactorización
2. **Ejecutar auditoría completa** usando los comandos PowerShell
3. **Evaluar opciones** con el equipo (A, B, o C)
4. **Crear issue/PR** para la refactorización con referencia a este documento
5. **Actualizar copilot-instructions.md** con el patrón elegido

---

**Estado:** ⏳ **Pendiente de refactorización**  
**Nota:** El usuario indicó "eso luego lo arreglaremos" - Este documento sirve como recordatorio y guía para cuando se aborde la tarea.

