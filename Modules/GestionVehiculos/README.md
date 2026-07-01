# Módulo Gestión de Vehículos 🚗

## 📋 Descripción
Módulo completo para la gestión de vehículos y documentos (SOAT, Tecno-Mecánica, etc.) en GestLog.

## 🏗️ Estructura refactorizada

```
Modules/GestionVehiculos/
├── Views/Vehicles/
│   ├── GestionVehiculosHomeView.xaml(.cs)
│   ├── VehicleFormDialog.xaml(.cs)
│   ├── VehicleDetailsView.xaml(.cs)
│   ├── VehicleDocumentsView.xaml(.cs)
│   ├── VehicleDocumentDialog.xaml(.cs)
│   └── Dialog/ConfirmDialog.xaml(.cs)
│
├── ViewModels/Vehicles/
│   ├── GestionVehiculosHomeViewModel.cs
│   ├── VehicleFormViewModel.cs
│   ├── VehicleDetailsViewModel.cs
│   ├── VehicleDocumentsViewModel.cs
│   └── VehicleDocumentDialogViewModel.cs ✅ (renombrado)
│
├── Services/
│   ├── Data/
│   │   ├── VehicleService.cs
│   │   └── VehicleDocumentService.cs
│   ├── Dialog/
│   │   ├── VehicleDocumentDialogService.cs
│   │   └── AppDialogService.cs
│   ├── Storage/ (planned)
│   ├── NetworkFileStorageService.cs
│   └── ServiceCollectionExtensions.cs ✅ (actualizado)
│
├── Interfaces/
│   ├── Data/ ✅ (jerárquico)
│   │   ├── IVehicleService.cs
│   │   └── IVehicleDocumentService.cs
│   ├── Dialog/
│   │   ├── IVehicleDocumentDialogService.cs
│   │   └── IAppDialogService.cs
│   └── Storage/ ✅ (nuevo)
│       └── IPhotoStorageService.cs
│
├── Models/
│   ├── DTOs/
│   │   ├── VehicleDto.cs
│   │   ├── VehicleDocumentDto.cs
│   │   └── ReplaceDocumentResultDto.cs
│   ├── Entities/
│   │   ├── Vehicle.cs
│   │   └── VehicleDocument.cs
│   └── Enums/
│       ├── VehicleState.cs
│       ├── VehicleType.cs
│       └── DocumentStatus.cs
│
├── Messages/
│   └── Documents/
│       ├── VehicleDocumentUploadProgressMessage.cs
│       └── VehicleDocumentCreatedMessage.cs
│
├── Utilities/ ✅ (nuevo)
│   ├── VehicleStateUtils.cs
│   └── DocumentStatusUtils.cs
│
└── Docs/
    └── README.md (este archivo)
```

## ✅ Características implementadas

- ✅ CRUD completo de vehículos y documentos
- ✅ Gestión de archivos (SOAT, Tecno-Mecánica, etc.)
- ✅ Carga y descarga de documentos
- ✅ Previsualización de imágenes y PDF
- ✅ Validación de documentos (tamaño, formato, contenido)
- ✅ Reemplazo automático de documentos vencidos
- ✅ Estadísticas de documentos
- ✅ Messaging reactivo (CommunityToolkit.Mvvm)
- ✅ Async/await en todas las operaciones I/O
- ✅ Logging completo con IGestLogLogger
- ✅ Soft delete de registros

## 🔐 Permisos requeridos

- `GestionVehiculos.Acceder` - Acceso completo al módulo
- `GestionVehiculos.Eliminar` - Eliminar vehículos y documentos

## 🎨 Colores y estados

### Estados de vehículos
- **Activo** - Verde #2B8E3F
- **En mantenimiento** - Ámbar #F9B233
- **Dado de baja** - Gris claro #EDEDED
- **Inactivo** - Gris oscuro #9E9E9E

Ver `VehicleStateUtils.cs` para conversiones automáticas.

### Estados de documentos
- **Vigente** - Verde #10B981
- **Próximo a vencer** - Ámbar #F59E0B
- **Vencido** - Rojo #C0392B
- **Archivado** - Gris #9E9E9E

Ver `DocumentStatusUtils.cs` para conversiones automáticas.

## 🔄 Refactorización completada (Febrero 2026)

✅ Interfaces reorganizadas jerárquicamente: `Interfaces/Data/` y `Interfaces/Storage/`
✅ Messages expandidos con estructura por dominio: `Vehicles/`, `Documents/`, `UI/`
✅ Utilities creadas para conversiones de estado
✅ ViewModel renombrado: `VehicleDocumentDialogModel` → `VehicleDocumentDialogViewModel`
✅ Namespaces actualizados en todos los archivos
✅ ServiceCollectionExtensions.cs refactorizado

## 📚 Próximos pasos

1. Implementar validaciones de permisos (CanAccederGestionVehiculos, CanEliminarGestionVehiculos)
2. Enlazar permisos en UI (botones, comandos)
3. Agregar más Messages para eventos críticos
4. Mejorar UI con estilos corporativos
5. Crear tests unitarios para servicios

## 📖 Estándares aplicados

- SRP: Una responsabilidad por clase
- Async: Siempre para operaciones I/O
- DI: Inyección por constructor obligatoria
- Logging: IGestLogLogger en todo el código
- MVVM: Cero lógica en code-behind
- Validación: Antes de procesar datos
- Mensajería: CommunityToolkit.Mvvm.Messaging
