using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using System;
using System.Collections.Generic;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels;

/// <summary>
/// ViewModel ligero para el registro de mantenimiento de un equipo desde el módulo de Gestión de Equipos.
/// Evita dependencia directa con el ViewModel de Seguimiento de Gestión de Mantenimientos.
/// </summary>
public partial class RegistroMantenimientoEquipoViewModel : ObservableObject
{
    [ObservableProperty]
    private string? codigo;
    [ObservableProperty]
    private string? nombre;
    [ObservableProperty]
    private TipoMantenimiento? tipoMtno = TipoMantenimiento.Preventivo;
    [ObservableProperty]
    private string? descripcion;
    [ObservableProperty]
    private string? responsable;
    [ObservableProperty]
    private decimal? costo;
    [ObservableProperty]
    private string? observaciones;
    [ObservableProperty]
    private DateTime? fechaRealizacion = DateTime.Now;
    [ObservableProperty]
    private int semana;
    [ObservableProperty]
    private int anio;
    [ObservableProperty]
    private bool isCorrectivo;

    public IEnumerable<TipoMantenimiento> AllowedTipos { get; } = new[] { TipoMantenimiento.Preventivo, TipoMantenimiento.Correctivo };

    public void LoadFrom(SeguimientoMantenimientoDto dto)
    {
        Codigo = dto.Codigo;
        Nombre = dto.Nombre;
        TipoMtno = dto.TipoMtno;
        Descripcion = dto.Descripcion;
        Responsable = dto.Responsable;
        Costo = dto.Costo;
        Observaciones = dto.Observaciones;
        FechaRealizacion = dto.FechaRealizacion ?? DateTime.Now;
        Semana = dto.Semana;
        Anio = dto.Anio;
    }

    public SeguimientoMantenimientoDto ToDto()
    {
        return new SeguimientoMantenimientoDto
        {
            Codigo = Codigo,
            Nombre = Nombre,
            TipoMtno = TipoMtno,
            Descripcion = Descripcion,
            Responsable = Responsable,
            Costo = Costo,
            Observaciones = Observaciones,
            FechaRealizacion = FechaRealizacion,
            Semana = Semana,
            Anio = Anio,
            Estado = EstadoSeguimientoMantenimiento.Pendiente
        };
    }

    [RelayCommand]
    private void MarcarCorrectivo()
    {
        TipoMtno = TipoMantenimiento.Correctivo;
    }

    [RelayCommand]
    private void MarcarPreventivo()
    {
        TipoMtno = TipoMantenimiento.Preventivo;
    }
}
