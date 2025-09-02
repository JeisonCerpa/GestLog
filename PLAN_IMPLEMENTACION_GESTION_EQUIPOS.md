# 🏗️ Plan de Implementación - Módulo Gestión de Equipos

## 📋 Resumen Ejecutivo
**Proyecto**: Migración de Mtto PC a GestLog como módulo nativo  
**Objetivo**: Sistema completo de gestión de equipos, conexiones, periféricos y mantenimiento  
**Tiempo Estimado**: 6-8 días laborales  
**Estrategia**: Módulo interno con arquitectura normalizada y UI moderna  

---

## 🎯 Fase 1: Configuración del Módulo (1 día)

### 1.1 Estructura de Carpetas
```
Modules/GestionEquipos/
├── ViewModels/
│   ├── EquiposViewModel.cs
│   ├── ConexionesViewModel.cs
│   ├── PerifericosViewModel.cs
│   ├── CronogramaViewModel.cs
│   └── GestionEquiposHomeViewModel.cs
├── Views/
│   ├── EquiposView.xaml
│   ├── ConexionesView.xaml
│   ├── PerifericosView.xaml
│   ├── CronogramaView.xaml
│   └── GestionEquiposHomeView.xaml
├── Services/
│   ├── IEquipoService.cs
│   ├── EquipoService.cs
│   ├── IConexionService.cs
│   ├── ConexionService.cs
│   ├── IPerifericoService.cs
│   ├── PerifericoService.cs
│   └── IExportacionService.cs
├── Models/
│   ├── DTOs/
│   ├── Entities/
│   └── Enums/
├── Interfaces/
└── Database/
    ├── Entities/
    ├── Migrations/
    └── Configuration/
```

### 1.2 Entidades EF Core Normalizadas

```csharp
// Entidad principal de equipos
public class EquipoEntity
{
    public string Codigo { get; set; } = string.Empty; // PK
    public string? UsuarioAsignado { get; set; }
    public string? NombreEquipo { get; set; }
    public decimal? Costo { get; set; }
    public DateTime? FechaCompra { get; set; }
    public EstadoEquipo Estado { get; set; }
    public Sede Sede { get; set; }
    public string? CodigoAnydesk { get; set; }
    public TipoEquipo TipoEquipo { get; set; }
    
    // Especificaciones técnicas
    public string? Modelo { get; set; }
    public string? SO { get; set; }
    public string? Marca { get; set; }
    public string? SerialNumber { get; set; }
    public string? Procesador { get; set; }
    
    // RAM
    public int? SlotsTotales { get; set; }
    public int? SlotsUtilizados { get; set; }
    public TipoRam? TipoRam { get; set; }
    
    // Almacenamiento
    public int? CantidadDiscos { get; set; }
    
    public string? Observaciones { get; set; }
    public DateTime? FechaBaja { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    
    // Navegación
    public virtual ICollection<ConexionRedEntity> Conexiones { get; set; } = new List<ConexionRedEntity>();
    public virtual ICollection<SlotRamEntity> SlotsRam { get; set; } = new List<SlotRamEntity>();
    public virtual ICollection<DiscoEntity> Discos { get; set; } = new List<DiscoEntity>();
    public virtual ICollection<MantenimientoEntity> Mantenimientos { get; set; } = new List<MantenimientoEntity>();
}

// Conexiones normalizadas (1:N)
public class ConexionRedEntity
{
    public int Id { get; set; }
    public string CodigoEquipo { get; set; } = string.Empty;
    public TipoConexion Tipo { get; set; }
    public string? DireccionMAC { get; set; }
    public string? DireccionIP { get; set; }
    public string? MascaraSubred { get; set; }
    public string? PuertaEnlace { get; set; }
    public bool Activa { get; set; } = true;
    
    // Navegación
    public virtual EquipoEntity Equipo { get; set; } = null!;
}

// Slots RAM normalizados (1:N)
public class SlotRamEntity
{
    public int Id { get; set; }
    public string CodigoEquipo { get; set; } = string.Empty;
    public int NumeroSlot { get; set; }
    public string? CapacidadGB { get; set; }
    public string? TipoMemoria { get; set; }
    public bool Ocupado { get; set; }
    
    // Navegación
    public virtual EquipoEntity Equipo { get; set; } = null!;
}

// Discos normalizados (1:N)
public class DiscoEntity
{
    public int Id { get; set; }
    public string CodigoEquipo { get; set; } = string.Empty;
    public int NumeroDisco { get; set; }
    public TipoDisco Tipo { get; set; }
    public string? Capacidad { get; set; }
    public bool Activo { get; set; } = true;
    
    // Navegación
    public virtual EquipoEntity Equipo { get; set; } = null!;
}

// Periféricos
public class PerifericoEntity
{
    public string Codigo { get; set; } = string.Empty; // PK
    public TipoDispositivo Dispositivo { get; set; }
    public DateTime? FechaCompra { get; set; }
    public decimal? Costo { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Serial { get; set; }
    public string? UsuarioAsignado { get; set; }
    public Sede Sede { get; set; }
    public EstadoPeriferico Estado { get; set; }
    public string? Observaciones { get; set; }
    public DateTime? FechaBaja { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}

// Cronograma de equipos por horas y días de la semana (calendario semanal)
public class CronogramaEquipoEntity
{
    public TimeSpan Hora { get; set; } // PK - Hora del día (ej: 08:00, 09:00, etc.)
    
    // Equipos asignados por día de semana
    public string? Lunes { get; set; }
    public string? LunesActividad { get; set; }
    public string? Martes { get; set; }
    public string? MartesActividad { get; set; }
    public string? Miercoles { get; set; }
    public string? MiercolesActividad { get; set; }
    public string? Jueves { get; set; }
    public string? JuevesActividad { get; set; }
    public string? Viernes { get; set; }
    public string? ViernesActividad { get; set; }
    public string? Sabado { get; set; }
    public string? SabadoActividad { get; set; }
    public string? Domingo { get; set; }
    public string? DomingoActividad { get; set; }
    
    public DateTime? FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}

// Historial de asignaciones de equipos (para auditoría)
public class HistorialCronogramaEntity
{
    public int Id { get; set; }
    public TimeSpan Hora { get; set; }
    public DayOfWeek DiaSemana { get; set; }
    public string? CodigoEquipoAnterior { get; set; }
    public string? CodigoEquipoNuevo { get; set; }
    public string? ActividadAnterior { get; set; }
    public string? ActividadNueva { get; set; }
    public string? UsuarioModificacion { get; set; }
    public DateTime FechaCambio { get; set; }
    public string? Motivo { get; set; }
    
    // Navegación
    public virtual EquipoEntity? EquipoAnterior { get; set; }
    public virtual EquipoEntity? EquipoNuevo { get; set; }
}
```

### 1.3 Enums Tipados

