namespace Seatsure.BLL.Exceptions;

/// <summary>
/// Base for all expected business failures. The API's exception-handling middleware
/// maps each derived type to an RFC 7807 Problem Details response with <see cref="StatusCode"/>.
/// </summary>
public abstract class AppException : Exception
{
    public abstract int StatusCode { get; }

    protected AppException(string message) : base(message) { }
}

/// <summary>400 — request is syntactically valid but breaks a business rule (e.g. quantity &lt; 1).</summary>
public sealed class ValidationException : AppException
{
    public override int StatusCode => 400;
    public ValidationException(string message) : base(message) { }
}

/// <summary>401 — authentication failed (bad credentials).</summary>
public sealed class UnauthorizedException : AppException
{
    public override int StatusCode => 401;
    public UnauthorizedException(string message = "Invalid credentials.") : base(message) { }
}

/// <summary>403 — authenticated but not allowed (e.g. not the resource owner).</summary>
public sealed class ForbiddenException : AppException
{
    public override int StatusCode => 403;
    public ForbiddenException(string message = "You do not have permission to perform this action.") : base(message) { }
}

/// <summary>404 — requested resource does not exist.</summary>
public sealed class NotFoundException : AppException
{
    public override int StatusCode => 404;
    public NotFoundException(string message) : base(message) { }
}

/// <summary>409 — conflict: insufficient inventory, concurrency conflict, or invalid state transition.</summary>
public sealed class ConflictException : AppException
{
    public override int StatusCode => 409;
    public ConflictException(string message) : base(message) { }
}
