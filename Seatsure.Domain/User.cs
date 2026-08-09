using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualBasic;
// identity user 

namespace Seatsure.Domain
{
    public class User
    {
        // EFcore cursor, ID, Id, id, ClassNameId, ClassNameID, ClassNameid, ClassNameId, ClassNameID, ClassNameid
        // make it by default an id in database 

        [Key]

        public Guid id { get; set; } = new Guid();

        [MaxLength(50)]
        [Required]
        public string name { get; set; }
        [EmailAddress]
        [Required]
        public string email { get; set; }

        // ensure at least 8 characters, one uppercase, one lowercase, one number, one special character
        // P@ssword$5rtdt 
        // hashed string, result of hashing for password 
        // SHA256, SHA512, bcrypt, argon2, PBKDF2
        // store it directly as an hashed value 

        public string PasswordHash { get; set; } = string.Empty;

        // role 
        public UserRole role { get; set; }

        public DateTime createdAtUtc { get; set; }

    }
}
