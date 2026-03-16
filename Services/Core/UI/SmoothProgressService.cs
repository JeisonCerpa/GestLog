using System;
using System.Windows.Threading;

namespace GestLog.Services.Core.UI
{
    /// <summary>
    /// Servicio para proporcionar un progreso suavizado para barras de progreso
    /// Implementa una transición animada entre valores de progreso
    /// 
    /// Este servicio soluciona el problema de las barras de progreso que avanzan "a brincos"
    /// mediante la implementación de una animación suave entre valores de progreso reportados.
    /// Cuando se reporta un nuevo valor de progreso, el servicio calcula automáticamente
    /// una serie de pasos intermedios para crear una transición fluida y agradable visualmente.
    /// </summary>
    public class SmoothProgressService : IDisposable
    {
        private readonly DispatcherTimer _timer;
        private readonly Action<double> _progressUpdateAction;

        private double _currentValue = 0;
        private double _targetValue = 0;
        private double _stepSize = 1.0;
        private bool _isRunning = false;
        private DateTime _lastUpdateTime = DateTime.Now;
        
        /// <summary>
        /// Umbral de rendimiento en milisegundos - si las actualizaciones son más lentas que esto,
        /// se ajustará automáticamente la velocidad de animación
        /// </summary>
        private const int PERFORMANCE_THRESHOLD_MS = 33; // ~30fps como objetivo
        
        /// <summary>
        /// Factor de reducción cuando se detecta bajo rendimiento
        /// </summary>
        private const double PERFORMANCE_ADJUSTMENT_FACTOR = 1.5;
        
        /// <summary>
        /// Contador para medición dinámica de rendimiento
        /// </summary>
        private int _performanceSampleCount = 0;
        private double _totalRenderTimeMs = 0;

        /// <summary>
        /// Constructor para el servicio de progreso suavizado
        /// </summary>
        /// <param name="progressUpdateAction">Acción que actualiza el valor de progreso en UI</param>        /// <param name="interval">Intervalo de actualización en milisegundos (por defecto 16ms = ~60fps)</param>
        public SmoothProgressService(Action<double> progressUpdateAction, int interval = 16)
        {
            _progressUpdateAction = progressUpdateAction ?? throw new ArgumentNullException(nameof(progressUpdateAction));
              _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(interval)
            };
            _timer.Tick += Timer_Tick!; // El signo de exclamación suprime la advertencia de nullabilidad
        }

        /// <summary>
        /// Reporta un nuevo valor objetivo de progreso
        /// </summary>
        /// <param name="value">El valor objetivo (entre 0 y 100)</param>
        public void Report(double value)
        {
            // Asegurar que el valor está entre 0 y 100
            _targetValue = Math.Min(Math.Max(value, 0), 100);
              // Calcular tamaño del paso basado en la distancia al objetivo
            // Adaptar dinámicamente el tamaño del paso según la distancia para lograr una sensación
            // de aceleración/desaceleración natural (similar a easing functions)
            var distance = Math.Abs(_targetValue - _currentValue);
            
            // Si la distancia es grande, utilizar pasos más grandes para una animación rápida
            // Si la distancia es pequeña, usar pasos más pequeños para una transición suave
            if (distance > 10) {
                _stepSize = distance / 15.0; // Movimiento rápido para grandes cambios
            } else {
                _stepSize = Math.Max(distance / 20.0, 0.2); // Movimiento más suave cerca del objetivo
            }
            
            // Ajustar tamaño del paso basado en el rendimiento detectado del sistema
            if (_performanceSampleCount >= 10) { // Después de recopilar suficientes muestras
                double avgRenderTime = _totalRenderTimeMs / _performanceSampleCount;
                if (avgRenderTime > PERFORMANCE_THRESHOLD_MS) {
                    // Reducir la suavidad para sistemas de bajo rendimiento
                    _stepSize *= PERFORMANCE_ADJUSTMENT_FACTOR;
                }
            }

            // Iniciar el timer si no está corriendo
            if (!_isRunning)
            {
                _isRunning = true;
                _lastUpdateTime = DateTime.Now;
                _timer.Start();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Medir el tiempo entre actualizaciones para evaluar el rendimiento
            var now = DateTime.Now;
            var renderTime = (now - _lastUpdateTime).TotalMilliseconds;
            _lastUpdateTime = now;
            
            // Acumular estadísticas de rendimiento (limitado a 100 muestras)
            if (_performanceSampleCount < 100) {
                _totalRenderTimeMs += renderTime;
                _performanceSampleCount++;
            } else if (_performanceSampleCount == 100) {
                // Resetear para mantener las mediciones actualizadas
                _performanceSampleCount = 50; // Mantener la mitad de los datos
                _totalRenderTimeMs /= 2;
            }

            // Si ya estamos en el valor objetivo, detener el timer
            if (Math.Abs(_currentValue - _targetValue) < 0.1)
            {
                _currentValue = _targetValue;
                _progressUpdateAction(_currentValue);
                _timer.Stop();
                _isRunning = false;
                return;
            }

            // Calcular el nuevo valor actual, moviéndonos hacia el objetivo
            if (_currentValue < _targetValue)
            {
                _currentValue = Math.Min(_currentValue + _stepSize, _targetValue);
            }
            else
            {
                _currentValue = Math.Max(_currentValue - _stepSize, _targetValue);
            }

            // Actualizar la UI con el nuevo valor
            try
            {
                _progressUpdateAction(_currentValue);
            }
            catch
            {
                // Si el ViewModel fue liberado o hay un error de binding, detener el timer
                // para evitar que la excepción llegue al DispatcherUnhandledException
                _timer.Stop();
                _isRunning = false;
            }
        }

        /// <summary>
        /// Establece directamente el valor de progreso sin animación
        /// Útil para resetear o finalizar
        /// </summary>
        public void SetValueDirectly(double value)
        {
            _currentValue = _targetValue = Math.Min(Math.Max(value, 0), 100);
            _progressUpdateAction(_currentValue);
_timer.Stop();
            _isRunning = false;
        }

        /// <summary>
        /// Detiene cualquier animación en curso
        /// </summary>
        public void Stop()
        {
            _timer.Stop();
            _isRunning = false;
        }

        /// <summary>
        /// Libera el DispatcherTimer para evitar que siga corriendo indefinidamente
        /// cuando el ViewModel dueño es liberado (memory leak + potencial crash).
        /// </summary>
        public void Dispose()
        {
            _timer.Stop();
            _timer.Tick -= Timer_Tick!;
            _isRunning = false;
        }
    }
}
