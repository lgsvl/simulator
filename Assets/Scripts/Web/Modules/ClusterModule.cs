using Database;
using Nancy;
using Nancy.ModelBinding;
using System;
using UnityEngine;

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
        public ClusterModule() : base("clusters")
        {
            base.Init();
        }

        protected override void Update()
        {
            Put("/{id}", x =>
            {
                try
                {
                    using (var db = DatabaseManager.Open())
                    {
                        int id = x.id;
                        if (id == 0)
                        {
                            throw new Exception("Cannot edit default cluster");
                        }

                        var boundObj = this.Bind<ClusterRequest>();
                        Cluster model = ConvertToModel(boundObj);
                        model.Id = x.id;

                        int result = db.Update(model);
                        if (result > 1)
                        {
                            throw new Exception($"more than one object has id {model.Id}");
                        }

                        if (result < 1)
                        {
                            throw new IndexOutOfRangeException($"id {x.id} does not exist");
                        }

                        model.Status = "Valid";

                        Debug.Log($"Updating {typeof(Cluster).ToString()} with id {model.Id}");
                        return ConvertToResponse(model);
                    }
                }
                catch (IndexOutOfRangeException ex)
                {
                    Debug.Log($"Failed to update {typeof(Cluster).ToString()}: {ex.Message}.");
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to update {typeof(Cluster).ToString()}: {ex.Message}."
                    }, HttpStatusCode.NotFound);
                    return r;
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to update {typeof(Cluster).ToString()}");
                    Debug.LogException(ex);
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to update {typeof(Cluster).ToString()}: {ex.Message}."
                    }, HttpStatusCode.InternalServerError);
                    return r;
                }
            });
        }

        protected override void Remove()
        {
            Delete("/{id}", x =>
            {
                try
                {
                    using (var db = DatabaseManager.Open())
                    {
                        int id = x.id;
                        if(id == 0)
                        {
                            throw new Exception("Cannot remove default cluster");
                        }

                        int result = db.Delete<Cluster>(id);
                        if (result > 1)
                        {
                            throw new Exception($"more than one object has id {id}");
                        }

                        if (result < 1)
                        {
                            throw new IndexOutOfRangeException($"id {x.id} does not exist");
                        }

                        Debug.Log($"Removing {typeof(Cluster).ToString()} with id {id}");
                    }

                    return HttpStatusCode.OK;
                }
                catch (IndexOutOfRangeException ex)
                {
                    Debug.Log($"Failed to remove {typeof(Cluster).ToString()}: {ex.Message}.");
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to remove {typeof(Cluster).ToString()}: {ex.Message}."
                    }, HttpStatusCode.NotFound);
                    return r;
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to remove {typeof(Cluster).ToString()}");
                    Debug.LogException(ex);
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to remove {typeof(Cluster).ToString()}: {ex.Message}."
                    }, HttpStatusCode.InternalServerError);
                    return r;
                }
            });
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
