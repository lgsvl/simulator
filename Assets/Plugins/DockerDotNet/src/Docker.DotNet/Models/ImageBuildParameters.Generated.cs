using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ImageBuildParameters // (main.ImageBuildParameters)
    {
        [QueryStringParameter("t", false, typeof(EnumerableQueryStringConverter))]
        public IList<string> Tags { get; set; }

        [QueryStringParameter("q", false, typeof(BoolQueryStringConverter))]
        public bool? SuppressOutput { get; set; }

        [QueryStringParameter("remote", false)]
        public string RemoteContext { get; set; }

        [QueryStringParameter("nocache", false, typeof(BoolQueryStringConverter))]
        public bool? NoCache { get; set; }

        [QueryStringParameter("rm", false, typeof(BoolQueryStringConverter))]
        public bool? Remove { get; set; }

        [QueryStringParameter("forcerm", false, typeof(BoolQueryStringConverter))]
        public bool? ForceRemove { get; set; }

        [QueryStringParameter("pullparent", false, typeof(BoolQueryStringConverter))]
        public bool? PullParent { get; set; }

        [QueryStringParameter("isolation", false)]
        public string Isolation { get; set; }

        [QueryStringParameter("cpusetcpus", false)]
        public string CPUSetCPUs { get; set; }

        [QueryStringParameter("cpusetmems", false)]
        public string CPUSetMems { get; set; }

        [QueryStringParameter("cpushares", false)]
        public long? CPUShares { get; set; }

        [QueryStringParameter("cpuquota", false)]
        public long? CPUQuota { get; set; }

        [QueryStringParameter("cpuperiod", false)]
        public long? CPUPeriod { get; set; }

        [QueryStringParameter("memory", false)]
        public long? Memory { get; set; }

        [QueryStringParameter("memswap", false)]
        public long? MemorySwap { get; set; }

        [QueryStringParameter("cgroupparent", false)]
        public string CgroupParent { get; set; }

        [QueryStringParameter("networkmode", false)]
        public string NetworkMode { get; set; }

        [QueryStringParameter("shmsize", false)]
        public long? ShmSize { get; set; }

        [QueryStringParameter("dockerfile", false)]
        public string Dockerfile { get; set; }

        [QueryStringParameter("ulimits", false, typeof(EnumerableQueryStringConverter))]
        public IList<Ulimit> Ulimits { get; set; }

        [QueryStringParameter("buildargs", false, typeof(MapQueryStringConverter))]
        public IDictionary<string, string> BuildArgs { get; set; }

        [DataMember(Name = "AuthConfigs", EmitDefaultValue = false)]
        public IDictionary<string, AuthConfig> AuthConfigs { get; set; }

        [QueryStringParameter("labels", false, typeof(MapQueryStringConverter))]
        public IDictionary<string, string> Labels { get; set; }

        [QueryStringParameter("squash", false, typeof(BoolQueryStringConverter))]
        public bool? Squash { get; set; }

        [QueryStringParameter("cachefrom", false, typeof(EnumerableQueryStringConverter))]
        public IList<string> CacheFrom { get; set; }

        [QueryStringParameter("securityopt", false, typeof(EnumerableQueryStringConverter))]
        public IList<string> SecurityOpt { get; set; }

        [QueryStringParameter("extrahosts", false, typeof(EnumerableQueryStringConverter))]
        public IList<string> ExtraHosts { get; set; }

        [QueryStringParameter("target", false)]
        public string Target { get; set; }

        [QueryStringParameter("session", false)]
        public string SessionID { get; set; }

        [QueryStringParameter("platform", false)]
        public string Platform { get; set; }
    }
}
