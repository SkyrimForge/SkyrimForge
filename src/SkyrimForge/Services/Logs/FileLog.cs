using System;
using System.IO;
using System.Threading;

namespace SkyrimForge;

internal sealed class FileLog(string path) : ILog
{
    private readonly string path = path;
    private readonly Lock writeLock = new();

    private int caughtMessageExceptions;

    public void Write(string message)
    {
        lock (writeLock)
        {
            try
            {
                if (caughtMessageExceptions > 0)
                {
                    Append($"{caughtMessageExceptions} messages could not be logged due to exceptions upon writing.");
                    caughtMessageExceptions = 0;
                }
                Append(message);
            }
            // Not only IOException. File.AppendAllText also throws UnauthorizedAccessException and SecurityException.
            catch (Exception)
            {
                caughtMessageExceptions++;
            }
        }
    }

    // Appended without an open FileStream, so the file can be read while the game runs.
    private void Append(string message) => File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
}
