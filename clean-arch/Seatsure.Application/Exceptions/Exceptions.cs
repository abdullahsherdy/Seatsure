using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seatsure.Application.Exceptions;  
// use Exceptions.ExceptionName; 

// Exception e {e.Message, e.SatusCode 
public abstract class AppException : Exception
{
    // added
    public abstract int SatusCode { get; }

    // to implement class
    // if the following line dones't exist, error will be raised, class doesn't implement parent class ctor. 
    protected AppException(string message): base(message ){}
}


/// <summary>
///  Not Found, Resources doesn't exist 
///  
/// </summary>

public sealed class NotFoundException : AppException
{
    public override int SatusCode => 404;
    public NotFoundException(string message) : base(message) { }
}


public sealed class ValidationException : AppException
{
    public ValidationException(string message) : base(message){}

    public override int SatusCode => 400;

}


public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
    public override int SatusCode => 409;
}


//throw NotFoundException("Resource not found");


public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(message) { }
    public override int SatusCode => 401;
}

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message) { }
    public override int SatusCode => 403;
}