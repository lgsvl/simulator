using System.Net.Http;
#if !NETSTANDARD1_6
using System.Security;
#endif

namespace Docker.DotNet.BasicAuth
{
    public class BasicAuthCredentials : Credentials
    {
        private readonly bool _isTls;

        private readonly MaybeSecureString _username;
        private readonly MaybeSecureString _password;

        public override HttpMessageHandler GetHandler(HttpMessageHandler innerHandler)
        {
            return new BasicAuthHandler(_username, _password, innerHandler);
        }

#if !NETSTANDARD1_6
        public BasicAuthCredentials(SecureString username, SecureString password, bool isTls = false)
            : this(new MaybeSecureString(username), new MaybeSecureString(password), isTls)
        {
        }
#endif

        public BasicAuthCredentials(string username, string password, bool isTls = false)
            : this(new MaybeSecureString(username), new MaybeSecureString(password), isTls)
        {
        }

        private BasicAuthCredentials(MaybeSecureString username, MaybeSecureString password, bool isTls)
        {
            _isTls = isTls;
            _username = username;
            _password = password;
        }

        public override bool IsTlsCredentials()
        {
            return _isTls;
        }

        public override void Dispose()
        {
            _username.Dispose();
            _password.Dispose();
        }
    }
}