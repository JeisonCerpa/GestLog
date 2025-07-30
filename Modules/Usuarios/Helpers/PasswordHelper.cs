using System;
using System.Security.Cryptography;
using System.Text;

namespace Modules.Usuarios.Helpers
{
    public static class PasswordHelper
    {
        public static string GenerateSalt(int size = 16)
        {
            var saltBytes = new byte[size];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        public static string HashPassword(string password, string salt)
        {
            using var sha256 = SHA256.Create();
            var combined = Encoding.UTF8.GetBytes(password + salt);
            var hash = sha256.ComputeHash(combined);
            return Convert.ToBase64String(hash);
        }
    }
}
