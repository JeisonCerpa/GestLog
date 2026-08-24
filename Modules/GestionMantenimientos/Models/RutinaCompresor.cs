using System;
using System.Collections.Generic;

namespace GestLog.Modules.GestionMantenimientos.Models
{
    /// <summary>
    /// Escalera de mantenimiento por horómetro (la del manual de compresores de tornillo AXP):
    /// se atiende cada Equipo.HorasPorServicio horas (o 1 año, lo que ocurra primero) y la rutina
    /// depende del número de servicio: cada 3 toca B, cada 6 C, cada 10 D, el resto A.
    /// El servicio 0 es el INICIAL de puesta en marcha.
    /// </summary>
    /// <param name="Toca">Si en esta lectura ya corresponde ejecutar la rutina.</param>
    /// <param name="HorasRestantes">Horas que faltan para el próximo servicio (0 si ya toca).</param>
    public record EstadoRutina(bool Toca, string Letra, string Descripcion, int HorasRestantes, int NumeroServicio, int? UltimaLectura, DateTime? FechaUltimaLectura);

    public static class RutinaCompresor
    {
        // ponytail: la escalera del manual AXP (B cada 3 servicios, C cada 6, D cada 10) se aplica a
        // todo equipo con horómetro; solo el intervalo en horas es propio de cada uno
        // (Equipo.HorasPorServicio). Si aparece un modelo con otra progresión, aquí va la excepción.
        // El orden importa: gana la rutina más completa cuyo período divida el número de servicio.
        private static readonly (string Letra, int CadaCuantosServicios)[] Escalera =
        {
            ("D", 10), ("C", 6), ("B", 3), ("A", 1)
        };

        public static readonly IReadOnlyDictionary<string, string> Tareas = new Dictionary<string, string>
        {
            ["INICIAL"] = "Cambio de aceite; Cambio filtro de aceite; Ajuste conexiones eléctricas; Purga tanque pulmón; Inspección y limpieza del equipo",
            ["A"] = "Cambio filtro de aceite; Cambio filtro de aire; Cambio cartucho separador; Cambio aceite; Purga tanque pulmón; Limpieza radiador; Inspección y limpieza del equipo",
            ["B"] = "Cambio filtro de aceite; Cambio filtro de aire; Cambio cartucho separador; Cambio aceite; Cambio correa transmisión; Mantenimiento válvula admisión; Revisión kit recuperador de aceite; Revisión válvula de seguridad; Revisión válvula cheque; Purga tanque pulmón; Limpieza radiador; Inspección y limpieza del equipo",
            ["C"] = "Cambio filtro de aceite; Cambio filtro de aire; Cambio cartucho separador; Cambio aceite; Cambio kit mtto válvula presión mínima; Cambio sensor de temperatura (termostato); Cambio válvula solenoide; Kit mtto válvula cheque/retención; Inspección válvula de seguridad; Purga tanque pulmón; Limpieza radiador; Inspección y limpieza del equipo",
            ["D"] = "Cambio filtro de aceite; Cambio filtro de aire; Cambio cartucho separador; Cambio aceite; Cambio correa transmisión o coupling; Cambio kit mtto válvula presión mínima; Cambio kit juntas bloque filtro de aceite y separador; Cambio sensor de temperatura (termostato); Cambio válvula solenoide; Cambio kit juntas eje unidad compresora; Inspección válvula de seguridad; Purga tanque pulmón; Limpieza radiador; Inspección y limpieza del equipo"
        };

        /// <summary>
        /// Rutina que corresponde al n-ésimo servicio (1 = el primero después del INICIAL).
        /// Se cuenta por servicios hechos, no por horómetro: una rutina disparada por calendario
        /// antes de las 4.000 h igual avanza la escalera.
        /// </summary>
        public static string LetraPara(int numeroServicio)
        {
            if (numeroServicio <= 0) return "INICIAL";
            foreach (var (letra, cada) in Escalera)
                if (numeroServicio % cada == 0)
                    return letra;
            return "A";
        }

        /// <summary>
        /// Servicio al que corresponde una lectura: se cuenta sobre el horómetro, no sobre el
        /// historial, para que un equipo que ya venía rodado no empiece la escalera desde cero.
        /// </summary>
        public static int NumeroServicio(int horometro, int horasPorServicio)
            => horasPorServicio <= 0 ? 0 : horometro / horasPorServicio;

        /// <summary>
        /// En la visita trimestral: ¿toca servicio? Dispara lo que ocurra primero, horas o 12 meses.
        /// </summary>
        public static bool TocaServicio(int horometroActual, int horometroUltimoServicio, DateTime fechaUltimoServicio, DateTime hoy, int horasPorServicio)
            => horometroActual - horometroUltimoServicio >= horasPorServicio
               || hoy >= fechaUltimoServicio.AddMonths(12);

        /// <summary>Tareas de la rutina que toca, listas para prellenar la descripción del seguimiento.</summary>
        public static string DescripcionPara(int numeroServicio)
        {
            var letra = LetraPara(numeroServicio);
            return $"RUTINA {letra} — {Tareas[letra]}";
        }

        /// <summary>Check de la escalera. Ejecutar tras tocar Escalera o los umbrales.</summary>
        public static void SelfCheck()
        {
            var casos = new (int Servicio, string Esperado)[]
            {
                (0, "INICIAL"), (1, "A"), (2, "A"), (3, "B"), (4, "A"), (5, "A"),
                (6, "C"), (9, "B"), (10, "D"), (12, "C"), (15, "B"), (20, "D"), (30, "D")
            };
            foreach (var (servicio, esperado) in casos)
            {
                var obtenido = LetraPara(servicio);
                if (obtenido != esperado)
                    throw new InvalidOperationException($"RutinaCompresor: servicio {servicio} dio '{obtenido}', se esperaba '{esperado}'");
            }
        }
    }
}
