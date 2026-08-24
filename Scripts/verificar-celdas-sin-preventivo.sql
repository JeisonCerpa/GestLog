-- Invariante: toda semana programada en un cronograma debe tener su seguimiento preventivo.
-- Se rompia cuando un correctivo en la misma semana bloqueaba al generador (CronogramaService).
-- Devuelve las celdas huerfanas; lo esperado es cero filas.
DECLARE @anio int = YEAR(GETDATE());

WITH N AS (SELECT TOP 53 ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n FROM sys.all_objects)
SELECT c.Codigo, c.Nombre, N.n AS Semana
FROM GestionMantenimientos_Cronogramas c
CROSS JOIN N
WHERE c.Anio = @anio
  AND SUBSTRING(c.Semanas, (N.n - 1) * 2 + 1, 1) = '1'
  AND NOT EXISTS (
      SELECT 1 FROM GestionMantenimientos_Seguimientos s
      WHERE s.Codigo = c.Codigo AND s.Anio = c.Anio AND s.Semana = N.n
        AND s.TipoMtno <> 2 /* Correctivo */)
ORDER BY c.Codigo, Semana;
