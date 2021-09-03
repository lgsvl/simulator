using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Docker.DotNet.Models
{
    internal static class StreamUtil
    {
        internal static async Task MonitorStreamAsync(Task<Stream> streamTask, DockerClient client, CancellationToken cancellationToken, IProgress<string> progress)
        {
            var tcs = new TaskCompletionSource<string>();

            using (var stream = await streamTask)
            using (var reader = new StreamReader(stream, new UTF8Encoding(false)))
            using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
            {
                string line;
                while ((line = await await Task.WhenAny(reader.ReadLineAsync(), tcs.Task)) != null)
                {
                    progress.Report(line);
                }
            }
        }

        internal static async Task MonitorStreamForMessagesAsync<T>(Task<Stream> streamTask, DockerClient client, CancellationToken cancellationToken, IProgress<T> progress)
        {
            var tcs = new TaskCompletionSource<bool>();

            using (var stream = await streamTask)
            using (var reader = new StreamReader(stream, new UTF8Encoding(false)))
            using (var jsonReader = new JsonTextReader(reader) { SupportMultipleContent = true })
            using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
            {
                while (await await Task.WhenAny(jsonReader.ReadAsync(cancellationToken), tcs.Task))
                {
                    var ev = await client.JsonSerializer.Deserialize<T>(jsonReader, cancellationToken);
                    progress.Report(ev);
                }
            }
        }

        internal static async Task MonitorResponseForMessagesAsync<T>(Task<HttpResponseMessage> responseTask, DockerClient client, CancellationToken cancel, IProgress<T> progress)
        {
            using (var response = await responseTask)
            {
                await MonitorStreamForMessagesAsync<T>(response.Content.ReadAsStreamAsync(), client, cancel, progress);
            }
        }
    }
}
