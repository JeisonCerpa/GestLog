# 🚀 Manual del Sistema de Actualizaciones Automáticas - Velopack

## 📋 Índice
- [Introducción](#introducción)
- [Configuración del Entorno](#configuración-del-entorno)
- [Proceso de Empaquetado](#proceso-de-empaquetado)
- [Despliegue al Servidor](#despliegue-al-servidor)
- [Flujo de Actualizaciones](#flujo-de-actualizaciones)
- [Resolución de Problemas](#resolución-de-problemas)
- [Comandos de Referencia](#comandos-de-referencia)

---

## 🎯 Introducción

GestLog utiliza **Velopack** como sistema de actualizaciones automáticas, proporcionando:
- ✅ **Actualizaciones incrementales** usando archivos delta
- ✅ **Verificación automática** en segundo plano al iniciar la aplicación
- ✅ **Instalación silenciosa** con permisos de administrador preexistentes
- ✅ **Rollback automático** en caso de errores
- ✅ **Verificación de integridad** de las actualizaciones

---

## ⚙️ Configuración del Entorno

### **Prerequisitos**
1. **.NET 9.0 SDK** instalado
2. **Velopack CLI** instalado globalmente:
   ```powershell
   dotnet tool install -g velopack
   ```
3. **Acceso al servidor de actualizaciones**: `\\SIMICSGROUPWKS1\Hackerland\Programas\GestLogUpdater`

### **Verificar Instalación**
```powershell
vpk --version
```

---

## 📦 Proceso de Empaquetado

### **Paso 1: Actualizar Versión**

#### **1.1 Actualizar GestLog.csproj**
```xml
<PropertyGroup>
    <AssemblyVersion>1.0.X.0</AssemblyVersion>
    <FileVersion>1.0.X.0</FileVersion>
    <Version>1.0.X</Version>
    <AssemblyInformationalVersion>1.0.X</AssemblyInformationalVersion>
    <AssemblyMetadata Include="ProductVersion" Value="1.0.X" />
</PropertyGroup>
```

#### **1.2 Actualizar MainWindow.xaml (Opcional)**
```xml
Title="GestLog - Sistema de Gestión v1.0.X"
```

#### **1.3 Actualizar Vista de Información (HomeView.xaml.cs)**
```csharp
private void btnInfo_Click(object sender, RoutedEventArgs e)
{
    System.Windows.MessageBox.Show(
        "GestLog v1.0.X\n\n" +
        "Sistema modular de gestión integrada\n" +
        "Desarrollado con .NET 9 y WPF\n\n" +
        "Módulos integrados:\n" +
        "• DaaterProccesor - Procesamiento de datos Excel\n" +
        "• Gestión de Cartera - Estados de cuenta PDF\n" +
        "• Envío de Catálogo - Envío masivo de catálogo\n\n" +
        "Estado: ✅ Operativo\n" +        "Actualizaciones: ✅ Sistema Velopack completamente funcional\n" +
        "Seguridad: 🔐 Privilegios de administrador mediante manifiesto\n" +
        "Comportamiento: 🎯 Detección silenciosa, aplicación con confirmación del usuario",
        "Información del Sistema",
        MessageBoxButton.OK,
        MessageBoxImage.Information
    );
}
```

### **Paso 2: Compilar en Release**
```powershell
cd "e:\Softwares\GestLog"
dotnet clean
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=false
```

### **Paso 3: Generar Paquetes Velopack**
```powershell
vpk pack --packId GestLog --packVersion 1.0.X --packDir "bin\Release\net9.0-windows\win-x64\publish" --mainExe "GestLog.exe"
```

#### **Archivos Generados:**
- `GestLog-1.0.X-full.nupkg` - Paquete completo (~95 MB)
- `GestLog-1.0.X-delta.nupkg` - Paquete incremental (varía según cambios)
- `GestLog-win-Setup.exe` - Instalador standalone
- `GestLog-win-Portable.zip` - Versión portable
- `RELEASES` - Manifiesto de versiones
- `releases.win.json` - Metadatos detallados

---

## 🌐 Despliegue al Servidor

### **Comando de Despliegue**
```powershell
vpk upload local --path "\\SIMICSGROUPWKS1\Hackerland\Programas\GestLogUpdater"
```

### **Verificar Despliegue**
```powershell
dir "\\SIMICSGROUPWKS1\Hackerland\Programas\GestLogUpdater" | findstr "1.0"
```

### **Estructura del Servidor**
```
\\SIMICSGROUPWKS1\Hackerland\Programas\GestLogUpdater\
├── GestLog-1.0.X-full.nupkg     # Paquete completo
├── GestLog-1.0.X-delta.nupkg    # Paquete incremental
├── GestLog-win-Setup.exe         # Instalador
├── GestLog-win-Portable.zip      # Versión portable
├── RELEASES                       # Manifiesto principal
└── releases.win.json            # Metadatos JSON
```

---

## 🔄 Flujo de Actualizaciones

### **Para Usuarios Finales**

#### **Actualización Automática (Recomendada)**
1. **Inicio Normal**: GestLog se ejecuta con permisos de administrador (mediante manifiesto)
2. **Verificación Silenciosa**: Busca actualizaciones en segundo plano (3 segundos después del inicio)
3. **Notificación al Usuario**: Si hay actualizaciones, muestra un diálogo preguntando si desea actualizar
4. **Descarga y Aplicación**: Si el usuario acepta, descarga e instala automáticamente
5. **Reinicio Automático**: La aplicación se reinicia con la nueva versión

> **Nota sobre elevación de privilegios:**
> - **GestLog requiere privilegios de administrador para ejecutarse** debido a las operaciones que realiza (acceso a servidor de red, manipulación de archivos del sistema, etc.).
> - La aplicación está configurada con un manifiesto (`app.manifest`) que solicita automáticamente elevación al iniciar.
> - Las actualizaciones se aplicarán automáticamente sin solicitar permisos adicionales, ya que la aplicación ya se ejecuta como administrador.
> - **Importante**: Los usuarios siempre verán el diálogo UAC al iniciar GestLog, esto es normal y necesario para el correcto funcionamiento.

#### **Instalación Manual de Nueva Versión**
1. Ejecutar `GestLog-win-Setup.exe` como **Administrador**
2. Seguir el asistente de instalación
3. La nueva versión reemplaza automáticamente la anterior

#### **Experiencia del Usuario**

#### **Sin Actualizaciones**
- ✅ Inicio normal con solicitud UAC (requerida por privilegios de administrador)
- ✅ No aparecen diálogos adicionales una vez iniciada la aplicación

#### **Con Actualizaciones Disponibles**
- 🔍 Detección automática en segundo plano
- 💬 Diálogo de confirmación al usuario
- 📥 Descarga e instalación solo si el usuario acepta
- 🔄 Reinicio transparente con nueva versión

---

## 🛠️ Resolución de Problemas

### **Error: "Acceso Denegado" durante Actualización**

#### **Causa**
La aplicación no tiene permisos suficientes para modificar archivos.

#### **Solución**
1. **Para Usuarios**: Asegurar que GestLog se ejecute como Administrador (ya configurado mediante manifiesto)
2. **Para Desarrolladores**: El sistema de auto-elevación fue removido en v1.0.9+ para mayor estabilidad

#### **Verificación**
```powershell
Get-Content "$env:LOCALAPPDATA\GestLog\velopack.log" -Tail 20
```

### **Error: "No se Encuentra el Servidor de Actualizaciones"**

#### **Causa**
El servidor de actualizaciones no está disponible o la ruta es incorrecta.

#### **Verificación**
```powershell
Test-Path "\\SIMICSGROUPWKS1\Hackerland\Programas\GestLogUpdater"
dir "\\SIMICSGROUPWKS1\Hackerland\Programas\GestLogUpdater"
```

#### **Solución**
1. Verificar conectividad de red
2. Comprobar permisos de acceso al servidor
3. Validar configuración en `appsettings.production.json`

### **Error: "Paquete Corrupto o Incompleto"**

#### **Causa**
El archivo de actualización se dañó durante la descarga.

#### **Solución**
```powershell
# Limpiar cache de actualizaciones
Remove-Item "$env:LOCALAPPDATA\GestLog\packages\*" -Recurse -Force
```

### **Logs de Diagnóstico**

#### **Ubicación de Logs**
- **Aplicación**: `$env:LOCALAPPDATA\GestLog\current\Logs\gestlog-YYYYMMDD.txt`
- **Velopack**: `$env:LOCALAPPDATA\GestLog\velopack.log`
- **Instalación**: `$env:LOCALAPPDATA\GestLog\current\sq.version`

#### **Comandos Útiles**
```powershell
# Ver versión actual instalada
Get-Content "$env:LOCALAPPDATA\GestLog\current\sq.version"

# Ver logs recientes de actualizaciones
Get-Content "$env:LOCALAPPDATA\GestLog\velopack.log" -Tail 50

# Ver logs de la aplicación
Get-Content "$env:LOCALAPPDATA\GestLog\current\Logs\gestlog-$(Get-Date -Format 'yyyyMMdd').txt" -Tail 30

# Verificar procesos de Velopack
Get-Process | Where-Object {$_.ProcessName -like "*Update*" -or $_.ProcessName -like "*velopack*"}
```

---

## 📝 Comandos de Referencia

### **Desarrollo y Empaquetado**

#### **Compilación Release**
```powershell
cd "e:\Softwares\GestLog"
dotnet clean
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=false
```

#### **Generar Paquete Velopack**
```powershell
vpk pack --packId GestLog --packVersion <VERSION> --packDir "bin\Release\net9.0-windows\win-x64\publish" --mainExe "GestLog.exe"
```

#### **Subir al Servidor**
```powershell
vpk upload local --path "\\SIMICSGROUPWKS1\Hackerland\Programas\GestLogUpdater"
```

### **Verificación y Diagnóstico**

#### **Verificar Conexión al Servidor**
```powershell
Test-Path "\\SIMICSGROUPWKS1\Hackerland\Programas\GestLogUpdater"
```

#### **Listar Versiones Disponibles**
```powershell
dir "\\SIMICSGROUPWKS1\Hackerland\Programas\GestLogUpdater" | findstr ".nupkg"
```

#### **Ver Contenido del Manifiesto**
```powershell
Get-Content "\\SIMICSGROUPWKS1\Hackerland\Programas\GestLogUpdater\releases.win.json"
```

### **Gestión de Instalaciones Locales**

#### **Ver Versión Instalada**
```powershell
Get-Content "$env:LOCALAPPDATA\GestLog\current\sq.version"
```

#### **Limpiar Cache de Actualizaciones**
```powershell
Remove-Item "$env:LOCALAPPDATA\GestLog\packages\*" -Recurse -Force
```

#### **Reinstalar Desde Cero**
```powershell
# 1. Desinstalar desde Panel de Control
# 2. Limpiar directorios residuales
Remove-Item "$env:LOCALAPPDATA\GestLog" -Recurse -Force -ErrorAction SilentlyContinue
# 3. Ejecutar nuevo instalador como administrador
```

---

## 🔐 Consideraciones de Seguridad

### **Principios Implementados**
- ✅ **Menor Privilegio**: Solo solicita admin cuando es necesario
- ✅ **Validación de Integridad**: Verifica firmas y hashes
- ✅ **Origen Confiable**: Solo acepta actualizaciones del servidor autorizado
- ✅ **Proceso Controlado**: Maneja errores y permite rollback

### **Recomendaciones para Producción**
1. **Ejecutar como Usuario Estándar**: La aplicación maneja la elevación automáticamente
2. **Mantener Servidor Seguro**: Acceso restringido al directorio de actualizaciones
3. **Monitorear Logs**: Revisar logs regularmente para detectar problemas
4. **Backup Regular**: Mantener copias de versiones estables

---

## 📊 Flujo de Versionado

### **Estrategia de Versiones**
- **Major.Minor.Patch** (ej: 1.0.5)
- **Major**: Cambios importantes o breaking changes
- **Minor**: Nuevas características compatible
- **Patch**: Correcciones de bugs y mejoras menores

### **Ejemplo de Flujo Completo**

#### **Versión 1.0.5 → 1.0.6**
```powershell
# 1. Actualizar versión en código
# Edit: GestLog.csproj y MainWindow.xaml

# 2. Compilar
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=false

# 3. Empaquetar
vpk pack --packId GestLog --packVersion 1.0.6 --packDir "bin\Release\net9.0-windows\win-x64\publish" --mainExe "GestLog.exe"

# 4. Desplegar
vpk upload local --path "\\SIMICSGROUPWKS1\Hackerland\Programas\GestLogUpdater"

# 5. Verificar
dir "\\SIMICSGROUPWKS1\Hackerland\Programas\GestLogUpdater" | findstr "1.0.6"
```

#### **Resultado Esperado**
- Los usuarios con v1.0.5 reciben automáticamente la actualización a v1.0.6
- Descarga delta pequeña (~200 KB típicamente)
- Aplicación automática con elevación de privilegios
- Reinicio transparente con nueva versión

---

## 🎯 Mejores Prácticas

### **Para Desarrolladores**
1. **Probar Antes de Desplegar**: Siempre probar actualizaciones localmente
2. **Verificar Dependencias**: Asegurar que todas las DLLs estén incluidas
3. **Documentar Cambios**: Mantener changelog actualizado
4. **Monitorear Despliegues**: Verificar que las actualizaciones se apliquen correctamente

### **Para Administradores**
1. **Backup del Servidor**: Mantener copias de versiones estables
2. **Acceso Controlado**: Restringir acceso al directorio de actualizaciones
3. **Monitoreo de Logs**: Revisar logs de actualización regularmente
4. **Pruebas en Entorno**: Probar actualizaciones antes de producción

### **Para Usuarios Finales**
1. **Permitir Actualizaciones**: Aceptar UAC cuando aparezca para actualizaciones
2. **Reportar Problemas**: Informar cualquier problema durante actualizaciones
3. **Mantener Conectividad**: Asegurar acceso a la red corporativa
4. **No Interrumpir**: Permitir que las actualizaciones se completen

---

*Última actualización: 20 de agosto de 2025*
*Versión del documento: 1.1 - Sistema sin auto-elevación implementado en v1.0.9*
