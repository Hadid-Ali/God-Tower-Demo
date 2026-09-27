using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace GodTower.Net
{
    /// <summary>
    /// Lightweight HTTP listener for the external boxing-glove event: GET or POST /bump on port 56789.
    /// Built on <see cref="TcpListener"/>, which works in the Editor and in IL2CPP Android builds.
    /// Socket work runs on background threads; <see cref="BumpAccepted"/> is raised off the main
    /// thread and must be marshalled by the subscriber (see GameSession / MainThreadDispatcher).
    /// </summary>
    public sealed class BumpServer : IDisposable
    {
        public const int DefaultPort = 56789;

        const int MaxHeadBytes = 8 * 1024;
        const int MaxBodyBytes = 64 * 1024;
        const int ReadTimeoutMs = 3000;

        /// <summary>Raised on a worker thread for each bump that should trigger gameplay.</summary>
        public event Action BumpAccepted;

        /// <summary>Set by the main thread: true only while a level is actively being played.</summary>
        public bool AcceptingBumps
        {
            get => Volatile.Read(ref _acceptingBumps) == 1;
            set => Interlocked.Exchange(ref _acceptingBumps, value ? 1 : 0);
        }

        public int Port { get; }
        public bool IsRunning => _running;

        TcpListener _listener;
        Thread _acceptThread;
        volatile bool _running;
        int _acceptingBumps;

        public BumpServer(int port) => Port = port;

        public void Start()
        {
            if (_running) return;
            try
            {
                _listener = CreateListener(Port);
                _running = true;
                _acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "GodTower.BumpServer" };
                _acceptThread.Start();
                Debug.Log($"[BumpServer] Listening: GET/POST http://localhost:{Port}{HttpRequestParser.BumpPath}");
            }
            catch (Exception e)
            {
                _running = false;
                Debug.LogError($"[BumpServer] Could not start on port {Port}: {e.Message}");
            }
        }

        static TcpListener CreateListener(int port)
        {
            // Prefer a dual-stack socket so both 127.0.0.1 and ::1 ("localhost") reach the game.
            try
            {
                var dual = new TcpListener(IPAddress.IPv6Any, port);
                dual.Server.DualMode = true;
                dual.Start();
                return dual;
            }
            catch (Exception)
            {
                var v4 = new TcpListener(IPAddress.Any, port);
                v4.Start();
                return v4;
            }
        }

        void AcceptLoop()
        {
            while (_running)
            {
                TcpClient client;
                try
                {
                    client = _listener.AcceptTcpClient();
                }
                catch (Exception)
                {
                    if (!_running) break; // Dispose() closed the socket.
                    continue;
                }

                ThreadPool.QueueUserWorkItem(_ => HandleClient(client));
            }
        }

        void HandleClient(TcpClient client)
        {
            using (client)
            {
                try
                {
                    client.ReceiveTimeout = ReadTimeoutMs;
                    client.SendTimeout = ReadTimeoutMs;
                    NetworkStream stream = client.GetStream();

                    if (!TryReadHead(stream, out string head, out int bodyBytesAlreadyRead) ||
                        !HttpRequestParser.TryParseHead(head, out HttpRequestHead request))
                    {
                        Write(stream, 400, "Bad Request", "{\"ok\":false,\"error\":\"bad request\"}");
                        return;
                    }

                    DrainBody(stream, Math.Min(request.ContentLength, MaxBodyBytes) - bodyBytesAlreadyRead);

                    switch (HttpRequestParser.Route(request))
                    {
                        case BumpRoute.Bump:
                            bool triggered = AcceptingBumps;
                            if (triggered) BumpAccepted?.Invoke();
                            Write(stream, 200, "OK", triggered
                                ? "{\"ok\":true,\"triggered\":true}"
                                : "{\"ok\":true,\"triggered\":false,\"reason\":\"no level is running\"}");
                            break;
                        case BumpRoute.Preflight:
                            Write(stream, 204, "No Content", string.Empty);
                            break;
                        case BumpRoute.MethodNotAllowed:
                            Write(stream, 405, "Method Not Allowed", "{\"ok\":false,\"error\":\"use GET or POST\"}", "Allow: GET, POST, OPTIONS\r\n");
                            break;
                        default:
                            Write(stream, 404, "Not Found", "{\"ok\":false,\"error\":\"not found\"}");
                            break;
                    }
                }
                catch (Exception e) when (e is IOException || e is SocketException || e is ObjectDisposedException)
                {
                    // Client disconnected or timed out; nothing to recover.
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[BumpServer] Request failed: {e.Message}");
                }
            }
        }

        /// <summary>Reads until the blank line that ends the head. Reports how many body bytes were over-read.</summary>
        static bool TryReadHead(NetworkStream stream, out string head, out int bodyBytesAlreadyRead)
        {
            head = null;
            bodyBytesAlreadyRead = 0;
            var buffer = new byte[MaxHeadBytes];
            int total = 0;

            while (total < buffer.Length)
            {
                int read = stream.Read(buffer, total, buffer.Length - total);
                if (read <= 0) break;
                total += read;

                int end = IndexOfHeadEnd(buffer, total);
                if (end < 0) continue;

                head = Encoding.ASCII.GetString(buffer, 0, end);
                bodyBytesAlreadyRead = total - (end + 4);
                return true;
            }

            if (total <= 0) return false;
            head = Encoding.ASCII.GetString(buffer, 0, total);
            return true;
        }

        static int IndexOfHeadEnd(byte[] buffer, int length)
        {
            for (int i = 3; i < length; i++)
            {
                if (buffer[i - 3] == (byte)'\r' && buffer[i - 2] == (byte)'\n' &&
                    buffer[i - 1] == (byte)'\r' && buffer[i] == (byte)'\n')
                    return i - 3;
            }
            return -1;
        }

        static void DrainBody(NetworkStream stream, int remaining)
        {
            var scratch = new byte[1024];
            while (remaining > 0)
            {
                int read = stream.Read(scratch, 0, Math.Min(scratch.Length, remaining));
                if (read <= 0) break;
                remaining -= read;
            }
        }

        static void Write(NetworkStream stream, int status, string reason, string body, string extraHeaders = null)
        {
            byte[] response = HttpRequestParser.BuildResponse(status, reason, body, extraHeaders);
            stream.Write(response, 0, response.Length);
            stream.Flush();
        }

        public void Dispose()
        {
            if (!_running) return;
            _running = false;
            try { _listener?.Stop(); }
            catch (Exception) { /* already closed */ }
            _listener = null;
            Debug.Log("[BumpServer] Stopped.");
        }
    }
}
