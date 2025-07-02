using System;

namespace RabbitMQCoreClient.Exceptions;

public class BadMessageException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="BadMessageException" /> class.</summary>
    public BadMessageException()
    {

    }

    /// <summary>Initializes a new instance of the <see cref="BadMessageException" /> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public BadMessageException(string? message) : base(message)
    {

    }

    /// <summary>Initializes a new instance of the <see cref="BadMessageException" /> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.</param>
    public BadMessageException(string? message, Exception? innerException) : base(message, innerException)
    {

    }
}