```csharp
public enum EstadoEquipo
{
    Activo = 1,
    Mantenimiento = 2,
    DadoDeBaja = 3,
    EnReparacion = 4,
    Almacenado = 5
}

public enum TipoEquipo
{
    Desktop = 1,
    Laptop = 2,
    Servidor = 3,
    Tablet = 4,
    AllInOne = 5
}

public enum TipoConexion
{
    Ethernet = 1,
    WiFi = 2,
    Bluetooth = 3,
    USB = 4
}

public enum TipoRam
{
    DDR3 = 1,
    DDR4 = 2,
    DDR5 = 3
}

public enum TipoDisco
{
    HDD = 1,
    SSD = 2,
    NVMe = 3,
    Hibrido = 4
}

public enum TipoDispositivo
{
    Monitor = 1,
    Teclado = 2,
    Mouse = 3,
    Impresora = 4,
    Camara = 5,
    Audifonos = 6,
    Parlantes = 7,
    Otro = 99
}

public enum EstadoPeriferico
{
    Activo = 1,
    Dañado = 2,
    DadoDeBaja = 3,
    EnReparacion = 4
}

public enum TipoActividad
{
    Mantenimiento = 1,
    Limpieza = 2,
    Backup = 3,
    Actualizacion = 4,
    Diagnostico = 5,
    Reparacion = 6,
    Instalacion = 7,
    Capacitacion = 8,
    Revision = 9,
    Otro = 99
}

public enum TipoMantenimiento
{
    Preventivo = 1,
    Correctivo = 2,
    Predictivo = 3,
    Limpieza = 4,
    Actualizacion = 5,
    Diagnostico = 6,
    Reparacion = 7
}

public enum EstadoSeguimientoMantenimiento
{
    Programado = 1,
    EnProceso = 2,
    Completado = 3,
    Cancelado = 4,
    Diferido = 5,
    NoRealizado = 6,
    Atrasado = 7,
    RealizadoEnTiempo = 8,
    RealizadoFueraDeTiempo = 9,
    Pendiente = 10
}

public enum FrecuenciaMantenimiento
{
    Semanal = 1,
    Quincenal = 2,
    Mensual = 4,
    Bimestral = 8,
    Trimestral = 13,
    Semestral = 26,
    Anual = 52
}

public enum Sede
{
    Bogota = 1,
    Medellin = 2,
    Cali = 3,
    Barranquilla = 4,
    Bucaramanga = 5
}
```

---

## 🗃️ Fase 2: Migración de Base de Datos (1-2 días)

### 2.1 Script de Migración

```sql
-- Migración de datos desde DBMttoPc a GestLog
-- Insertar equipos
INSERT INTO EquipoEntity (Codigo, UsuarioAsignado, NombreEquipo, ...)
SELECT Codigo, Usuario_Asignado, Nombre_del_Equipo, ...
FROM [DBMttoPc].[dbo].[Equipos];

-- Normalizar conexiones (de 4 columnas a filas)
INSERT INTO ConexionRedEntity (CodigoEquipo, Tipo, DireccionMAC, DireccionIP, MascaraSubred, PuertaEnlace)
SELECT Codigo, 1, Direccion_MAC1, Direccion_IPv4_1, Mascara_de_Subred1, Puerta_de_Enlace1
FROM [DBMttoPc].[dbo].[Conexiones]
WHERE Conexion1 IS NOT NULL;

-- Similar para Conexion2, 3, 4...

-- Normalizar slots RAM
INSERT INTO SlotRamEntity (CodigoEquipo, NumeroSlot, CapacidadGB, Ocupado)
SELECT Codigo, 1, Slot1, CASE WHEN Slot1 IS NOT NULL THEN 1 ELSE 0 END
FROM [DBMttoPc].[dbo].[Equipos]
UNION ALL
SELECT Codigo, 2, Slot2, CASE WHEN Slot2 IS NOT NULL THEN 1 ELSE 0 END
FROM [DBMttoPc].[dbo].[Equipos]
-- ... hasta Slot8

-- Migrar periféricos
INSERT INTO PerifericoEntity (Codigo, Dispositivo, Marca, ...)
SELECT Codigo, Dispositivo, Marca, ...
FROM [DBMttoPc].[dbo].[Periferico];
```

### 2.2 EF Core Configuration

```csharp
public class GestionEquiposDbConfiguration : IEntityTypeConfiguration<EquipoEntity>
{
    public void Configure(EntityTypeBuilder<EquipoEntity> builder)
    {
        builder.HasKey(e => e.Codigo);
        builder.Property(e => e.Codigo).HasMaxLength(50);
        
        builder.HasMany(e => e.Conexiones)
            .WithOne(c => c.Equipo)
            .HasForeignKey(c => c.CodigoEquipo)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(e => e.SlotsRam)
            .WithOne(s => s.Equipo)
            .HasForeignKey(s => s.CodigoEquipo)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

## ⚙️ Fase 3: Servicios y Lógica de Negocio (2 días)

### 3.1 IEquipoService

```csharp
public interface IEquipoService
{
    Task<IEnumerable<EquipoDto>> GetAllAsync();
    Task<EquipoDto?> GetByCodigoAsync(string codigo);
    Task<EquipoDto> CreateAsync(EquipoDto equipo);
    Task<EquipoDto> UpdateAsync(EquipoDto equipo);
    Task<bool> DeleteAsync(string codigo);
    Task<PagedResult<EquipoDto>> SearchAsync(string filtro, int page, int pageSize);
    Task<IEnumerable<EquipoDto>> GetBySedeAsync(Sede sede);
    Task<IEnumerable<EquipoDto>> GetByEstadoAsync(EstadoEquipo estado);
}

public class EquipoService : IEquipoService
{
    private readonly GestLogDbContext _context;
    private readonly IGestLogLogger _logger;
    private readonly IMapper _mapper;

    public EquipoService(GestLogDbContext context, IGestLogLogger logger, IMapper mapper)
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<IEnumerable<EquipoDto>> GetAllAsync()
    {
        try
        {
            var equipos = await _context.Equipos
                .Include(e => e.Conexiones)
                .Include(e => e.SlotsRam)
                .Include(e => e.Discos)
                .Where(e => e.FechaBaja == null)
                .OrderBy(e => e.Codigo)
                .ToListAsync();

            return _mapper.Map<IEnumerable<EquipoDto>>(equipos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener equipos");
            throw new GestLogException("Error al cargar los equipos", "EQUIPOS_LOAD_ERROR", ex);
        }
    }

    public async Task<EquipoDto> CreateAsync(EquipoDto equipoDto)
    {
        try
        {
            // Validar código único
            var existe = await _context.Equipos
                .AnyAsync(e => e.Codigo == equipoDto.Codigo);
                
            if (existe)
                throw new BusinessException($"Ya existe un equipo con código '{equipoDto.Codigo}'");

            var entity = _mapper.Map<EquipoEntity>(equipoDto);
            entity.FechaCreacion = DateTime.UtcNow;

            _context.Equipos.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Equipo creado: {Codigo}", entity.Codigo);
            return _mapper.Map<EquipoDto>(entity);
        }
        catch (Exception ex) when (!(ex is BusinessException))
        {
            _logger.LogError(ex, "Error al crear equipo {Codigo}", equipoDto.Codigo);
            throw new GestLogException("Error al crear el equipo", "EQUIPO_CREATE_ERROR", ex);
        }
    }
}
```

### 3.2 ICronogramaEquipoService (Sistema de calendario semanal)

```csharp
public interface ICronogramaEquipoService
{
    Task<IEnumerable<CronogramaEquipoDto>> GetAllAsync();
    Task<CronogramaEquipoDto?> GetByHoraAsync(TimeSpan hora);
    Task<CronogramaEquipoDto> CreateAsync(CronogramaEquipoDto cronograma);
    Task<CronogramaEquipoDto> UpdateAsync(CronogramaEquipoDto cronograma);
    Task<bool> DeleteAsync(TimeSpan hora);
    Task<bool> AsignarEquipoAsync(TimeSpan hora, DayOfWeek diaSemana, string codigoEquipo, string actividad);
    Task<bool> LiberarEquipoAsync(TimeSpan hora, DayOfWeek diaSemana);
    Task<Dictionary<DayOfWeek, string?>> GetEquiposPorHoraAsync(TimeSpan hora);
    Task<Dictionary<TimeSpan, Dictionary<DayOfWeek, string?>>> GetCronogramaCompletoAsync();
    Task<byte[]> ExportarAExcelAsync();
    Task<List<HistorialCronogramaDto>> GetHistorialCambiosAsync(int ultimosDias = 30);
    Task RegistrarCambioHistorialAsync(TimeSpan hora, DayOfWeek diaSemana, string? equipoAnterior, string? equipoNuevo, string? actividadAnterior, string? actividadNueva, string motivo);
}

public class CronogramaEquipoService : ICronogramaEquipoService
{
    private readonly GestLogDbContext _context;
    private readonly IGestLogLogger _logger;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;

