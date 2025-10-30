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

        /// <summary>
        /// Hashea una contraseña usando PBKDF2-SHA256
        /// </summary>
        public static string HashPassword(string password, string salt)
        {
            try
            {
                // Convertir salt de Base64 a bytes
                byte[] saltBytes = Convert.FromBase64String(salt);
                
                // Usar PBKDF2-SHA256 con 10,000 iteraciones
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256))
                {
                    byte[] hash = pbkdf2.GetBytes(32);
                    return Convert.ToBase64String(hash);
                }
            }
            catch
            {
                // Fallback a SHA256 simple para compatibilidad con contraseñas antiguas
                using var sha256 = SHA256.Create();
                var combined = Encoding.UTF8.GetBytes(password + salt);
                var hash = sha256.ComputeHash(combined);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Verifica una contraseña contra su hash y salt
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
                return false;

            var hashToCheck = HashPassword(password, storedSalt);
            return hashToCheck.Equals(storedHash, StringComparison.Ordinal);
        }
    }
}
