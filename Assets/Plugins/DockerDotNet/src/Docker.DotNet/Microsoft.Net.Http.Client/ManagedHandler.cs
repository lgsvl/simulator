using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Net.Http.Client
{
    public class ManagedHandler : HttpMessageHandler
    {
        public delegate Task<Stream> StreamOpener(string host, int port, CancellationToken cancellationToken);
        public delegate Task<Socket> SocketOpener(string host, int port, CancellationToken cancellationToken);

        public ManagedHandler()
        {
            _socketOpener = TCPSocketOpenerAsync;
        }

        public ManagedHandler(StreamOpener opener)
        {
            _streamOpener = opener ?? throw new ArgumentNullException(nameof(opener));
        }

        public ManagedHandler(SocketOpener opener)
        {
            _socketOpener = opener ?? throw new ArgumentNullException(nameof(opener));
        }

        public IWebProxy Proxy
        {
            get
            {
                if (_proxy == null)
                {
                    _proxy = WebRequest.DefaultWebProxy;
                }
                return _proxy;
            }
            set
            {
                _proxy = value;
            }
        }

        public bool UseProxy { get; set; } = true;

        public int MaxAutomaticRedirects { get; set; } = 20;

        public RedirectMode RedirectMode { get; set; } = RedirectMode.NoDowngrade;

        public X509CertificateCollection ClientCertificates { get; set; }

        public RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }

        private StreamOpener _streamOpener;
        private SocketOpener _socketOpener;
        private IWebProxy _proxy;

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            HttpResponseMessage response = null;
            int redirectCount = 0;
            bool retry;

            do
            {
                retry = false;
                response = await ProcessRequestAsync(request, cancellationToken);
                if (redirectCount < MaxAutomaticRedirects && IsAllowedRedirectResponse(request, response))
                {
                    redirectCount++;
                    retry = true;
                }

            } while (retry);

            return response;
        }

        private bool IsAllowedRedirectResponse(HttpRequestMessage request, HttpResponseMessage response)
        {
            // Are redirects enabled?
            if (RedirectMode == RedirectMode.None)
            {
                return false;
            }

            // Status codes 301 and 302
            if (response.StatusCode != HttpStatusCode.Redirect && response.StatusCode != HttpStatusCode.Moved)
            {
                return false;
            }

            Uri location = response.Headers.Location;

            if (location == null)
            {
                return false;
            }

            if (!location.IsAbsoluteUri)
            {
                request.RequestUri = location;
                request.SetPathAndQueryProperty(null);
                request.SetAddressLineProperty(null);
                request.Headers.Authorization = null;
                return true;
            }

            // Check if redirect from https to http is allowed
            if (request.IsHttps() && string.Equals("http", location.Scheme, StringComparison.OrdinalIgnoreCase)
                && RedirectMode == RedirectMode.NoDowngrade)
            {
                return false;
            }

            // Reset fields calculated from the URI.
            request.RequestUri = location;
            request.SetSchemeProperty(null);
            request.Headers.Host = null;
            request.Headers.Authorization = null;
            request.SetHostProperty(null);
            request.SetConnectionHostProperty(null);
            request.SetPortProperty(null);
            request.SetConnectionPortProperty(null);
            request.SetPathAndQueryProperty(null);
            request.SetAddressLineProperty(null);
            return true;
        }

        private async Task<HttpResponseMessage> ProcessRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ProcessUrl(request);
            ProcessHostHeader(request);
            request.Headers.ConnectionClose = true; // TODO: Connection re-use is not supported.

            ProxyMode proxyMode = DetermineProxyModeAndAddressLine(request);
            Socket socket;
            Stream transport;
            try
            {
                if (_socketOpener != null)
                {
                    socket = await _socketOpener(request.GetConnectionHostProperty(), request.GetConnectionPortProperty().Value, cancellationToken).ConfigureAwait(false);
                    transport = new NetworkStream(socket, true);
                }
                else
                {
                    socket = null;
                    transport = await _streamOpener(request.GetConnectionHostProperty(), request.GetConnectionPortProperty().Value, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (SocketException sox)
            {
                throw new HttpRequestException("Connection failed", sox);
            }

            if (proxyMode == ProxyMode.Tunnel)
            {
                await TunnelThroughProxyAsync(request, transport, cancellationToken);
            }

            System.Diagnostics.Debug.Assert(!(proxyMode == ProxyMode.Http && request.IsHttps()));

            if (request.IsHttps())
            {
                SslStream sslStream = new SslStream(transport, false, ServerCertificateValidationCallback);
                await sslStream.AuthenticateAsClientAsync(request.GetHostProperty(), ClientCertificates, SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false);
                transport = sslStream;
            }

            var bufferedReadStream = new BufferedReadStream(transport, socket);
            var connection = new HttpConnection(bufferedReadStream);
            return await connection.SendAsync(request, cancellationToken);
        }

        // Data comes from either the request.RequestUri or from the request.Properties
        private void ProcessUrl(HttpRequestMessage request)
        {
            string scheme = request.GetSchemeProperty();
            if (string.IsNullOrWhiteSpace(scheme))
            {
                if (!request.RequestUri.IsAbsoluteUri)
                {
                    throw new InvalidOperationException("Missing URL Scheme");
                }
                scheme = request.RequestUri.Scheme;
                request.SetSchemeProperty(scheme);
            }
            if (!(request.IsHttp() || request.IsHttps()))
            {
                throw new InvalidOperationException("Only HTTP or HTTPS are supported, not: " + request.RequestUri.Scheme);
            }

            string host = request.GetHostProperty();
            if (string.IsNullOrWhiteSpace(host))
            {
                if (!request.RequestUri.IsAbsoluteUri)
                {
                    throw new InvalidOperationException("Missing URL Scheme");
                }
                host = request.RequestUri.DnsSafeHost;
                request.SetHostProperty(host);
            }
            string connectionHost = request.GetConnectionHostProperty();
            if (string.IsNullOrWhiteSpace(connectionHost))
            {
                request.SetConnectionHostProperty(host);
            }

            int? port = request.GetPortProperty();
            if (!port.HasValue)
            {
                if (!request.RequestUri.IsAbsoluteUri)
                {
                    throw new InvalidOperationException("Missing URL Scheme");
                }
                port = request.RequestUri.Port;
                request.SetPortProperty(port);
            }
            int? connectionPort = request.GetConnectionPortProperty();
            if (!connectionPort.HasValue)
            {
                request.SetConnectionPortProperty(port);
            }

            string pathAndQuery = request.GetPathAndQueryProperty();
            if (string.IsNullOrWhiteSpace(pathAndQuery))
            {
                if (request.RequestUri.IsAbsoluteUri)
                {
                    pathAndQuery = request.RequestUri.PathAndQuery;
                }
                else
                {
                    pathAndQuery = Uri.EscapeUriString(request.RequestUri.ToString());
                }
                request.SetPathAndQueryProperty(pathAndQuery);
            }
        }

        private void ProcessHostHeader(HttpRequestMessage request)
        {
            if (string.IsNullOrWhiteSpace(request.Headers.Host))
            {
                string host = request.GetHostProperty();
                int port = request.GetPortProperty().Value;
                if (host.Contains(':'))
                {
                    // IPv6
                    host = '[' + host + ']';
                }

                request.Headers.Host = host + ":" + port.ToString(CultureInfo.InvariantCulture);
            }
        }

        private ProxyMode DetermineProxyModeAndAddressLine(HttpRequestMessage request)
        {
            string scheme = request.GetSchemeProperty();
            string host = request.GetHostProperty();
            int? port = request.GetPortProperty();
            string pathAndQuery = request.GetPathAndQueryProperty();
            string addressLine = request.GetAddressLineProperty();

            if (string.IsNullOrEmpty(addressLine))
            {
                request.SetAddressLineProperty(pathAndQuery);
            }

            try
            {
                if (!UseProxy || (Proxy == null) || Proxy.IsBypassed(request.RequestUri))
                {
                    return ProxyMode.None;
                }
            }
            catch (System.PlatformNotSupportedException)
            {
                return ProxyMode.None;
            }

            var proxyUri = Proxy.GetProxy(request.RequestUri);
            if (proxyUri == null)
            {
                return ProxyMode.None;
            }

            if (request.IsHttp())
            {
                if (string.IsNullOrEmpty(addressLine))
                {
                    addressLine = scheme + "://" + host + ":" + port.Value + pathAndQuery;
                    request.SetAddressLineProperty(addressLine);
                }
                request.SetConnectionHostProperty(proxyUri.DnsSafeHost);
                request.SetConnectionPortProperty(proxyUri.Port);
                return ProxyMode.Http;
            }
            // Tunneling generates a completely seperate request, don't alter the original, just the connection address.
            request.SetConnectionHostProperty(proxyUri.DnsSafeHost);
            request.SetConnectionPortProperty(proxyUri.Port);
            return ProxyMode.Tunnel;
        }

        private static async Task<Socket> TCPSocketOpenerAsync(string host, int port, CancellationToken cancellationToken)
        {
            var addresses = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
            if (addresses.Length == 0)
            {
                throw new Exception($"could not resolve address for {host}");
            }

            Socket connectedSocket = null;
            Exception lastException = null;
            foreach (var address in addresses)
            {
                var s = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
#if (NETSTANDARD1_3 || NETSTANDARD1_6 || NETSTANDARD2_0)
                    await s.ConnectAsync(address, port).ConfigureAwait(false);
#else
                    await Task.Factory.FromAsync(
                        s.BeginConnect,
                        s.EndConnect,
                        new IPEndPoint(address, port),
                        null
                    ).ConfigureAwait(false);
#endif
                    connectedSocket = s;
                    break;
                }
                catch (Exception e)
                {
                    s.Dispose();
                    lastException = e;
                }
            }

            if (connectedSocket == null)
            {
                throw lastException;
            }

            return connectedSocket;
        }

        private async Task TunnelThroughProxyAsync(HttpRequestMessage request, Stream transport, CancellationToken cancellationToken)
        {
            // Send a Connect request:
            // CONNECT server.example.com:80 HTTP / 1.1
            // Host: server.example.com:80
            var connectRequest = new HttpRequestMessage();
            connectRequest.Version = new Version(1, 1);

            connectRequest.Headers.ProxyAuthorization = request.Headers.ProxyAuthorization;
            connectRequest.Method = new HttpMethod("CONNECT");
            // TODO: IPv6 hosts
            string authority = request.GetHostProperty() + ":" + request.GetPortProperty().Value;
            connectRequest.SetAddressLineProperty(authority);
            connectRequest.Headers.Host = authority;

            HttpConnection connection = new HttpConnection(new BufferedReadStream(transport, null));
            HttpResponseMessage connectResponse;
            try
            {
                connectResponse = await connection.SendAsync(connectRequest, cancellationToken);
                // TODO:? await connectResponse.Content.LoadIntoBufferAsync(); // Drain any body
                // There's no danger of accidently consuming real response data because the real request hasn't been sent yet.
            }
            catch (Exception ex)
            {
                transport.Dispose();
                throw new HttpRequestException("SSL Tunnel failed to initialize", ex);
            }

            // Listen for a response. Any 2XX is considered success, anything else is considered a failure.
            if ((int)connectResponse.StatusCode < 200 || 300 <= (int)connectResponse.StatusCode)
            {
                transport.Dispose();
                throw new HttpRequestException("Failed to negotiate the proxy tunnel: " + connectResponse.ToString());
            }
        }
    }
}