    public CronogramaEquipoService(GestLogDbContext context, IGestLogLogger logger, IMapper mapper, ICurrentUserService currentUserService)
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<CronogramaEquipoDto>> GetAllAsync()
    {
        try
        {
            var cronogramas = await _context.CronogramasEquipos
                .OrderBy(c => c.Hora)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CronogramaEquipoDto>>(cronogramas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cronogramas de equipos");
            throw new GestLogException("Error al cargar cronogramas", "CRONOGRAMA_LOAD_ERROR", ex);
        }
    }

    public async Task<bool> AsignarEquipoAsync(TimeSpan hora, DayOfWeek diaSemana, string codigoEquipo, string actividad)
    {
        try
        {
            var cronograma = await _context.CronogramasEquipos
                .FirstOrDefaultAsync(c => c.Hora == hora);

            if (cronograma == null)
            {
                cronograma = new CronogramaEquipoEntity
                {
                    Hora = hora,
                    FechaCreacion = DateTime.UtcNow
                };
                _context.CronogramasEquipos.Add(cronograma);
            }

            // Obtener valores anteriores para historial
            string? equipoAnterior = GetEquipoPorDia(cronograma, diaSemana);
            string? actividadAnterior = GetActividadPorDia(cronograma, diaSemana);

            // Asignar nuevo equipo y actividad
            SetEquipoPorDia(cronograma, diaSemana, codigoEquipo);
            SetActividadPorDia(cronograma, diaSemana, actividad);
            
            cronograma.FechaModificacion = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            // Registrar cambio en historial
            await RegistrarCambioHistorialAsync(hora, diaSemana, equipoAnterior, codigoEquipo, actividadAnterior, actividad, "Asignación de equipo");

            _logger.LogInformation("Equipo {Codigo} asignado a {Dia} {Hora} con actividad {Actividad}", 
                codigoEquipo, diaSemana, hora, actividad);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al asignar equipo {Codigo} a {Dia} {Hora}", codigoEquipo, diaSemana, hora);
            throw new GestLogException("Error al asignar equipo", "CRONOGRAMA_ASSIGN_ERROR", ex);
        }
    }

    public async Task<bool> LiberarEquipoAsync(TimeSpan hora, DayOfWeek diaSemana)
    {
        try
        {
            var cronograma = await _context.CronogramasEquipos
                .FirstOrDefaultAsync(c => c.Hora == hora);

            if (cronograma == null) return true;

            // Obtener valores anteriores para historial
            string? equipoAnterior = GetEquipoPorDia(cronograma, diaSemana);
            string? actividadAnterior = GetActividadPorDia(cronograma, diaSemana);

            // Liberar asignación
            SetEquipoPorDia(cronograma, diaSemana, null);
            SetActividadPorDia(cronograma, diaSemana, null);
            
            cronograma.FechaModificacion = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            // Registrar cambio en historial
            await RegistrarCambioHistorialAsync(hora, diaSemana, equipoAnterior, null, actividadAnterior, null, "Liberación de equipo");

            _logger.LogInformation("Equipo liberado de {Dia} {Hora}", diaSemana, hora);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al liberar equipo de {Dia} {Hora}", diaSemana, hora);
            throw new GestLogException("Error al liberar equipo", "CRONOGRAMA_RELEASE_ERROR", ex);
        }
    }

    public async Task<Dictionary<DayOfWeek, string?>> GetEquiposPorHoraAsync(TimeSpan hora)
    {
        try
        {
            var cronograma = await _context.CronogramasEquipos
                .FirstOrDefaultAsync(c => c.Hora == hora);

            var resultado = new Dictionary<DayOfWeek, string?>();
            
            if (cronograma != null)
            {
                resultado[DayOfWeek.Monday] = cronograma.Lunes;
                resultado[DayOfWeek.Tuesday] = cronograma.Martes;
                resultado[DayOfWeek.Wednesday] = cronograma.Miercoles;
                resultado[DayOfWeek.Thursday] = cronograma.Jueves;
                resultado[DayOfWeek.Friday] = cronograma.Viernes;
                resultado[DayOfWeek.Saturday] = cronograma.Sabado;
                resultado[DayOfWeek.Sunday] = cronograma.Domingo;
            }
            else
            {
                // Inicializar vacío
                foreach (DayOfWeek dia in Enum.GetValues<DayOfWeek>())
                {
                    resultado[dia] = null;
                }
            }

            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener equipos para hora {Hora}", hora);
            throw new GestLogException("Error al cargar equipos por hora", "CRONOGRAMA_GET_HOUR_ERROR", ex);
        }
    }

    public async Task<Dictionary<TimeSpan, Dictionary<DayOfWeek, string?>>> GetCronogramaCompletoAsync()
    {
        try
        {
            var cronogramas = await _context.CronogramasEquipos
                .OrderBy(c => c.Hora)
                .ToListAsync();

            var resultado = new Dictionary<TimeSpan, Dictionary<DayOfWeek, string?>>();

            // Generar horas de trabajo (8:00 AM - 6:00 PM)
            for (int hora = 8; hora <= 18; hora++)
            {
                var timeSpan = new TimeSpan(hora, 0, 0);
                var cronograma = cronogramas.FirstOrDefault(c => c.Hora == timeSpan);
                
                var equiposPorDia = new Dictionary<DayOfWeek, string?>();
                
                if (cronograma != null)
                {
                    equiposPorDia[DayOfWeek.Monday] = cronograma.Lunes;
                    equiposPorDia[DayOfWeek.Tuesday] = cronograma.Martes;
                    equiposPorDia[DayOfWeek.Wednesday] = cronograma.Miercoles;
                    equiposPorDia[DayOfWeek.Thursday] = cronograma.Jueves;
                    equiposPorDia[DayOfWeek.Friday] = cronograma.Viernes;
                    equiposPorDia[DayOfWeek.Saturday] = cronograma.Sabado;
                    equiposPorDia[DayOfWeek.Sunday] = cronograma.Domingo;
                }
                else
                {
                    foreach (DayOfWeek dia in Enum.GetValues<DayOfWeek>())
                    {
                        equiposPorDia[dia] = null;
                    }
                }
                
                resultado[timeSpan] = equiposPorDia;
            }

            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cronograma completo");
            throw new GestLogException("Error al cargar cronograma completo", "CRONOGRAMA_FULL_ERROR", ex);
        }
    }

    public async Task RegistrarCambioHistorialAsync(TimeSpan hora, DayOfWeek diaSemana, string? equipoAnterior, string? equipoNuevo, string? actividadAnterior, string? actividadNueva, string motivo)
    {
        try
        {
            var historial = new HistorialCronogramaEntity
            {
                Hora = hora,
                DiaSemana = diaSemana,
                CodigoEquipoAnterior = equipoAnterior,
                CodigoEquipoNuevo = equipoNuevo,
                ActividadAnterior = actividadAnterior,
                ActividadNueva = actividadNueva,
                UsuarioModificacion = _currentUserService.Current?.FullName ?? "Sistema",
                FechaCambio = DateTime.UtcNow,
                Motivo = motivo
            };

            _context.HistorialCronograma.Add(historial);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar cambio en historial");
            // No lanzar excepción para no afectar el flujo principal
        }
    }

    // Métodos auxiliares para manejar propiedades por día
    private string? GetEquipoPorDia(CronogramaEquipoEntity cronograma, DayOfWeek dia)
    {
        return dia switch
        {
            DayOfWeek.Monday => cronograma.Lunes,
            DayOfWeek.Tuesday => cronograma.Martes,
            DayOfWeek.Wednesday => cronograma.Miercoles,
            DayOfWeek.Thursday => cronograma.Jueves,
            DayOfWeek.Friday => cronograma.Viernes,
            DayOfWeek.Saturday => cronograma.Sabado,
            DayOfWeek.Sunday => cronograma.Domingo,
            _ => null
        };
    }

    private string? GetActividadPorDia(CronogramaEquipoEntity cronograma, DayOfWeek dia)
    {
        return dia switch
        {
            DayOfWeek.Monday => cronograma.LunesActividad,
            DayOfWeek.Tuesday => cronograma.MartesActividad,
            DayOfWeek.Wednesday => cronograma.MiercolesActividad,
            DayOfWeek.Thursday => cronograma.JuevesActividad,
            DayOfWeek.Friday => cronograma.ViernesActividad,
            DayOfWeek.Saturday => cronograma.SabadoActividad,
            DayOfWeek.Sunday => cronograma.DomingoActividad,
            _ => null
        };
    }

    private void SetEquipoPorDia(CronogramaEquipoEntity cronograma, DayOfWeek dia, string? valor)
    {
        switch (dia)
        {
            case DayOfWeek.Monday: cronograma.Lunes = valor; break;
            case DayOfWeek.Tuesday: cronograma.Martes = valor; break;
            case DayOfWeek.Wednesday: cronograma.Miercoles = valor; break;
            case DayOfWeek.Thursday: cronograma.Jueves = valor; break;
            case DayOfWeek.Friday: cronograma.Viernes = valor; break;
            case DayOfWeek.Saturday: cronograma.Sabado = valor; break;
            case DayOfWeek.Sunday: cronograma.Domingo = valor; break;
        }
    }

    private void SetActividadPorDia(CronogramaEquipoEntity cronograma, DayOfWeek dia, string? valor)
    {
        switch (dia)
        {
            case DayOfWeek.Monday: cronograma.LunesActividad = valor; break;
            case DayOfWeek.Tuesday: cronograma.MartesActividad = valor; break;
            case DayOfWeek.Wednesday: cronograma.MiercolesActividad = valor; break;
            case DayOfWeek.Thursday: cronograma.JuevesActividad = valor; break;
            case DayOfWeek.Friday: cronograma.ViernesActividad = valor; break;
            case DayOfWeek.Saturday: cronograma.SabadoActividad = valor; break;
            case DayOfWeek.Sunday: cronograma.DomingoActividad = valor; break;
        }
    }

    public async Task<byte[]> ExportarAExcelAsync()
    {
        try
        {
            var cronogramaCompleto = await GetCronogramaCompletoAsync();
            
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Cronograma de Equipos");
            
            // Encabezados
            ws.Cell(1, 1).Value = "Hora";
            ws.Cell(1, 2).Value = "Lunes";
            ws.Cell(1, 3).Value = "Martes";
            ws.Cell(1, 4).Value = "Miércoles";
            ws.Cell(1, 5).Value = "Jueves";
            ws.Cell(1, 6).Value = "Viernes";
            ws.Cell(1, 7).Value = "Sábado";
            ws.Cell(1, 8).Value = "Domingo";
            
            // Aplicar formato a encabezados
            var headerRange = ws.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            
            // Datos
            int row = 2;
            foreach (var horaData in cronogramaCompleto.OrderBy(h => h.Key))
            {
                ws.Cell(row, 1).Value = horaData.Key.ToString(@"hh\:mm");
                ws.Cell(row, 2).Value = horaData.Value[DayOfWeek.Monday] ?? "";
                ws.Cell(row, 3).Value = horaData.Value[DayOfWeek.Tuesday] ?? "";
                ws.Cell(row, 4).Value = horaData.Value[DayOfWeek.Wednesday] ?? "";
                ws.Cell(row, 5).Value = horaData.Value[DayOfWeek.Thursday] ?? "";
                ws.Cell(row, 6).Value = horaData.Value[DayOfWeek.Friday] ?? "";
                ws.Cell(row, 7).Value = horaData.Value[DayOfWeek.Saturday] ?? "";
                ws.Cell(row, 8).Value = horaData.Value[DayOfWeek.Sunday] ?? "";
                row++;
            }
            
            // Ajustar columnas
            ws.Columns().AdjustToContents();
            
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar cronograma a Excel");
            throw new GestLogException("Error al exportar cronograma", "EXPORT_CRONOGRAMA_ERROR", ex);
        }
    }
}
```

---

## 🎨 Fase 4: ViewModels y UI Cronograma (Vista Calendario Semanal) (3-4 días)

### 4.1 CronogramaEquiposViewModel (Vista de calendario diario)

```csharp
public partial class CronogramaEquiposViewModel : ObservableObject, IDisposable
{
    private readonly ICronogramaEquipoService _cronogramaService;
    private readonly IEquipoService _equipoService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IGestLogLogger _logger;
    private CurrentUserInfo _currentUser;

