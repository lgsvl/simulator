using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ServiceLogsParameters // (main.ServiceLogsParameters)
    {
        [QueryStringParameter("stdout", false, typeof(BoolQueryStringConverter))]
        public bool? ShowStdout { get; set; }

        [QueryStringParameter("stderr", false, typeof(BoolQueryStringConverter))]
        public bool? ShowStderr { get; set; }

        [QueryStringParameter("since", false)]
        public string Since { get; set; }

        [QueryStringParameter("timestamps", false, typeof(BoolQueryStringConverter))]
        public bool? Timestamps { get; set; }

        [QueryStringParameter("follow", false, typeof(BoolQueryStringConverter))]
        public bool? Follow { get; set; }

        [QueryStringParameter("tail", false)]
        public string Tail { get; set; }

        [QueryStringParameter("details", false, typeof(BoolQueryStringConverter))]
        public bool? Details { get; set; }
    }
}
