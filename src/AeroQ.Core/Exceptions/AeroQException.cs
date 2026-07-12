namespace AeroQ.Core.Exceptions;

/// <summary>
/// Базовое исключение для всех ошибок AeroQ
/// </summary>
public class AeroQException : Exception
{
    public AeroQException(string message): base(message) { }
    public AeroQException(string message, Exception innerException) : base(message, innerException) { }
}