    [ObservableProperty] private ObservableCollection<CronogramaHoraDto> horasDisponibles = new();
    [ObservableProperty] private ObservableCollection<EquipoDto> equiposDisponibles = new();
    [ObservableProperty] private Dictionary<TimeSpan, Dictionary<DayOfWeek, string?>> cronogramaCompleto = new();
    [ObservableProperty] private TimeSpan? horaSeleccionada;
    [ObservableProperty] private DayOfWeek? diaSeleccionado;
    [ObservableProperty] private string? equipoSeleccionado;
    [ObservableProperty] private string actividad = string.Empty;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string statusMessage = string.Empty;

    // Propiedades de permisos
    public bool CanAsignarEquipo => _currentUser.HasPermission("GestionEquipos.AsignarCronograma");
    public bool CanLiberarEquipo => _currentUser.HasPermission("GestionEquipos.LiberarCronograma");
    public bool CanExportarCronograma => _currentUser.HasPermission("GestionEquipos.ExportarDatos");
    public bool CanVerHistorial => _currentUser.HasPermission("GestionEquipos.VerHistorial");

    // Lista de días de la semana para la UI
    public List<DiaSemanaCronogramaDto> DiasSemanales { get; } = new()
    {
        new(DayOfWeek.Monday, "Lunes"),
        new(DayOfWeek.Tuesday, "Martes"),
        new(DayOfWeek.Wednesday, "Miércoles"),
        new(DayOfWeek.Thursday, "Jueves"),
        new(DayOfWeek.Friday, "Viernes"),
        new(DayOfWeek.Saturday, "Sábado"),
        new(DayOfWeek.Sunday, "Domingo")
    };

    public CronogramaEquiposViewModel(
        ICronogramaEquipoService cronogramaService,
        IEquipoService equipoService,
        ICurrentUserService currentUserService,
        IGestLogLogger logger)
    {
        _cronogramaService = cronogramaService;
        _equipoService = equipoService;
        _currentUserService = currentUserService;
        _logger = logger;
        _currentUser = _currentUserService.Current ?? new CurrentUserInfo();

        RecalcularPermisos();
        _currentUserService.CurrentUserChanged += OnCurrentUserChanged;

        // Generar horas de trabajo (8:00 AM - 6:00 PM)
        GenerarHorasDisponibles();
        
        // Cargar datos iniciales
        _ = LoadDataAsync();
    }

