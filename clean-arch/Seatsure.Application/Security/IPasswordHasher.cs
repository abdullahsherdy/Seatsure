

using System.Security.Cryptography.X509Certificates;

namespace Seatsure.Application.Security;
public interface IPasswordHasher
{
    string Hash(string password);
    // 
    /// <summary>
    ///  flow of passowrd. user enter normal password, we store it as hashed value
    ///  login, verify normal password(not stored) but entered in client side, with stored hasedPassword, if match then login success 
    ///  using the same generator 
    /// </summary>
    /// <param name="passord"></param>
    /// <param name="hashedPassword"></param>
    /// <returns></returns>
    bool verify (string passord, string hashedPassword);

}
