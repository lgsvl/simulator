using System;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class JSONMessage // (jsonmessage.JSONMessage)
    {
        [DataMember(Name = "stream", EmitDefaultValue = false)]
        public string Stream { get; set; }

        [DataMember(Name = "status", EmitDefaultValue = false)]
        public string Status { get; set; }

        [DataMember(Name = "progressDetail", EmitDefaultValue = false)]
        public JSONProgress Progress { get; set; }

        [DataMember(Name = "progress", EmitDefaultValue = false)]
        public string ProgressMessage { get; set; }

        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string ID { get; set; }

        [DataMember(Name = "from", EmitDefaultValue = false)]
        public string From { get; set; }

        [DataMember(Name = "time", EmitDefaultValue = false)]
        public DateTime Time { get; set; }

        [DataMember(Name = "timeNano", EmitDefaultValue = false)]
        public long TimeNano { get; set; }

        [DataMember(Name = "errorDetail", EmitDefaultValue = false)]
        public JSONError Error { get; set; }

        [DataMember(Name = "error", EmitDefaultValue = false)]
        public string ErrorMessage { get; set; }

        [DataMember(Name = "aux", EmitDefaultValue = false)]
        public ObjectExtensionData Aux { get; set; }
    }
}
