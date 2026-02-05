# Módulo Gestión de Vehículos

Estructura mínima creada para empezar la implementación del módulo Gestión de Vehículos.

Carpetas creadas:
- Views/Vehicles
- ViewModels/Vehicles
- Services
- Interfaces/IData
- Models/DTOs
- Models/Entities

Archivos placeholder:
- GestionVehiculosHomeView.xaml + .cs
- GestionVehiculosHomeViewModel.cs
- ServiceCollectionExtensions.cs (registro DI)
- IVehicleService.cs
- VehicleDto.cs
- Vehicle.cs

Siguientes pasos recomendados:
1. Registrar `AddGestionVehiculosModule` en el contenedor DI principal (Startup).
2. Implementar `IVehicleService` usando EF Core y crear la tabla correspondiente.
3. Crear la vista de lista de vehículos y formularios de edición.
4. Agregar permisos en la base de datos (ya creado: Herramientas.AccederGestionVehiculos).
5. Crear migrations EF si se implementan entidades.

Reglas: seguir `copilot-instructions.md` para organisation y estándares.
