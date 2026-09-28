using System;
using System.Globalization;
using System.Text;

namespace GodTower.Net
{
    public readonly struct HttpRequestHead
    {
        public readonly string Method;
        public readonly string Path;
        public readonly int ContentLength;

        public HttpRequestHead(string method, string path, int contentLength)
        {
            Method = method;
            Path = path;
            ContentLength = contentLength;
        }
    }

    /// <summary>
    /// Minimal HTTP/1.x request-head parsing and routing for the /bump webhook.
    /// Pure functions so they can be unit tested without sockets.
    /// </summary>
    public static class HttpRequestParser
    {
        public const string BumpPath = "/bump";

        static readonly char[] QueryOrFragment = { '?', '#' };
        static readonly string[] LineBreak = { "\r\n" };

        /// <summary>Parses the request line and the Content-Length header from a raw request head.</summary>
        public static bool TryParseHead(string head, out HttpRequestHead request)
        {
            request = default;
            if (string.IsNullOrEmpty(head)) return false;

            int lineEnd = head.IndexOf("\r\n", StringComparison.Ordinal);
            string requestLine = lineEnd >= 0 ? head.Substring(0, lineEnd) : head;
            string[] parts = requestLine.Split(' ');
            if (parts.Length < 2 || parts[0].Length == 0 || parts[1].Length == 0) return false;

            string method = parts[0].ToUpperInvariant();
            int contentLength = 0;

            if (lineEnd >= 0)
            {
                foreach (string line in head.Substring(lineEnd + 2).Split(LineBreak, StringSplitOptions.RemoveEmptyEntries))
                {
                    int colon = line.IndexOf(':');
                    if (colon <= 0) continue;
                    if (!line.Substring(0, colon).Trim().Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
                    int.TryParse(line.Substring(colon + 1).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out contentLength);
                    contentLength = Math.Max(0, contentLength);
                }
            }

            request = new HttpRequestHead(method, NormalizePath(parts[1]), contentLength);
            return true;
        }

        /// <summary>Strips scheme/host (absolute-form targets), query string, fragment and a trailing slash.</summary>
        public static string NormalizePath(string target)
        {
            string path = target ?? string.Empty;
            int schemeIndex = path.IndexOf("://", StringComparison.Ordinal);
            if (schemeIndex >= 0)
            {
                int slash = path.IndexOf('/', schemeIndex + 3);
                path = slash >= 0 ? path.Substring(slash) : "/";
            }

            int cut = path.IndexOfAny(QueryOrFragment);
            if (cut >= 0) path = path.Substring(0, cut);
            if (path.Length > 1) path = path.TrimEnd('/');
            return path.Length == 0 ? "/" : path;
        }

        public static BumpRoute Route(in HttpRequestHead request)
        {
            if (!request.Path.Equals(BumpPath, StringComparison.OrdinalIgnoreCase)) return BumpRoute.NotFound;

            switch (request.Method)
            {
                case "GET":
                case "POST":
                    return BumpRoute.Bump;
                case "OPTIONS":
                    return BumpRoute.Preflight;
                default:
                    return BumpRoute.MethodNotAllowed;
            }
        }

        public static byte[] BuildResponse(int statusCode, string reason, string jsonBody, string extraHeaders = null)
        {
            byte[] body = Encoding.UTF8.GetBytes(jsonBody ?? string.Empty);
            var head = new StringBuilder(256)
                .Append("HTTP/1.1 ").Append(statusCode).Append(' ').Append(reason).Append("\r\n")
                .Append("Content-Type: application/json; charset=utf-8\r\n")
                .Append("Content-Length: ").Append(body.Length).Append("\r\n")
                .Append("Access-Control-Allow-Origin: *\r\n")
                .Append("Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n")
                .Append("Access-Control-Allow-Headers: Content-Type\r\n")
                .Append("Connection: close\r\n");
            if (!string.IsNullOrEmpty(extraHeaders)) head.Append(extraHeaders);
            head.Append("\r\n");

            byte[] headBytes = Encoding.ASCII.GetBytes(head.ToString());
            var result = new byte[headBytes.Length + body.Length];
            Buffer.BlockCopy(headBytes, 0, result, 0, headBytes.Length);
            Buffer.BlockCopy(body, 0, result, headBytes.Length, body.Length);
            return result;
        }
    }
}
