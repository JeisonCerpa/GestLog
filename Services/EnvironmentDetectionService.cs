using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using GestLog.Services.Core.Logging;
using GestLog.Services.Interfaces;

namespace GestLog.Services;

/// <summary>
/// Servicio profesional de detección de entorno con reglas configurables
/// Sigue SRP: Solo responsable de detectar y gestionar entornos
/// </summary>
public class EnvironmentDetectionService : IEnvironmentDetectionService
{
    private readonly IGestLogLogger _logger;
    private readonly IConfiguration _configuration;
    private string? _forcedEnvironment;
    private string? _detectedEnvironment;

    public string CurrentEnvironment => _forcedEnvironment ?? _detectedEnvironment ?? "Production";
    public bool AutoDetectionEnabled => _configuration.GetValue<bool>("Environment:AutoDetect", true);

    public EnvironmentDetectionService(IGestLogLogger logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<string> DetectEnvironmentAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_forcedEnvironment != null)
            {
                _logger.LogInformation("🔒 Entorno forzado: {Environment}", _forcedEnvironment);
                return _forcedEnvironment;
            }

            if (_detectedEnvironment != null)
            {
                return _detectedEnvironment;
            }

            _logger.LogInformation("🔍 Iniciando detección automática de entorno...");

            // Prioridad 1: Variable de entorno GESTLOG_ENVIRONMENT
            var envVariable = Environment.GetEnvironmentVariable("GESTLOG_ENVIRONMENT");
            if (!string.IsNullOrWhiteSpace(envVariable))
            {
                _detectedEnvironment = envVariable;
                _logger.LogInformation("✅ Entorno detectado por variable de entorno: {Environment}", _detectedEnvironment);
                return _detectedEnvironment;
            }

            // Prioridad 2: Nombre de máquina
            var machineName = Environment.MachineName;
            _detectedEnvironment = DetectByMachineName(machineName);
            if (_detectedEnvironment != null)
            {
                _logger.LogInformation("✅ Entorno detectado por nombre de máquina '{MachineName}': {Environment}", 
                    machineName, _detectedEnvironment);
                return _detectedEnvironment;
            }

            // Prioridad 3: Configuración de base de datos
            _detectedEnvironment = await DetectByDatabaseConfigAsync(cancellationToken);
            if (_detectedEnvironment != null)
            {
                _logger.LogInformation("✅ Entorno detectado por configuración de BD: {Environment}", _detectedEnvironment);
                return _detectedEnvironment;
            }

            // Prioridad 4: Archivo específico de entorno
            _detectedEnvironment = DetectByConfigFile();
            if (_detectedEnvironment != null)
            {
                _logger.LogInformation("✅ Entorno detectado por archivo de configuración: {Environment}", _detectedEnvironment);
                return _detectedEnvironment;
            }

            // Por defecto: Producción
            _detectedEnvironment = "Production";
            _logger.LogWarning("⚠️ No se pudo detectar entorno automáticamente, usando por defecto: {Environment}", _detectedEnvironment);
            
            return _detectedEnvironment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error detectando entorno automáticamente");
            _detectedEnvironment = "Production";
            return _detectedEnvironment;
        }
    }

    public void SetEnvironment(string environment)
    {
        if (string.IsNullOrWhiteSpace(environment))
            throw new ArgumentException("El entorno no puede estar vacío", nameof(environment));

        _forcedEnvironment = environment;
        _logger.LogInformation("🔧 Entorno forzado manualmente: {Environment}", environment);
    }

    public bool IsValidEnvironment(string environment)
    {
        var validEnvironments = new[] { "Development", "Testing", "Production" };
        return validEnvironments.Contains(environment, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<T?> GetEnvironmentConfigAsync<T>(string sectionName, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var currentEnv = await DetectEnvironmentAsync(cancellationToken);
            var configPath = Path.Combine("config", $"database-{currentEnv.ToLowerInvariant()}.json");

            if (!File.Exists(configPath))
            {
                _logger.LogWarning("⚠️ Archivo de configuración específico no encontrado: {ConfigPath}", configPath);
                return null;
            }

            var builder = new ConfigurationBuilder()
                .AddJsonFile(configPath, optional: true);
            
            var envConfig = builder.Build();
            var section = envConfig.GetSection(sectionName);
            
            return section.Get<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error cargando configuración específica de entorno para {SectionName}", sectionName);
            return null;
        }
    }

    private string? DetectByMachineName(string machineName)
    {
        var detectionRules = _configuration.GetSection("Environment:DetectionRules");
        
        foreach (var environment in new[] { "Development", "Testing", "Production" })
        {
            var rules = detectionRules.GetSection(environment).Get<string[]>() ?? Array.Empty<string>();
            
            if (rules.Any(rule => machineName.Contains(rule, StringComparison.OrdinalIgnoreCase)))
            {
                return environment;
            }
        }

        return null;
    }    private async Task<string?> DetectByDatabaseConfigAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.CompletedTask; // Para cumplir con la signatura async
            
            // Detectar por servidor de base de datos configurado
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                return null;

            if (connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
                connectionString.Contains("127.0.0.1"))
            {
                return "Development";
            }

            if (connectionString.Contains("TEST", StringComparison.OrdinalIgnoreCase) ||
                connectionString.Contains("QA", StringComparison.OrdinalIgnoreCase))
            {
                return "Testing";
            }

            if (connectionString.Contains("SIMICSGROUPWKS1", StringComparison.OrdinalIgnoreCase))
            {
                return "Production";
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detectando entorno por configuración de base de datos");
            return null;
        }
    }

    private string? DetectByConfigFile()
    {
        try
        {
            var environments = new[] { "development", "testing", "production" };
            
            foreach (var env in environments)
            {
                var configFile = Path.Combine("config", $"database-{env}.json");
                if (File.Exists(configFile))
                {
                    var content = File.ReadAllText(configFile);
                    if (!string.IsNullOrWhiteSpace(content) && content.Contains("\"Server\""))
                    {
                        return char.ToUpperInvariant(env[0]) + env[1..];
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detectando entorno por archivos de configuración");
            return null;
        }
    }
}
