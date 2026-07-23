using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionCartera.Services;

/// <summary>
/// Prueba de conexión SMTP real (handshake + autenticación) SIN enviar ningún correo.
/// System.Net.Mail.SmtpClient no ofrece un "solo conectar/autenticar", así que hablamos
/// el protocolo SMTP directamente y cerramos con QUIT antes de enviar nada.
/// </summary>
internal static class SmtpConnectionTester
{
    /// <returns>(ok, mensaje) — mensaje en español, listo para mostrar al usuario.</returns>
    public static async Task<(bool ok, string message)> TestAsync(
        string server, int port, bool useSsl, string username, string password,
        int timeoutMs, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeoutMs <= 0 ? 30000 : timeoutMs);
        var ct = cts.Token;

        using var tcp = new TcpClient();
        try
        {
            await tcp.ConnectAsync(server, port, ct);
        }
        catch (OperationCanceledException)
        {
            return (false, $"No se pudo conectar a {server}:{port} (tiempo de espera agotado). Verifique el servidor, el puerto y su conexión a internet.");
        }

        Stream stream = tcp.GetStream();
        bool implicitSsl = port == 465; // 465 = SSL implícito; 587/25 = STARTTLS
        if (implicitSsl)
            stream = await UpgradeSslAsync(stream, server, ct);

        var greeting = await ReadAsync(stream, ct);
        if (!greeting.StartsWith("220"))
            return (false, $"El servidor respondió inesperadamente al conectar: {greeting.Trim()}");

        await SendAsync(stream, $"EHLO {Environment.MachineName}", ct);
        var ehlo = await ReadAsync(stream, ct);
        if (ehlo.StartsWith("5"))
        {
            await SendAsync(stream, $"HELO {Environment.MachineName}", ct);
            ehlo = await ReadAsync(stream, ct);
        }

        if (!implicitSsl && useSsl)
        {
            await SendAsync(stream, "STARTTLS", ct);
            var startTls = await ReadAsync(stream, ct);
            if (!startTls.StartsWith("220"))
                return (false, "El servidor no aceptó STARTTLS. El puerto o el cifrado (SSL) no son compatibles; para la mayoría de servidores use el puerto 587 con SSL activado.");
            stream = await UpgradeSslAsync(stream, server, ct);
            await SendAsync(stream, $"EHLO {Environment.MachineName}", ct);
            await ReadAsync(stream, ct);
        }

        await SendAsync(stream, "AUTH LOGIN", ct);
        var authPrompt = await ReadAsync(stream, ct);
        if (!authPrompt.StartsWith("334"))
            return (false, $"El servidor no ofreció autenticación por usuario/contraseña (AUTH LOGIN). Respuesta: {authPrompt.Trim()}");

        await SendAsync(stream, Convert.ToBase64String(Encoding.UTF8.GetBytes(username)), ct);
        await ReadAsync(stream, ct);
        await SendAsync(stream, Convert.ToBase64String(Encoding.UTF8.GetBytes(password)), ct);
        var authResult = await ReadAsync(stream, ct);

        try { await SendAsync(stream, "QUIT", ct); } catch { /* cierre de cortesía, sin importancia */ }

        if (authResult.StartsWith("235"))
            return (true, "Conexión y autenticación exitosas.");
        if (authResult.StartsWith("535") || authResult.StartsWith("534") || authResult.Contains("5.7"))
            return (false, "Usuario o contraseña del correo incorrectos, o el servidor rechazó la autenticación. Verifique el correo y la contraseña (algunos servidores requieren una 'contraseña de aplicación').");
        return (false, $"El servidor rechazó la autenticación: {authResult.Trim()}");
    }

    private static async Task<Stream> UpgradeSslAsync(Stream inner, string host, CancellationToken ct)
    {
        var ssl = new SslStream(inner, leaveInnerStreamOpen: false);
        await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = host }, ct);
        return ssl;
    }

    private static async Task SendAsync(Stream s, string line, CancellationToken ct)
    {
        var bytes = Encoding.ASCII.GetBytes(line + "\r\n");
        await s.WriteAsync(bytes.AsMemory(0, bytes.Length), ct);
        await s.FlushAsync(ct);
    }

    private static async Task<string> ReadAsync(Stream s, CancellationToken ct)
    {
        var buffer = new byte[4096];
        var sb = new StringBuilder();
        while (true)
        {
            int n = await s.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
            if (n <= 0) break;
            sb.Append(Encoding.ASCII.GetString(buffer, 0, n));
            if (IsComplete(sb.ToString())) break;
        }
        return sb.ToString();
    }

    /// <summary>Una respuesta SMTP está completa cuando la última línea es "NNN &lt;espacio&gt;...".</summary>
    private static bool IsComplete(string response)
    {
        var lines = response.Replace("\r\n", "\n").Split('\n');
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            var line = lines[i];
            if (line.Length == 0) continue;
            return line.Length >= 4 && char.IsDigit(line[0]) && char.IsDigit(line[1]) && char.IsDigit(line[2]) && line[3] == ' ';
        }
        return false;
    }
}
