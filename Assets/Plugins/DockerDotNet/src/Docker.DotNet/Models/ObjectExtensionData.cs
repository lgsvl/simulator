using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Docker.DotNet.Models
{
    public class ObjectExtensionData
    {
        [JsonExtensionDataAttribute]
        public IDictionary<string, object> ExtensionData { get; set; }
    }
}