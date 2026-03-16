using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GestLog.Services.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GestLog.Services.Core;

namespace GestLog.Services.Core
{
    /// <summary>
    /// Define una variable de entorno con su valor esperado
    /// </summary>
    public class EnvironmentVariableDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = true;
        public bool IsSensitive { get; set; } = false;
    }

    /// <summary>
    /// Resultado de la sincronización de variables de entorno
    /// </summary>
    public class EnvironmentSyncResult
    {
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Unchanged { get; set; }
        public int Failed { get; set; }
        public List<string> Logs { get; set; } = new();
    }

    /// <summary>
    /// Interfaz para sincronizar variables de entorno
    /// </summary>
    public interface IEnvironmentVariableService
    {
        /// <summary>
        /// Sincroniza automáticamente las variables de entorno necesarias
        /// Crea, actualiza o valida según sea necesario
        /// </summary>
        Task<EnvironmentSyncResult> SyncEnvironmentVariablesAsync();
    }

    /// <summary>
    /// Servicio que sincroniza variables de entorno automáticamente
    /// Al iniciar, verifica si existen las variables necesarias
    /// Si no existen, las crea; si están desactualizadas, las actualiza
    /// </summary>
    public class EnvironmentVariableService : IEnvironmentVariableService
    {
        private readonly IConfiguration _configuration;
        private readonly IGestLogLogger _logger;

        public EnvironmentVariableService(
            IConfiguration configuration,
            IGestLogLogger logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sincroniza automáticamente las variables de entorno necesarias
        /// </summary>
        public async Task<EnvironmentSyncResult> SyncEnvironmentVariablesAsync()
        {
            var result = new EnvironmentSyncResult();

            try
            {
                _logger.Logger.LogInformation("🔄 Iniciando sincronización de variables de entorno...");

                // Obtener las variables que se necesitan desde appsettings.json
                var requiredVariables = GetRequiredEnvironmentVariables();

                _logger.Logger.LogInformation("📋 Total de variables a sincronizar: {VariableCount}", requiredVariables.Count);

                foreach (var variable in requiredVariables)
                {
                    try
                    {
                        var currentValue = Environment.GetEnvironmentVariable(variable.Name);
                        var expectedValue = variable.Value;

                        // Logging sin mostrar valores sensibles
                        var displayValue = variable.IsSensitive ? "***" : expectedValue;
                        var displayCurrent = variable.IsSensitive ? "***" : currentValue;                        if (currentValue == null)
                        {
                            // Variable no existe, crearla
                            if (!string.IsNullOrEmpty(expectedValue))
                            {
                                try
                                {
                                    Environment.SetEnvironmentVariable(variable.Name, expectedValue, EnvironmentVariableTarget.User);
                                    result.Created++;
                                    result.Logs.Add($"✅ CREADA: {variable.Name}");
                                    _logger.Logger.LogInformation("✅ Variable creada: {VariableName}", variable.Name);
                                }
                                catch (Exception ex)
                                {
                                    result.Failed++;
                                    result.Logs.Add($"❌ ERROR creando {variable.Name}: {ex.Message}");
                                    _logger.Logger.LogError(ex, "❌ Error al crear variable: {VariableName}", variable.Name);
                                }
                            }
                        }
                        else if (currentValue != expectedValue)
                        {
                            // ⚠️ IMPORTANTE: En modo Development, respeta las variables ya definidas
                            // Solo actualiza si el ambiente es Production o si la variable viene de appsettings
                            var environment = Environment.GetEnvironmentVariable("GESTLOG_ENVIRONMENT") ?? "Production";
                            var isDevelopment = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
                            
                            if (!isDevelopment || variable.Name == "GESTLOG_ENVIRONMENT")
                            {
                                // Variable existe pero con valor diferente, actualizar (solo en producción)
                                try
                                {
                                    Environment.SetEnvironmentVariable(variable.Name, expectedValue, EnvironmentVariableTarget.User);
                                    result.Updated++;
                                    result.Logs.Add($"🔄 ACTUALIZADA: {variable.Name}");
                                    _logger.Logger.LogInformation("🔄 Variable actualizada: {VariableName}", variable.Name);
                                }
                                catch (Exception ex)
                                {
                                    result.Failed++;
                                    result.Logs.Add($"❌ ERROR actualizando {variable.Name}: {ex.Message}");
                                    _logger.Logger.LogError(ex, "❌ Error al actualizar variable: {VariableName}", variable.Name);
                                }
                            }
                            else
                            {
                                // En desarrollo, respetar la variable existente
                                result.Unchanged++;
                                result.Logs.Add($"⏭️ SIN CAMBIOS (Dev): {variable.Name}");
                                _logger.Logger.LogInformation("⏭️ Variable respetada en development: {VariableName}", variable.Name);
                            }
                        }
                        else
                        {
                            // Variable existe y tiene el valor correcto
                            result.Unchanged++;
                            result.Logs.Add($"✓ SIN CAMBIOS: {variable.Name}");
                            _logger.Logger.LogInformation("✓ Variable correcta: {VariableName}", variable.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Failed++;
                        result.Logs.Add($"❌ ERROR procesando {variable.Name}: {ex.Message}");
                        _logger.Logger.LogError(ex, "❌ Error procesando variable: {VariableName}", variable.Name);
                    }
                }

                // Resumen final
                _logger.Logger.LogInformation("📊 Sincronización completada: Creadas={CreatedCount}, Actualizadas={UpdatedCount}, Sin cambios={UnchangedCount}, Errores={FailedCount}",
                    result.Created, result.Updated, result.Unchanged, result.Failed);

                if (result.Failed == 0)
                {
                    _logger.Logger.LogInformation("✅ Variables de entorno sincronizadas correctamente");
                }
                else
                {
                    _logger.Logger.LogWarning("⚠️ Hubo {FailedCount} error(es) sincronizando variables", result.Failed);
                }
            }
            catch (Exception ex)
            {
                _logger.Logger.LogError(ex, "❌ Error crítico sincronizando variables de entorno");
                result.Failed++;
            }

            return await Task.FromResult(result);
        }        /// <summary>
        /// Obtiene la lista de variables de entorno requeridas desde appsettings.json
        /// </summary>
        private List<EnvironmentVariableDefinition> GetRequiredEnvironmentVariables()
        {
            var variables = new List<EnvironmentVariableDefinition>();

            var bootstrapVariables = BootstrapProvisioningService.LoadBootstrapVariables(AppContext.BaseDirectory, out var bootstrapPath);
            if (bootstrapVariables.Count > 0)
            {
                _logger.Logger.LogInformation("🧩 Bootstrap detectado en {BootstrapPath} con {VariableCount} variable(s)", bootstrapPath, bootstrapVariables.Count);
            }

            // 🔍 Detectar el ambiente actual
            var currentEnvironment = Environment.GetEnvironmentVariable("GESTLOG_ENVIRONMENT") ?? "Production";
            _logger.Logger.LogInformation("🔍 Leyendo configuración para ambiente: {Environment}", currentEnvironment);

            // Variables de Base de Datos
            // ✅ IMPORTANTE: Solo usar valores de appsettings.json si NO están ya definidos en variables de entorno
            // Esto permite que los desarrolladores mantengan su configuración de desarrollo sin que sea sobrescrita
            
            var dbSection = _configuration.GetSection("Database");
            
            // Leer valores actuales primero (si existen, respetarlos)
            var dbServer = Environment.GetEnvironmentVariable("GESTLOG_DB_SERVER")
                           ?? bootstrapVariables.GetValueOrDefault("GESTLOG_DB_SERVER")
                           ?? dbSection["FallbackServer"];
            var dbName = Environment.GetEnvironmentVariable("GESTLOG_DB_NAME")
                         ?? bootstrapVariables.GetValueOrDefault("GESTLOG_DB_NAME")
                         ?? dbSection["FallbackDatabase"];
            var dbUser = Environment.GetEnvironmentVariable("GESTLOG_DB_USER")
                         ?? bootstrapVariables.GetValueOrDefault("GESTLOG_DB_USER")
                         ?? dbSection["FallbackUsername"];
            var dbPassword = Environment.GetEnvironmentVariable("GESTLOG_DB_PASSWORD")
                             ?? bootstrapVariables.GetValueOrDefault("GESTLOG_DB_PASSWORD")
                             ?? dbSection["FallbackPassword"];
            var dbIntegratedSecurity = Environment.GetEnvironmentVariable("GESTLOG_DB_INTEGRATED_SECURITY")
                                       ?? bootstrapVariables.GetValueOrDefault("GESTLOG_DB_INTEGRATED_SECURITY")
                                       ?? dbSection["FallbackUseIntegratedSecurity"];

            variables.Add(new EnvironmentVariableDefinition
            {
                Name = "GESTLOG_DB_SERVER",
                Value = dbServer,
                Description = "Servidor de Base de Datos SQL Server",
                IsRequired = true,
                IsSensitive = false
            });

            variables.Add(new EnvironmentVariableDefinition
            {
                Name = "GESTLOG_DB_NAME",
                Value = dbName,
                Description = "Nombre de la Base de Datos",
                IsRequired = true,
                IsSensitive = false
            });

            variables.Add(new EnvironmentVariableDefinition
            {
                Name = "GESTLOG_DB_USER",
                Value = dbUser,
                Description = "Usuario de la Base de Datos",
                IsRequired = true,
                IsSensitive = true
            });

            variables.Add(new EnvironmentVariableDefinition
            {
                Name = "GESTLOG_DB_PASSWORD",
                Value = dbPassword,
                Description = "Contraseña de la Base de Datos",
                IsRequired = true,
                IsSensitive = true
            });

            variables.Add(new EnvironmentVariableDefinition
            {
                Name = "GESTLOG_DB_INTEGRATED_SECURITY",
                Value = dbIntegratedSecurity,
                Description = "Usar autenticación integrada de Windows",
                IsRequired = false,
                IsSensitive = false
            });

            // Variables de Email (si están configuradas)
            var emailSection = _configuration.GetSection("EmailServices:PasswordReset");
            if (emailSection.Exists())
            {
                var smtpServer = Environment.GetEnvironmentVariable("GESTLOG_SMTP_SERVER")
                                 ?? bootstrapVariables.GetValueOrDefault("GESTLOG_SMTP_SERVER")
                                 ?? emailSection["SmtpServer"];
                var smtpPort = Environment.GetEnvironmentVariable("GESTLOG_SMTP_PORT")
                               ?? bootstrapVariables.GetValueOrDefault("GESTLOG_SMTP_PORT")
                               ?? emailSection["SmtpPort"];
                var senderEmail = Environment.GetEnvironmentVariable("GESTLOG_SENDER_EMAIL")
                                  ?? bootstrapVariables.GetValueOrDefault("GESTLOG_SENDER_EMAIL")
                                  ?? emailSection["SenderEmail"];
                var username = Environment.GetEnvironmentVariable("GESTLOG_EMAIL_USERNAME")
                               ?? bootstrapVariables.GetValueOrDefault("GESTLOG_EMAIL_USERNAME")
                               ?? bootstrapVariables.GetValueOrDefault("GESTLOG_PASSWORD_RESET_EMAIL_USERNAME")
                               ?? emailSection["Username"];
                var password = Environment.GetEnvironmentVariable("GESTLOG_EMAIL_PASSWORD")
                               ?? bootstrapVariables.GetValueOrDefault("GESTLOG_EMAIL_PASSWORD")
                               ?? bootstrapVariables.GetValueOrDefault("GESTLOG_PASSWORD_RESET_EMAIL_PASSWORD")
                               ?? emailSection["Password"];

                variables.Add(new EnvironmentVariableDefinition
                {
                    Name = "GESTLOG_SMTP_SERVER",
                    Value = smtpServer,
                    Description = "Servidor SMTP para envío de emails",
                    IsRequired = false,
                    IsSensitive = false
                });

                variables.Add(new EnvironmentVariableDefinition
                {
                    Name = "GESTLOG_SMTP_PORT",
                    Value = smtpPort,
                    Description = "Puerto SMTP",
                    IsRequired = false,
                    IsSensitive = false
                });

                variables.Add(new EnvironmentVariableDefinition
                {
                    Name = "GESTLOG_SENDER_EMAIL",
                    Value = senderEmail,
                    Description = "Email remitente",
                    IsRequired = false,
                    IsSensitive = false
                });

                variables.Add(new EnvironmentVariableDefinition
                {
                    Name = "GESTLOG_EMAIL_USERNAME",
                    Value = username,
                    Description = "Usuario para autenticación SMTP",
                    IsRequired = false,
                    IsSensitive = true
                });

                variables.Add(new EnvironmentVariableDefinition
                {
                    Name = "GESTLOG_EMAIL_PASSWORD",
                    Value = password,
                    Description = "Contraseña para autenticación SMTP",
                    IsRequired = false,
                    IsSensitive = true
                });
            }

            // Filtrar variables con valor definido
            return variables.Where(v => !string.IsNullOrEmpty(v.Value)).ToList();
        }
    }
}
