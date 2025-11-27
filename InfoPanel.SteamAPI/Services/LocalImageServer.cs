using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace InfoPanel.SteamAPI.Services
{
    /// <summary>
    /// A simple local HTTP server to serve cached images to InfoPanel
    /// </summary>
    public class LocalImageServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly string _rootDirectory;
        private readonly EnhancedLoggingService? _enhancedLogger;
        private Task? _listenTask;
        private CancellationTokenSource? _cts;
        private int _port;
        private bool _isStarted;

        public string BaseUrl => $"http://localhost:{_port}/";
        public bool IsRunning => _isStarted && _listener.IsListening;

        public LocalImageServer(string rootDirectory, int port = 0, EnhancedLoggingService? enhancedLogger = null)
        {
            _rootDirectory = rootDirectory;
            _port = port;
            _enhancedLogger = enhancedLogger;
            _listener = new HttpListener();

            // Ensure root directory exists
            if (!Directory.Exists(_rootDirectory))
            {
                Directory.CreateDirectory(_rootDirectory);
            }
        }

        public void Start()
        {
            if (_isStarted) return;

            try
            {
                // If port is 0, find an available one. Otherwise use the specified port.
                if (_port == 0)
                {
                    _port = GetAvailablePort();
                }

                _listener.Prefixes.Clear();
                _listener.Prefixes.Add(BaseUrl);
                _listener.Start();

                _cts = new CancellationTokenSource();
                _listenTask = Task.Run(() => HandleRequests(_cts.Token));
                _isStarted = true;

                _enhancedLogger?.LogInfo("LocalImageServer", $"Started local image server on port {_port}", new { RootDir = _rootDirectory });
            }
            catch (Exception ex)
            {
                _enhancedLogger?.LogError("LocalImageServer", "Failed to start server", ex);
            }
        }

        public void Stop()
        {
            if (!_isStarted) return;

            try
            {
                _cts?.Cancel();
                _listener.Stop();
                _isStarted = false;
                _enhancedLogger?.LogInfo("LocalImageServer", "Server stopped");
            }
            catch (Exception ex)
            {
                _enhancedLogger?.LogError("LocalImageServer", "Error stopping server", ex);
            }
        }

        private int GetAvailablePort()
        {
            // Bind to port 0 to let OS pick a free port
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                return ((IPEndPoint)socket.LocalEndPoint!).Port;
            }
        }

        private async Task HandleRequests(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = ProcessRequestAsync(context);
                }
                catch (HttpListenerException)
                {
                    // Listener stopped
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // Listener disposed
                    break;
                }
                catch (Exception ex)
                {
                    _enhancedLogger?.LogError("LocalImageServer", "Error accepting request", ex);
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            try
            {
                // URL decode the path to handle spaces and special chars
                var urlPath = context.Request.Url?.AbsolutePath ?? "";
                var relativePath = WebUtility.UrlDecode(urlPath.TrimStart('/'));

                if (string.IsNullOrEmpty(relativePath))
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                // Security check: prevent directory traversal
                if (relativePath.Contains("..") || Path.IsPathRooted(relativePath))
                {
                    context.Response.StatusCode = 403;
                    return;
                }

                var filePath = Path.Combine(_rootDirectory, relativePath);

                // Handle forward/back slashes
                filePath = Path.GetFullPath(filePath);
                if (!filePath.StartsWith(Path.GetFullPath(_rootDirectory), StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = 403;
                    return;
                }

                if (File.Exists(filePath))
                {
                    var bytes = await File.ReadAllBytesAsync(filePath);

                    // Determine content type based on extension
                    string ext = Path.GetExtension(filePath).ToLowerInvariant();
                    context.Response.ContentType = ext switch
                    {
                        ".png" => "image/png",
                        ".jpg" or ".jpeg" => "image/jpeg",
                        ".webp" => "image/webp",
                        ".gif" => "image/gif",
                        _ => "application/octet-stream"
                    };

                    context.Response.ContentLength64 = bytes.Length;
                    await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                    context.Response.StatusCode = 200;
                }
                else
                {
                    _enhancedLogger?.LogDebug("LocalImageServer", $"File not found: {filePath}");
                    context.Response.StatusCode = 404;
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                _enhancedLogger?.LogError("LocalImageServer", "Error processing request", ex);
            }
            finally
            {
                try
                {
                    context.Response.Close();
                }
                catch { /* Ignore close errors */ }
            }
        }

        /// <summary>
        /// Converts a local file path to a server URL
        /// Returns null if the file is not within the server's root directory
        /// </summary>
        public string? GetUrlForFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;

            var fullPath = Path.GetFullPath(filePath);
            var rootPath = Path.GetFullPath(_rootDirectory);

            if (fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                // Extract relative path
                var relativePath = fullPath.Substring(rootPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                // Convert to URL format (forward slashes)
                var urlPath = relativePath.Replace('\\', '/');
                // Encode path segments
                urlPath = string.Join("/", Array.ConvertAll(urlPath.Split('/'), WebUtility.UrlEncode));

                return $"{BaseUrl}{urlPath}";
            }

            return null;
        }

        public void Dispose()
        {
            Stop();
            _listener.Close();
            _cts?.Dispose();
        }
    }
}