    private void GenerarHorasDisponibles()
    {
        HorasDisponibles.Clear();
        for (int hora = 8; hora <= 18; hora++)
        {
            var timeSpan = new TimeSpan(hora, 0, 0);
            HorasDisponibles.Add(new CronogramaHoraDto
            {
                Hora = timeSpan,
                HoraTexto = timeSpan.ToString(@"hh\:mm"),
                HoraDisplay = $"{hora}:00"
            });
        }
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Cargando cronograma y equipos...";
            
            // Cargar equipos activos
            var equipos = await _equipoService.GetAllAsync();
            var equiposActivos = equipos.Where(e => e.Estado == EstadoEquipo.Activo).ToList();
            
            // Cargar cronograma completo
            var cronograma = await _cronogramaService.GetCronogramaCompletoAsync();
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                EquiposDisponibles.Clear();
                foreach (var equipo in equiposActivos)
                    EquiposDisponibles.Add(equipo);
                    
                CronogramaCompleto = cronograma;
                OnPropertyChanged(nameof(CronogramaCompleto));
            });
            
            StatusMessage = $"Cronograma cargado - {equiposActivos.Count} equipos disponibles";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar datos del cronograma");
            StatusMessage = "Error al cargar cronograma";
            await ShowErrorAsync("Error", "No se pudo cargar el cronograma");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAsignarEquipo))]
    private async Task AsignarEquipoAsync()
    {
        if (!HoraSeleccionada.HasValue || !DiaSeleccionado.HasValue || string.IsNullOrEmpty(EquipoSeleccionado))
        {
            await ShowWarningAsync("Validación", "Debe seleccionar hora, día y equipo para asignar");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Asignando equipo...";

            var exito = await _cronogramaService.AsignarEquipoAsync(
                HoraSeleccionada.Value, 
                DiaSeleccionado.Value, 
                EquipoSeleccionado, 
                Actividad);

            if (exito)
            {
                await LoadDataAsync();
                await ShowSuccessAsync("Éxito", 
                    $"Equipo {EquipoSeleccionado} asignado a {DiaSeleccionado} {HoraSeleccionada:hh\\:mm}");
                
                // Limpiar selección
                LimpiarSeleccion();
            }
            else
            {
                await ShowErrorAsync("Error", "No se pudo asignar el equipo");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al asignar equipo");
            await ShowErrorAsync("Error", "Error al asignar equipo");
        }
        finally
        {
            IsLoading = false;
            StatusMessage = string.Empty;
        }
    }

    [RelayCommand(CanExecute = nameof(CanLiberarEquipo))]
    private async Task LiberarEquipoAsync()
    {
        if (!HoraSeleccionada.HasValue || !DiaSeleccionado.HasValue)
        {
            await ShowWarningAsync("Validación", "Debe seleccionar hora y día para liberar");
            return;
        }

        try
        {
            var confirmacion = await ShowConfirmationAsync("Confirmar", 
                $"¿Está seguro de liberar el equipo asignado a {DiaSeleccionado} {HoraSeleccionada:hh\\:mm}?");
                
            if (!confirmacion) return;

            IsLoading = true;
            StatusMessage = "Liberando equipo...";

            var exito = await _cronogramaService.LiberarEquipoAsync(HoraSeleccionada.Value, DiaSeleccionado.Value);

            if (exito)
            {
                await LoadDataAsync();
                await ShowSuccessAsync("Éxito", "Equipo liberado correctamente");
                LimpiarSeleccion();
            }
            else
            {
                await ShowErrorAsync("Error", "No se pudo liberar el equipo");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al liberar equipo");
            await ShowErrorAsync("Error", "Error al liberar equipo");
        }
        finally
        {
            IsLoading = false;
            StatusMessage = string.Empty;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportarCronograma))]
    private async Task ExportarCronogramaAsync()
    {
        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                FileName = $"CRONOGRAMA_EQUIPOS_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                Title = "Exportar cronograma de equipos a Excel"
            };
            
            if (saveFileDialog.ShowDialog() != true) return;

            IsLoading = true;
            StatusMessage = "Exportando cronograma...";

            var excelData = await _cronogramaService.ExportarAExcelAsync();
            await File.WriteAllBytesAsync(saveFileDialog.FileName, excelData);
            
            StatusMessage = $"Exportación completada: {saveFileDialog.FileName}";
            await ShowSuccessAsync("Éxito", "Cronograma exportado correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar cronograma");
            await ShowErrorAsync("Error", "Error al exportar cronograma");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanVerHistorial))]
    private async Task VerHistorialAsync()
    {
        try
        {
            var serviceProvider = LoggingService.GetServiceProvider();
            var historialViewModel = serviceProvider.GetRequiredService<HistorialCronogramaViewModel>();
            
            var dialog = new HistorialCronogramaDialog(historialViewModel);
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al abrir historial");
            await ShowErrorAsync("Error", "Error al abrir historial de cambios");
        }
    }

    private void LimpiarSeleccion()
    {
        HoraSeleccionada = null;
        DiaSeleccionado = null;
        EquipoSeleccionado = null;
        Actividad = string.Empty;
    }

    public string? GetEquipoEnSlot(TimeSpan hora, DayOfWeek dia)
    {
        if (CronogramaCompleto.TryGetValue(hora, out var equiposPorDia))
        {
            return equiposPorDia.TryGetValue(dia, out var equipo) ? equipo : null;
        }
        return null;
    }

    public bool IsSlotOcupado(TimeSpan hora, DayOfWeek dia)
    {
        return !string.IsNullOrEmpty(GetEquipoEnSlot(hora, dia));
    }

    public string GetColorSlot(TimeSpan hora, DayOfWeek dia)
    {
        if (IsSlotOcupado(hora, dia))
        {
            return "#E8F5E8"; // Verde claro para ocupado
        }
        return "Transparent"; // Transparente para libre
    }

    private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
    {
        _currentUser = user ?? new CurrentUserInfo();
        RecalcularPermisos();
    }

    private void RecalcularPermisos()
    {
        OnPropertyChanged(nameof(CanAsignarEquipo));
        OnPropertyChanged(nameof(CanLiberarEquipo));
        OnPropertyChanged(nameof(CanExportarCronograma));
        OnPropertyChanged(nameof(CanVerHistorial));
        
        AsignarEquipoCommand.NotifyCanExecuteChanged();
        LiberarEquipoCommand.NotifyCanExecuteChanged();
        ExportarCronogramaCommand.NotifyCanExecuteChanged();
        VerHistorialCommand.NotifyCanExecuteChanged();
    }

    public void Dispose()
    {
        _currentUserService.CurrentUserChanged -= OnCurrentUserChanged;
    }
}

// DTOs auxiliares
public class CronogramaHoraDto
{
    public TimeSpan Hora { get; set; }
    public string HoraTexto { get; set; } = string.Empty;
    public string HoraDisplay { get; set; } = string.Empty;
}

public class DiaSemanaCronogramaDto
{
    public DayOfWeek Dia { get; set; }
    public string Nombre { get; set; } = string.Empty;
    
