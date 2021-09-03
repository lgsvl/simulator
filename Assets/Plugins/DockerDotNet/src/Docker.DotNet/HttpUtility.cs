using System;

namespace Docker.DotNet
{
    internal static class HttpUtility
    {
        public static Uri BuildUri(Uri baseUri, Version requestedApiVersion, string path, IQueryString queryString)
        {
            if (baseUri == null)
            {
                throw new ArgumentNullException(nameof(baseUri));
            }

            var builder = new UriBuilder(baseUri);

            if (requestedApiVersion != null)
            {
                builder.Path += $"v{requestedApiVersion}/";
            }

            if (!string.IsNullOrEmpty(path))
            {
                builder.Path += path;
            }

            if (queryString != null)
            {
                builder.Query = queryString.GetQueryString();
            }

            return builder.Uri;
        }
    }
}