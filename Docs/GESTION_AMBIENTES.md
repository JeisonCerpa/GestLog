# 🌍 Cambiar entre Development y Production

## ⚡ Cambiar de Ambiente - Opción Rápida

### Ver ambiente actual:
```powershell
$env:GESTLOG_ENVIRONMENT
```

---

## 📌 OPCIÓN 1: Cambio rápido (solo sesión actual)

```powershell
# Para Development
$env:GESTLOG_ENVIRONMENT = "Development"

# Para Production
$env:GESTLOG_ENVIRONMENT = "Production"
```

⚠️ **Nota**: Se pierde al cerrar PowerShell

---

## 🔧 OPCIÓN 2: Cambio permanente (RECOMENDADO) - PowerShell

```powershell
# Para Production (PERMANENTE)
[Environment]::SetEnvironmentVariable(
  "GESTLOG_ENVIRONMENT",
  "Production",
  "User"
)

# Para Development (PERMANENTE)
[Environment]::SetEnvironmentVariable(
  "GESTLOG_ENVIRONMENT",
  "Development",
  "User"
)
```

✅ **Ventaja**: Funciona inmediatamente en nuevas sesiones de PowerShell sin necesidad de GUI

---

## 🔧 OPCIÓN 3: Cambio permanente - Variables de Sistema Windows (GUI)

1. **Windows + X** → "Sistema"
2. **"Configuración avanzada del sistema"** en la derecha
3. **Botón "Variables de entorno..."** abajo
4. **"Nuevo..."** en "Variables de usuario"
5. Nombre: `GESTLOG_ENVIRONMENT`
6. Valor: `Production` o `Development`
7. **OK** dos veces
8. **Cierra todas las PowerShell**
9. **Abre una NUEVA PowerShell** y verifica:
   ```powershell
   $env:GESTLOG_ENVIRONMENT
   ```

---

## 🎯 Resumen Rápido
| Acción | Método |
|--------|--------|
| Ver ambiente actual | `$env:GESTLOG_ENVIRONMENT` en PowerShell |
| Development (sesión) | `$env:GESTLOG_ENVIRONMENT = "Development"` |
| Production (sesión) | `$env:GESTLOG_ENVIRONMENT = "Production"` |
| Permanente (Development) | **Opción 2**: `[Environment]::SetEnvironmentVariable("GESTLOG_ENVIRONMENT", "Development", "User")` |
| Permanente (Production) | **Opción 2**: `[Environment]::SetEnvironmentVariable("GESTLOG_ENVIRONMENT", "Production", "User")` |

---

## 🛠️ Aplicar migraciones en Development y Production

Para cambios de esquema (como permitir múltiples facturas activas), aplica migración en **ambos ambientes**.

### 1) Development

```powershell
$env:GESTLOG_ENVIRONMENT = "Development"
dotnet ef database update
```

Verifica:

```powershell
dotnet ef migrations list
```

### 2) Production

```powershell
$env:GESTLOG_ENVIRONMENT = "Production"
dotnet ef database update
```

Verifica:

```powershell
dotnet ef migrations list
```

### Recomendaciones para Production

- Realizar respaldo de base de datos antes de `dotnet ef database update`.
- Ejecutar en ventana de mantenimiento.
- Confirmar conexión del ambiente (`GESTLOG_ENVIRONMENT=Production`) antes de correr el comando.

---

**Última actualización**: 3 de marzo de 2026

