using Database.Models;

namespace Web.Modules
{
    public class ClusterModule : BaseModule<Cluster>
    {
        public ClusterModule()
        {
            header = "clusters";
            base.Init();
        }
    }
}
