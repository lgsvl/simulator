using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ImagesCreateParameters // (main.ImagesCreateParameters)
    {
        [QueryStringParameter("fromImage", false)]
        public string FromImage { get; set; }

        [QueryStringParameter("fromSrc", false)]
        public string FromSrc { get; set; }

        [QueryStringParameter("repo", false)]
        public string Repo { get; set; }

        [QueryStringParameter("tag", false)]
        public string Tag { get; set; }
    }
}
