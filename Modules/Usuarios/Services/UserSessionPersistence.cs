using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using GestLog.Modules.Usuarios.Models.Authentication;

namespace GestLog.Modules.Usuarios.Services
{
    /// <summary>
    /// Servicio para persistir y restaurar la sesión de usuario de forma segura
    /// </summary>
    public static class UserSessionPersistence
    {
        private static readonly string SessionFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "user_session.dat");

        public static void SaveSession(CurrentUserInfo userInfo)
        {
            var directory = Path.GetDirectoryName(SessionFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var json = JsonSerializer.Serialize(userInfo);
            var data = System.Text.Encoding.UTF8.GetBytes(json);
            var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(SessionFilePath, encrypted);
        }

        public static CurrentUserInfo? LoadSession()
        {
            if (!File.Exists(SessionFilePath)) return null;
            try
            {
                var encrypted = File.ReadAllBytes(SessionFilePath);
                var data = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                var json = System.Text.Encoding.UTF8.GetString(data);
                return JsonSerializer.Deserialize<CurrentUserInfo>(json);
            }
            catch
            {
                return null;
            }
        }

        public static void ClearSession()
        {
            if (File.Exists(SessionFilePath))
                File.Delete(SessionFilePath);
        }
    }
}