    public DiaSemanaCronogramaDto(DayOfWeek dia, string nombre)
    {
        Dia = dia;
        Nombre = nombre;
    }
}
```

### 4.3 CronogramaEquiposView.xaml (Vista del calendario)

```xaml
<UserControl x:Class="GestLog.Modules.GestionEquipos.Views.CronogramaEquiposView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <UserControl.Resources>
        <BooleanToVisibilityConverter x:Key="BoolToVisibilityConverter"/>
        <BooleanToOpacityConverter x:Key="BooleanToOpacityConverter"/>
        
        <!-- Estilo para cards de semana -->
        <Style x:Key="SemanaCardStyle" TargetType="Border">
            <Setter Property="CornerRadius" Value="8"/>
            <Setter Property="Margin" Value="5"/>
            <Setter Property="Padding" Value="10"/>
            <Setter Property="MinHeight" Value="80"/>
            <Setter Property="MinWidth" Value="120"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Effect">
                <Setter.Value>
                    <DropShadowEffect Color="#504F4E" BlurRadius="3" Direction="315" ShadowDepth="2" Opacity="0.3"/>
                </Setter.Value>
            </Setter>
            <Style.Triggers>
                <Trigger Property="IsMouseOver" Value="True">
                    <Setter Property="Effect">
                        <Setter.Value>
                            <DropShadowEffect Color="#118938" BlurRadius="5" Direction="315" ShadowDepth="3" Opacity="0.6"/>
                        </Setter.Value>
                    </Setter>
                </Trigger>
            </Style.Triggers>
        </Style>
    </UserControl.Resources>
    
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Título -->
        <TextBlock Grid.Row="0" Text="Cronograma de Mantenimiento - Equipos" 
                   FontSize="24" FontWeight="Bold" 
                   Foreground="#118938" Margin="0,0,0,20"/>
        
        <!-- Filtros y Controles -->
        <Grid Grid.Row="1" Margin="0,0,0,15">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            
            <!-- Selector de año -->
            <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
                <TextBlock Text="Año:" VerticalAlignment="Center" Margin="0,0,10,0" FontWeight="SemiBold"/>
                <ComboBox Width="80" 
                          ItemsSource="{Binding AniosDisponibles}"
                          SelectedItem="{Binding AnioSeleccionado}"
                          VerticalAlignment="Center"/>
            </StackPanel>
            
            <!-- Botones de acción -->
            <StackPanel Grid.Column="2" Orientation="Horizontal" HorizontalAlignment="Right">
                <Button Content="➕ Nuevo Cronograma" 
                        Command="{Binding CrearCronogramaCommand}"
                        IsEnabled="{Binding CanCrearCronograma}"
                        Opacity="{Binding CanCrearCronograma, Converter={StaticResource BooleanToOpacityConverter}}"
                        Style="{StaticResource PrimaryButtonStyle}"
                        Margin="0,0,10,0"/>
                
                <Button Content="🔄 Actualizar" 
                        Command="{Binding LoadCronogramasCommand}"
                        Style="{StaticResource SecondaryButtonStyle}"
                        Margin="0,0,10,0"/>
                
                <Button Content="📄 Exportar Excel" 
                        Command="{Binding ExportarCronogramasCommand}"
                        IsEnabled="{Binding CanExportarCronograma}"
                        Opacity="{Binding CanExportarCronograma, Converter={StaticResource BooleanToOpacityConverter}}"
                        Style="{StaticResource SecondaryButtonStyle}"/>
            </StackPanel>
        </Grid>
        
        <!-- Leyenda de colores -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="0,0,0,15">
            <TextBlock Text="Leyenda: " FontWeight="SemiBold" VerticalAlignment="Center" Margin="0,0,15,0"/>
            
            <Border Background="#388E3C" Width="15" Height="15" CornerRadius="3" Margin="0,0,5,0"/>
            <TextBlock Text="Realizado" VerticalAlignment="Center" Margin="0,0,15,0"/>
            
            <Border Background="#FFB300" Width="15" Height="15" CornerRadius="3" Margin="0,0,5,0"/>
            <TextBlock Text="Atrasado/Parcial" VerticalAlignment="Center" Margin="0,0,15,0"/>
            
            <Border Background="#C80000" Width="15" Height="15" CornerRadius="3" Margin="0,0,5,0"/>
            <TextBlock Text="No Realizado" VerticalAlignment="Center" Margin="0,0,15,0"/>
            
            <Border Background="Transparent" BorderBrush="#BDBDBD" BorderThickness="1" Width="15" Height="15" CornerRadius="3" Margin="0,0,5,0"/>
            <TextBlock Text="Sin Programar" VerticalAlignment="Center"/>
        </StackPanel>
        
        <!-- Grid de semanas (52 semanas en 13 filas x 4 columnas) -->
        <ScrollViewer Grid.Row="3" VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled">
            <ItemsControl ItemsSource="{Binding Semanas}">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate>
                        <UniformGrid Columns="4" Rows="13"/>
                    </ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
                
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <Border Style="{StaticResource SemanaCardStyle}">
                            <Border.Background>
                                <SolidColorBrush Color="{Binding ColorSemana}"/>
                            </Border.Background>
                            
                            <Border.InputBindings>
                                <MouseBinding MouseAction="LeftClick" Command="{Binding VerSemanaCommand}"/>
                            </Border.InputBindings>
                            
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="Auto"/>
                                    <RowDefinition Height="Auto"/>
                                    <RowDefinition Height="*"/>
                                    <RowDefinition Height="Auto"/>
                                </Grid.RowDefinitions>
                                
                                <!-- Número de semana -->
                                <TextBlock Grid.Row="0" 
                                           Text="{Binding NumeroSemana, StringFormat='S{0}'}" 
                                           FontWeight="Bold" 
                                           FontSize="14"
                                           Foreground="White"
                                           HorizontalAlignment="Center"/>
                                
                                <!-- Rango de fechas -->
                                <TextBlock Grid.Row="1" 
                                           Text="{Binding RangoFechas}" 
                                           FontSize="10"
                                           Foreground="White"
                                           HorizontalAlignment="Center"
                                           TextWrapping="Wrap"/>
                                
                                <!-- Número de cronogramas -->
                                <TextBlock Grid.Row="3" 
                                           Text="{Binding Cronogramas.Count, StringFormat='{0} equipos'}" 
                                           FontSize="9"
                                           Foreground="White"
                                           HorizontalAlignment="Center"
                                           Visibility="{Binding TieneCronogramas, Converter={StaticResource BoolToVisibilityConverter}}"/>
                                
                                <!-- Indicador de semana actual -->
                                <Ellipse Grid.Row="2" 
                                         Width="8" Height="8" 
                                         Fill="Yellow" 
                                         HorizontalAlignment="Right" 
                                         VerticalAlignment="Top"
                                         Margin="0,2,2,0"
                                         Visibility="{Binding IsSemanaActual, Converter={StaticResource BoolToVisibilityConverter}}"/>
                            </Grid>
                            
                            <!-- Tooltip con información detallada -->
                            <Border.ToolTip>
                                <StackPanel>
                                    <TextBlock Text="{Binding TituloSemana}" FontWeight="Bold"/>
                                    <TextBlock Text="{Binding Cronogramas.Count, StringFormat='Equipos programados: {0}'}"/>
                                    <TextBlock Text="{Binding EstadosMantenimientos.Count, StringFormat='Estados de mantenimiento: {0}'}"/>
                                    <TextBlock Text="Click para ver detalles" FontStyle="Italic"/>
                                </StackPanel>
                            </Border.ToolTip>
                        </Border>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </ScrollViewer>
        
        <!-- Barra de estado -->
        <Grid Grid.Row="4" Margin="0,15,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            
            <TextBlock Grid.Column="0" Text="{Binding StatusMessage}" 
                       VerticalAlignment="Center" FontStyle="Italic"/>
            
            <ProgressBar Grid.Column="1" Width="200" Height="20"
                         IsIndeterminate="{Binding IsLoading}"
                         Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibilityConverter}}"/>
        </Grid>
    </Grid>
</UserControl>
```

### 4.1 EquiposViewModel

```csharp
public partial class EquiposViewModel : ObservableObject, IDisposable
{
    private readonly IEquipoService _equipoService;
    private readonly IExportacionService _exportacionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IGestLogLogger _logger;
    private CurrentUserInfo _currentUser;

    [ObservableProperty] private ObservableCollection<EquipoDto> equipos = new();
    [ObservableProperty] private EquipoDto? equipoSeleccionado;
    [ObservableProperty] private string filtroTexto = string.Empty;
    [ObservableProperty] private EstadoEquipo? filtroEstado;
    [ObservableProperty] private Sede? filtroSede;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private ICollectionView? equiposView;

