# 🎯 Plan de Migración Auto-Refresh - Módulos Específicos GestLog

## 📋 **MÓDULOS IDENTIFICADOS PARA MIGRACIÓN**

### **🔧 ANÁLISIS REALIZADO**
He analizado todos los ViewModels en los 4 módulos prioritarios y identificado cuáles usan base de datos directamente:

---

## 🚀 **MÓDULO 1: GestionEquiposInformaticos**

### **✅ PerifericosViewModel** 
- **Estado:** ✅ **YA IMPLEMENTADO** con `DatabaseAwareViewModel`
- **Archivo:** `Modules/GestionEquiposInformaticos/ViewModels/PerifericosViewModel.cs`
- **Tipo BD:** `IDbContextFactory<GestLogDbContext>` ✅

### **✅ EquiposInformaticosViewModel** 
- **Estado:** ✅ **COMPLETADO** - Migrado con `DatabaseAwareViewModel`
- **Archivo:** `ViewModels/Tools/GestionEquipos/EquiposInformaticosViewModel.cs`
- **Tipo BD:** `IDbContextFactory<GestLogDbContext>` ✅ con timeout 1s
- **Auto-refresh:** ✅ Implementado automáticamente
- **Compilación:** ✅ Exitosa

### **🔄 DetallesEquipoInformaticoViewModel**
- **Estado:** 🔄 **PENDIENTE - PRIORIDAD MEDIA**
- **Archivo:** `ViewModels/Tools/GestionEquipos/DetallesEquipoInformaticoViewModel.cs`
- **Tipo BD:** `GestLogDbContext? _db`
- **Tiempo estimado:** 30 minutos

### **🔄 AgregarEquipoInformaticoViewModel**
- **Estado:** 🔄 **PENDIENTE - PRIORIDAD BAJA**
- **Archivo:** `ViewModels/Tools/GestionEquipos/AgregarEquipoInformaticoViewModel.cs`
- **Tipo BD:** Crea instancias directas de `GestLogDbContext`
- **Tiempo estimado:** 60 minutos
- **Complejidad:** ALTA - Múltiples instanciaciones directas

### **⚠️ Otros ViewModels en GestionEquiposInformaticos:**
Los siguientes **NO necesitan migración** porque usan servicios (no acceso directo a BD):
- `CrearPlanCronogramaViewModel`
- `RegistroEjecucionPlanViewModel` 
- `HistorialEjecucionesViewModel`
- `CronogramaDiarioViewModel`
- `RegistroMantenimientoEquipoViewModel`

---

## 🔧 **MÓDULO 2: GestionMantenimientos**

### **✅ ESTADO: No necesita migración inmediata**
**Razón:** Todos los ViewModels usan servicios (`ISeguimientoService`, etc.) en lugar de acceso directo a BD.

**ViewModels analizados (todos usan servicios):**
- `SeguimientoViewModel` → usa `ISeguimientoService`
- `RegistroMantenimientoViewModel` → usa servicios
- `MantenimientoDiarioViewModel` → usa servicios
- `EquiposViewModel` → usa servicios
- `CronogramaViewModel` → usa servicios

**🎯 Acción requerida:** Verificar que los servicios subyacentes ya usen `IDbContextFactory` con timeout de 1 segundo.

---

## 👥 **MÓDULO 3: Usuarios (IdentidadCatalogos)**

### **🔄 UsuarioManagementViewModel**
- **Estado:** 🔄 **PENDIENTE - PRIORIDAD ALTA**
- **Archivo:** `Modules/Usuarios/ViewModels/UsuarioManagementViewModel.cs`
- **Tipo BD:** Usa servicios + 1 acceso directo con `GestLogDbContextFactory`
- **Tiempo estimado:** 30 minutos
- **Estrategia:** Implementar auto-refresh para coordinar servicios

### **✅ Otros ViewModels en Usuarios:**
**NO necesitan migración** porque usan servicios:
- `PersonaManagementViewModel` → usa `IPersonaService`
- `RolManagementViewModel` → usa servicios
- `CatalogosManagementViewModel` → usa servicios
- `AuditoriaManagementViewModel` → usa servicios
- `GestionPermisosRolViewModel` → usa servicios
- `LoginViewModel` → autenticación, no gestión de datos

---

## 👤 **MÓDULO 4: Personas**

### **✅ ESTADO: No necesita migración**
**Razón:** Todos los ViewModels de personas están en el módulo `Usuarios` y usan servicios (`IPersonaService`).

---

## 📊 **RESUMEN EJECUTIVO**

### **ViewModels que REQUIEREN migración:**

#### **🔥 PRIORIDAD CRÍTICA:**
1. **EquiposInformaticosViewModel** - Módulo principal de gestión de equipos
2. **UsuarioManagementViewModel** - Gestión crítica de usuarios

#### **📋 PRIORIDAD MEDIA:**
3. **DetallesEquipoInformaticoViewModel** - Detalles de equipos

#### **📝 PRIORIDAD BAJA:**
4. **AgregarEquipoInformaticoViewModel** - Formulario de creación

### **Total de ViewModels a migrar: 4**
### **Tiempo total estimado: 2.75 horas**

---

## 🚀 **PLAN DE IMPLEMENTACIÓN INMEDIATA**

### **FASE 1 (Hoy - 1.25 horas):**
- [x] ✅ PerifericosViewModel (COMPLETADO)
- [ ] 🔄 EquiposInformaticosViewModel (45 min)
- [ ] 🔄 UsuarioManagementViewModel (30 min)

### **FASE 2 (Mañana - 1.5 horas):**
- [ ] 🔄 DetallesEquipoInformaticoViewModel (30 min)
- [ ] 🔄 AgregarEquipoInformaticoViewModel (60 min)

### **VALIDACIÓN (30 min):**
- [ ] Testing de auto-refresh en todos los módulos
- [ ] Verificación de timeouts ultrarrápidos
- [ ] Validación de experiencia de usuario

---

## 🎯 **NEXT ACTION: EquiposInformaticosViewModel**

**Archivo a modificar:** `ViewModels/Tools/GestionEquipos/EquiposInformaticosViewModel.cs`

**Cambios requeridos:**
1. ✅ Herencia: `ObservableObject` → `DatabaseAwareViewModel`
2. ✅ Constructor: Agregar `IDatabaseConnectionService`, cambiar a `IDbContextFactory`
3. ✅ Implementar `RefreshDataAsync()`
4. ✅ Actualizar método de carga con timeout de 1 segundo
5. ✅ Eliminar propiedades duplicadas

**¿Proceder con EquiposInformaticosViewModel ahora?**
