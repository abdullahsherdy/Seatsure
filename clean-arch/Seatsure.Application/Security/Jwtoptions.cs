
namespace Seatsure.Application.Security;

public sealed class Jwtoptions
{
    public const string sectionName = "Jwt";

    public string Issure { get; set; } = string.Empty;  
    public string Audience { get; set; } = string.Empty; 

    public string Key { get; set; } = string.Empty;

    public int ExpiryInMinutes { get; set; } 120; 

}

}