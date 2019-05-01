using Database;

namespace Web.Modules
{
    public class ClusterRequest
    {
        public string name;
        public string[] ips;
    }

    public class ClusterResponse : WebResponse
    {
        public string Name;
        public string[] Ips;
    }

    public class ClusterModule : BaseModule<Cluster, ClusterRequest, ClusterResponse>
    {
        public ClusterModule()
        {
            header = "clusters";
            base.Init();
        }

        protected override Cluster ConvertToModel(ClusterRequest clusterRequest)
        {
            Cluster cluster = new Cluster();
            if (clusterRequest.ips != null && clusterRequest.ips.Length > 0)
            {
                cluster.Ips = string.Join(",", clusterRequest.ips);
            }

            cluster.Name = clusterRequest.name;
            return cluster;
        }

        public override ClusterResponse ConvertToResponse(Cluster cluster)
        {
            ClusterResponse clusterResponse = new ClusterResponse();
            clusterResponse.Id = cluster.Id;
            if (cluster.Ips != null && cluster.Ips.Length > 0)
            {
                clusterResponse.Ips = cluster.Ips.Split(',');
            }

            clusterResponse.Name = cluster.Name;
            return clusterResponse;
        }
    }
}
