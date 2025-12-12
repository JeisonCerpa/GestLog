using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento;
using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Utilities; // Utilidad compartida de fechas de semana

namespace GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento;

public partial class RegistroMantenimientoEquipoDialog : Window
{

    public SeguimientoMantenimientoDto? Resultado { get; private set; }
    public RegistroMantenimientoEquipoDialog()
    {
        InitializeComponent();
    }

    public void CargarDesde(SeguimientoMantenimientoDto dto)
    {
        if (DataContext is RegistroMantenimientoEquipoViewModel vm)
        {
            vm.LoadFrom(dto);
        }
    }

    private void Guardar_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not RegistroMantenimientoEquipoViewModel vm)
        {
            DialogResult = false;
            return;
        }

        var errores = new List<string>();
        if (vm.TipoMtno == null)
            errores.Add("Debe seleccionar el tipo de mantenimiento.");
        if (string.IsNullOrWhiteSpace(vm.Descripcion))
            errores.Add("La descripción es obligatoria.");
        if (string.IsNullOrWhiteSpace(vm.Responsable))
            errores.Add("El responsable es obligatorio.");
        if (errores.Count > 0)
        {
            System.Windows.MessageBox.Show(string.Join("\n", errores), "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dto = vm.ToDto();
        DateTime inicioSemana = DateTimeWeekHelper.FirstDateOfWeekISO8601(dto.Anio, dto.Semana);
        DateTime finSemana = inicioSemana.AddDays(6);
        if (dto.FechaRealizacion.HasValue)
        {
            if (dto.FechaRealizacion.Value >= inicioSemana && dto.FechaRealizacion.Value <= finSemana)
                dto.Estado = EstadoSeguimientoMantenimiento.RealizadoEnTiempo;
            else if (dto.FechaRealizacion.Value > finSemana)
                dto.Estado = EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo;
            else
                dto.Estado = EstadoSeguimientoMantenimiento.NoRealizado;
        }
        Resultado = dto;
        DialogResult = true;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        Resultado = null;
        DialogResult = false;
    }
}


