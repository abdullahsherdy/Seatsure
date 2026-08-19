using Microsoft.Extensions.Options;
using Seatsure.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seatsure.Application.Security;
internal sealed class JwtService : IJWTService
{
    private readonly Jwtoptions _jwtoptions; 

    public JwtService(IOptions<Jwtoptions> jwtoptions)
    {
        _jwtoptions = jwtoptions.Value;
    }
public (string token, DateTime ExpiresAtUtc) generateToken(User user)
    {
        throw new NotImplementedException();
    }
}
