using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace Docker.DotNet
{
    internal class SecretsOperations : ISecretsOperations
    {
        private readonly DockerClient _client;

        internal SecretsOperations(DockerClient client)
        {
            this._client = client;
        }

        async Task<IList<Secret>> ISecretsOperations.ListAsync(CancellationToken cancellationToken)
        {
            var response = await this._client.MakeRequestAsync(this._client.NoErrorHandlers, HttpMethod.Get, "secrets", cancellationToken).ConfigureAwait(false);
            return this._client.JsonSerializer.DeserializeObject<IList<Secret>>(response.Body);
        }

        async Task<SecretCreateResponse> ISecretsOperations.CreateAsync(SecretSpec body, CancellationToken cancellationToken)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            var data = new JsonRequestContent<SecretSpec>(body, this._client.JsonSerializer);
            var response = await this._client.MakeRequestAsync(this._client.NoErrorHandlers, HttpMethod.Post, "secrets/create", null, data, cancellationToken).ConfigureAwait(false);
            return this._client.JsonSerializer.DeserializeObject<SecretCreateResponse>(response.Body);
        }

        async Task<Secret> ISecretsOperations.InspectAsync(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            var response = await this._client.MakeRequestAsync(this._client.NoErrorHandlers, HttpMethod.Get, $"secrets/{id}", cancellationToken).ConfigureAwait(false);
            return this._client.JsonSerializer.DeserializeObject<Secret>(response.Body);
        }

        Task ISecretsOperations.DeleteAsync(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            return this._client.MakeRequestAsync(this._client.NoErrorHandlers, HttpMethod.Delete, $"secrets/{id}", cancellationToken);
        }
    }
}