using System;

namespace GestLog.Models.Exceptions;

/// <summary>
/// Exception thrown when security validation fails
/// </summary>
public class SecurityConfigurationException : Exception
{
    public string SecurityContext { get; }

    public SecurityConfigurationException(string message, string securityContext) 
        : base(message)
    {
        SecurityContext = securityContext;
    }

    public SecurityConfigurationException(string message, string securityContext, Exception innerException) 
        : base(message, innerException)
    {
        SecurityContext = securityContext;
    }
}

/// <summary>
/// Exception thrown when environment variables are missing
/// </summary>
public class EnvironmentVariableException : Exception
{
    public string VariableName { get; }
    public bool IsRequired { get; }

    public EnvironmentVariableException(string message, string variableName, bool isRequired = true) 
        : base(message)
    {
        VariableName = variableName;
        IsRequired = isRequired;
    }

    public EnvironmentVariableException(string message, string variableName, bool isRequired, Exception innerException) 
        : base(message, innerException)
    {
        VariableName = variableName;
        IsRequired = isRequired;
    }
}
