using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Seatsure.Domain; 

namespace Seatsure.Application.Interfaces;
public interface IUserRepository
{
       // get by id, email, add 

      Task<User?> GetByIdAsync(Guid id);

      Task<User?> GetByEmailAsync(string email); 

        // define custome type (Email), worthly or over engineering 
        // if my purpose from defining custome email type is to validate, so why you have the model, services. 

      Task AddAsync(User user);
}