    // Propiedades de permisos
    public bool CanCrearEquipo => _currentUser.HasPermission("GestionEquipos.CrearEquipo");
    public bool CanEditarEquipo => _currentUser.HasPermission("GestionEquipos.EditarEquipo");
    public bool CanEliminarEquipo => _currentUser.HasPermission("GestionEquipos.EliminarEquipo");
    public bool CanExportarDatos => _currentUser.HasPermission("GestionEquipos.ExportarDatos");

    public EquiposViewModel(
        IEquipoService equipoService,
        IExportacionService exportacionService,
        ICurrentUserService currentUserService,
        IGestLogLogger logger)
    {
        _equipoService = equipoService;
        _exportacionService = exportacionService;
        _currentUserService = currentUserService;
        _logger = logger;
        _currentUser = _currentUserService.Current ?? new CurrentUserInfo();
        
        // Configurar filtros
        ConfigurarFiltros();
        
        // Cargar datos iniciales
        _ = LoadEquiposAsync();
    }

    [RelayCommand]
    private async Task LoadEquiposAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Cargando equipos...";
            
            var equiposData = await _equipoService.GetAllAsync();
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                Equipos.Clear();
                foreach (var equipo in equiposData)
                    Equipos.Add(equipo);
                    
                ActualizarVista();
            });
            
            StatusMessage = $"Se cargaron {Equipos.Count} equipos";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar equipos");
            StatusMessage = "Error al cargar equipos";
            await ShowErrorAsync("Error", "No se pudieron cargar los equipos");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCrearEquipo))]
    private async Task CrearEquipoAsync()
    {
        try
        {
            var nuevoEquipo = new EquipoDto
            {
                Codigo = string.Empty,
                Estado = EstadoEquipo.Activo,
                Sede = Sede.Bogota,
                FechaCreacion = DateTime.Now
            };
            
            if (await ShowEquipoDialogAsync(nuevoEquipo, isEditing: false))
            {
                await _equipoService.CreateAsync(nuevoEquipo);
                await LoadEquiposAsync();
                
                _logger.LogInformation("Equipo creado: {Codigo}", nuevoEquipo.Codigo);
                await ShowSuccessAsync("Éxito", "Equipo creado correctamente");
            }
        }
        catch (BusinessException ex)
        {
            await ShowWarningAsync("Validación", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear equipo");
            await ShowErrorAsync("Error", "No se pudo crear el equipo");
        }
    }

    [RelayCommand(CanExecute = nameof(CanEditarEquipo))]
    private async Task EditarEquipoAsync()
    {
        if (EquipoSeleccionado == null) return;
        
        try
        {
            var equipoEdicion = _mapper.Map<EquipoDto>(EquipoSeleccionado);
            
            if (await ShowEquipoDialogAsync(equipoEdicion, isEditing: true))
            {
                await _equipoService.UpdateAsync(equipoEdicion);
                await LoadEquiposAsync();
                
                _logger.LogInformation("Equipo actualizado: {Codigo}", equipoEdicion.Codigo);
                await ShowSuccessAsync("Éxito", "Equipo actualizado correctamente");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar equipo");
            await ShowErrorAsync("Error", "No se pudo actualizar el equipo");
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportarDatos))]
    private async Task ExportarEquiposAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Generando archivo Excel...";
            
            var excelData = await _exportacionService.ExportarEquiposAExcelAsync();
            
            var dialog = new SaveFileDialog
            {
                Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                FileName = $"Equipos_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };
            
            if (dialog.ShowDialog() == true)
            {
                await File.WriteAllBytesAsync(dialog.FileName, excelData);
                await ShowSuccessAsync("Éxito", "Equipos exportados correctamente");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar equipos");
            await ShowErrorAsync("Error", "No se pudo exportar los equipos");
        }
        finally
        {
            IsLoading = false;
            StatusMessage = string.Empty;
        }
    }

    private void ConfigurarFiltros()
    {
        EquiposView = CollectionViewSource.GetDefaultView(Equipos);
        EquiposView.Filter = FiltrarEquipos;
        
        // Reactivar filtros cuando cambien las propiedades
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(FiltroTexto) || 
                e.PropertyName == nameof(FiltroEstado) || 
                e.PropertyName == nameof(FiltroSede))
            {
                EquiposView?.Refresh();
            }
        };
    }

    private bool FiltrarEquipos(object item)
    {
        if (item is not EquipoDto equipo) return false;
        
        // Filtro por texto
        if (!string.IsNullOrWhiteSpace(FiltroTexto))
        {
            var filtroLower = FiltroTexto.ToLower();
            if (!equipo.Codigo?.ToLower().Contains(filtroLower) == true &&
                !equipo.NombreEquipo?.ToLower().Contains(filtroLower) == true &&
                !equipo.UsuarioAsignado?.ToLower().Contains(filtroLower) == true)
                return false;
        }
        
        // Filtro por estado
        if (FiltroEstado.HasValue && equipo.Estado != FiltroEstado.Value)
            return false;
            
        // Filtro por sede
        if (FiltroSede.HasValue && equipo.Sede != FiltroSede.Value)
            return false;
            
        return true;
    }

    public void Dispose()
    {
        _currentUserService.CurrentUserChanged -= OnCurrentUserChanged;
    }
}
```

### 4.2 EquiposView.xaml

```xaml
<UserControl x:Class="GestLog.Modules.GestionEquipos.Views.EquiposView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <UserControl.Resources>
        <BooleanToVisibilityConverter x:Key="BoolToVisibilityConverter"/>
        <BooleanToOpacityConverter x:Key="BooleanToOpacityConverter"/>
    </UserControl.Resources>
    
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Título -->
        <TextBlock Grid.Row="0" Text="Gestión de Equipos" 
                   FontSize="24" FontWeight="Bold" 
                   Foreground="#118938" Margin="0,0,0,20"/>
        
        <!-- Filtros y Acciones -->
        <Grid Grid.Row="1" Margin="0,0,0,15">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            
            <!-- Panel de Filtros -->
            <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
                <!-- Filtro por texto -->
                <TextBox x:Name="txtFiltro" Width="200" Margin="0,0,10,0"
                         Text="{Binding FiltroTexto, UpdateSourceTrigger=PropertyChanged}"
                         Tag="Buscar por código, nombre o usuario..."/>
                
                <!-- Filtro por estado -->
                <ComboBox Width="120" Margin="0,0,10,0"
                          ItemsSource="{Binding EstadosDisponibles}"
                          SelectedItem="{Binding FiltroEstado}"
                          DisplayMemberPath="Value" SelectedValuePath="Key"/>
                
                <!-- Filtro por sede -->
                <ComboBox Width="120" Margin="0,0,10,0"
                          ItemsSource="{Binding SedesDisponibles}"
                          SelectedItem="{Binding FiltroSede}"
                          DisplayMemberPath="Value" SelectedValuePath="Key"/>
            </StackPanel>
            
            <!-- Botones de Acción -->
            <StackPanel Grid.Column="1" Orientation="Horizontal" HorizontalAlignment="Right">
                <Button Content="➕ Nuevo Equipo" 
                        Command="{Binding CrearEquipoCommand}"
                        IsEnabled="{Binding CanCrearEquipo}"
                        Opacity="{Binding CanCrearEquipo, Converter={StaticResource BooleanToOpacityConverter}}"
                        Style="{StaticResource PrimaryButtonStyle}"
                        Margin="0,0,10,0"/>
                
                <Button Content="📄 Exportar Excel" 
                        Command="{Binding ExportarEquiposCommand}"
                        IsEnabled="{Binding CanExportarDatos}"
                        Opacity="{Binding CanExportarDatos, Converter={StaticResource BooleanToOpacityConverter}}"
                        Style="{StaticResource SecondaryButtonStyle}"/>
            </StackPanel>
        </Grid>
        
        <!-- Grilla de Equipos -->
        <DataGrid Grid.Row="2" 
                  ItemsSource="{Binding EquiposView}"
                  SelectedItem="{Binding EquipoSeleccionado}"
                  AutoGenerateColumns="False"
                  CanUserAddRows="False"
                  CanUserDeleteRows="False"
                  IsReadOnly="True"
                  GridLinesVisibility="Horizontal"
                  RowHeaderWidth="0"
                  Style="{StaticResource ModernDataGridStyle}">
            
            <DataGrid.Columns>
                <DataGridTextColumn Header="Código" Binding="{Binding Codigo}" Width="100"/>
                <DataGridTextColumn Header="Nombre" Binding="{Binding NombreEquipo}" Width="*"/>
                <DataGridTextColumn Header="Usuario" Binding="{Binding UsuarioAsignado}" Width="150"/>
                <DataGridTextColumn Header="Marca" Binding="{Binding Marca}" Width="100"/>
                <DataGridTextColumn Header="Modelo" Binding="{Binding Modelo}" Width="120"/>
                <DataGridTextColumn Header="Estado" Binding="{Binding Estado}" Width="100"/>
                <DataGridTextColumn Header="Sede" Binding="{Binding Sede}" Width="100"/>
                <DataGridTextColumn Header="Fecha Compra" Binding="{Binding FechaCompra, StringFormat=dd/MM/yyyy}" Width="100"/>
                
                <!-- Acciones -->
                <DataGridTemplateColumn Header="Acciones" Width="120">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <Button Content="✏️" ToolTip="Editar"
                                        Command="{Binding DataContext.EditarEquipoCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        IsEnabled="{Binding DataContext.CanEditarEquipo, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        Style="{StaticResource IconButtonStyle}"
                                        Margin="2"/>
                                
                                <Button Content="🗑️" ToolTip="Eliminar"
                                        Command="{Binding DataContext.EliminarEquipoCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}"
                                        IsEnabled="{Binding DataContext.CanEliminarEquipo, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        Style="{StaticResource DangerIconButtonStyle}"
                                        Margin="2"/>
                            </StackPanel>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>
        
        <!-- Barra de Estado -->
        <Grid Grid.Row="3" Margin="0,15,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            
            <TextBlock Grid.Column="0" Text="{Binding StatusMessage}" 
                       VerticalAlignment="Center" FontStyle="Italic"/>
            
            <ProgressBar Grid.Column="1" Width="200" Height="20"
                         IsIndeterminate="{Binding IsLoading}"
                         Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibilityConverter}}"/>
        </Grid>
    </Grid>
