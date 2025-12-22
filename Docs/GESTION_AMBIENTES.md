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

**Última actualización**: 22 de diciembre de 2025

