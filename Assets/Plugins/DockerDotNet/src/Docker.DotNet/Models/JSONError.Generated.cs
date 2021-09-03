using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class JSONError // (jsonmessage.JSONError)
    {
        [DataMember(Name = "code", EmitDefaultValue = false)]
        public long Code { get; set; }

        [DataMember(Name = "message", EmitDefaultValue = false)]
        public string Message { get; set; }
    }
}