</UserControl>
```

---

## 🔐 Fase 5: Sistema de Permisos (0.5 días)

### 5.1 Permisos Requeridos

```csharp
// Permisos a definir en base de datos
public static class GestionEquiposPermisos
{
    public const string AccederModulo = "GestionEquipos.AccederModulo";
    public const string CrearEquipo = "GestionEquipos.CrearEquipo";
    public const string EditarEquipo = "GestionEquipos.EditarEquipo";
    public const string EliminarEquipo = "GestionEquipos.EliminarEquipo";
    public const string GestionarConexiones = "GestionEquipos.GestionarConexiones";
    public const string GestionarPerifericos = "GestionEquipos.GestionarPerifericos";
    public const string ExportarDatos = "GestionEquipos.ExportarDatos";
    public const string ProgramarMantenimiento = "GestionEquipos.ProgramarMantenimiento";
}
```

### 5.2 Validación en ViewModels

```csharp
// En cada ViewModel
private void RecalcularPermisos()
{
    OnPropertyChanged(nameof(CanCrearEquipo));
    OnPropertyChanged(nameof(CanEditarEquipo));
    OnPropertyChanged(nameof(CanEliminarEquipo));
    OnPropertyChanged(nameof(CanExportarDatos));
    
    // Notificar CanExecute de comandos
    CrearEquipoCommand.NotifyCanExecuteChanged();
    EditarEquipoCommand.NotifyCanExecuteChanged();
    ExportarEquiposCommand.NotifyCanExecuteChanged();
}
```

---

## 🔧 Fase 6: Integración con GestLog (0.5 días)

### 6.1 Registro en DI

```csharp
// En Startup.GestionEquipos.cs
public static class StartupGestionEquipos
{
    public static IServiceCollection AddGestionEquipos(this IServiceCollection services)
    {
        // Servicios
        services.AddScoped<IEquipoService, EquipoService>();
        services.AddScoped<IConexionService, ConexionService>();
        services.AddScoped<IPerifericoService, PerifericoService>();
        services.AddScoped<IExportacionService, ExportacionService>();
        
        // ViewModels
        services.AddTransient<EquiposViewModel>();
        services.AddTransient<ConexionesViewModel>();
        services.AddTransient<PerifericosViewModel>();
        services.AddTransient<GestionEquiposHomeViewModel>();
        
        // AutoMapper profiles
        services.AddAutoMapper(typeof(GestionEquiposProfile));
        
        return services;
    }
}
```

### 6.2 Navegación desde Menú

```csharp
// En HerramientasViewModel
[RelayCommand]
private async Task AbrirGestionEquipos()
{
    if (!_currentUser.HasPermission(GestionEquiposPermisos.AccederModulo))
    {
        await ShowWarningAsync("Acceso Denegado", "No tiene permisos para acceder a Gestión de Equipos");
        return;
    }
    
    try
    {
        var serviceProvider = LoggingService.GetServiceProvider();
        var viewModel = serviceProvider.GetRequiredService<GestionEquiposHomeViewModel>();
        var view = new GestionEquiposHomeView { DataContext = viewModel };
        
        var mainWindow = Application.Current.MainWindow as MainWindow;
        mainWindow?.NavigateToView(view, "Gestión de Equipos");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al abrir Gestión de Equipos");
        await ShowErrorAsync("Error", "No se pudo abrir el módulo");
    }
}
```

---

## 📊 Cronograma de Implementación

| Fase | Duración | Tareas Principales |
|------|----------|-------------------|
| **Fase 1** | 1 día | Configuración módulo, entidades, enums |
| **Fase 2** | 1-2 días | Migraciones BD, normalización datos |
| **Fase 3** | 2 días | Servicios, lógica negocio, validaciones |
| **Fase 4** | 2-3 días | ViewModels, UI, filtros, exportación |
| **Fase 5** | 0.5 días | Sistema de permisos |
| **Fase 6** | 0.5 días | Integración con GestLog |
| **Testing** | 1 día | Pruebas integración, validación |

**Total: 6-8 días laborales**

---

## ✅ Criterios de Aceptación

1. ✅ **Migración 100% exitosa** de datos desde DBMttoPc
2. ✅ **Funcionalidad completa** - CRUD equipos, conexiones, periféricos
3. ✅ **UI moderna** consistente con GestLog (tema verde, efectos)
4. ✅ **Sistema de permisos** totalmente integrado
5. ✅ **Exportación Excel** con formato mejorado
6. ✅ **Performance** - Consultas < 2 segundos
7. ✅ **Logging completo** con IGestLogLogger
8. ✅ **Validaciones robustas** y manejo de errores

---

## 🚀 Próximos Pasos

1. **Aprobar el plan** y recursos necesarios
2. **Crear estructura** del módulo (Fase 1)
3. **Configurar migraciones** EF Core (Fase 2)
4. **Implementar servicios** core (Fase 3)
5. **Desarrollar UI** moderna (Fase 4)
6. **Testing exhaustivo** y optimización

¿Procedemos con la implementación?
