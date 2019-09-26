/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Linq;
using UnityEngine;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using FluentValidation;
using Simulator.Database;
using Simulator.Database.Services;

namespace Simulator.Web.Modules
{
    public class ClusterRequest
    {
        public string name;
        public string[] ips;

        public ClusterModel ToModel(string owner)
        {
            return new ClusterModel()
            {
                Owner = owner,
                Name = name,
                Ips = ips == null ? null : string.Join(",", ips),
            };
        }
    }

    public class ClusterResponse
    {
        public long Id;
        public string Name;
        public string[] Ips;

        public static ClusterResponse Create(ClusterModel cluster)
        {
            return new ClusterResponse()
            {
                Id = cluster.Id,
                Name = cluster.Name,
                Ips = string.IsNullOrEmpty(cluster.Ips) ? Array.Empty<string>() : cluster.Ips.Split(','),
            };
        }
    }

    public class ClusterRequestValidator : AbstractValidator<ClusterRequest>
    {
        public ClusterRequestValidator()
        {
           RuleFor(req => req.name)
                .NotEmpty().WithMessage("You must specify a non-empty name");

            RuleFor(req => req.ips).Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty().WithMessage("You must specify at least one IP")
                .Must(ips => ips.Length == ips.Distinct().Count()).WithMessage("Specified IPs must be unique");
        }
    }

    public class ClustersModule : NancyModule
    {
        public ClustersModule(IClusterService service, IUserService userService) : base("clusters")
        {
            this.RequiresAuthentication();

            Get("/", x =>
            {
                Debug.Log($"Listing cluster");
                try
                {
                    string filter = Request.Query["filter"];
                    int offset = Request.Query["offset"];
                    // TODO: Items per page should be read from personal user settings.
                    //       This value should be independent for each module: maps, vehicles and simulation.
                    //       But for now 5 is just an arbitrary value to ensure that we don't try and Page a count of 0
                    int count = Request.Query["count"] > 0 ? Request.Query["count"] : Config.DefaultPageSize;
                    return service.List(filter, offset, count, this.Context.CurrentUser.Identity.Name).Select(ClusterResponse.Create).ToArray();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to list clusters: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Get("/{id:long}", x =>
            {
                long id = x.id;
                Debug.Log($"Getting cluster with id {id}");
                try
                {
                    var cluster = service.Get(id, this.Context.CurrentUser.Identity.Name);
                    return ClusterResponse.Create(cluster);
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Cluster with id {id} does not exist");
                    return Response.AsJson(new { error = $"Cluster with id {id} does not exist" }, HttpStatusCode.NotFound);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to get cluster with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Post("/", x =>
            {
                Debug.Log($"Adding new cluster");
                try
                {
                    var req = this.BindAndValidate<ClusterRequest>();
                    if (!ModelValidationResult.IsValid)
                    {
                        var message = ModelValidationResult.Errors.First().Value.First().ErrorMessage;
                        Debug.Log($"Validation for adding cluster failed: {message}");
                        return Response.AsJson(new { error = $"Failed to add cluster: {message}" }, HttpStatusCode.BadRequest);
                    }

                    var cluster = req.ToModel(this.Context.CurrentUser.Identity.Name);

                    cluster.Status = "Valid";

                    long id = service.Add(cluster);
                    Debug.Log($"Cluster added with id {id}");
                    cluster.Id = id;
                    SIM.LogWeb(SIM.Web.ClusterAddName, cluster.Name);
                    SIM.LogWeb(SIM.Web.ClusterAddIPS, cluster.Ips);

                    return ClusterResponse.Create(cluster);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to add cluster: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Put("/{id:long}", x =>
            {
                long id = x.id;
                Debug.Log($"Updating cluster with id {id}");

                try
                {
                    if (id == 0)
                    {
                        throw new Exception("Cannot edit default cluster");
                    }

                    var req = this.BindAndValidate<ClusterRequest>();
                    if (!ModelValidationResult.IsValid)
                    {
                        var message = ModelValidationResult.Errors.First().Value.First().ErrorMessage;
                        Debug.Log($"Validation for updating cluster failed: {message}");
                        return Response.AsJson(new { error = $"Failed to update cluster: {message}" }, HttpStatusCode.BadRequest);
                    }

                    var cluster = service.Get(id, this.Context.CurrentUser.Identity.Name);

                    cluster.Name = req.name;
                    cluster.Ips = string.Join(",", req.ips);

                    int result = service.Update(cluster);
                    SIM.LogWeb(SIM.Web.ClusterEditName, cluster.Name);
                    SIM.LogWeb(SIM.Web.ClusterEditIPS, cluster.Ips);
                    if (result > 1)
                    {
                        throw new Exception($"More than one cluster has id {id}");
                    }
                    else if (result < 1)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    cluster.Status = "Valid";

                    return ClusterResponse.Create(cluster);
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Cluster with id {id} does not exist");
                    return Response.AsJson(new { error = $"Cluster with id {id} does not exist" }, HttpStatusCode.NotFound);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to update cluster with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Delete("/{id:long}", x =>
            {
                long id = x.id;
                Debug.Log($"Removing cluster with id {id}");
                try
                {
                    if (id == 0)
                    {
                        throw new Exception("Cannot remove default cluster");
                    }

                    try
                    {
                        var clusterModel = service.Get(id, this.Context.CurrentUser.Identity.Name);
                        SIM.LogWeb(SIM.Web.ClusterDeleteName, clusterModel.Name);
                        SIM.LogWeb(SIM.Web.ClusterDeleteIPS, clusterModel.Ips);
                    }
                    catch
                    { };
                    int result = service.Delete(id, this.Context.CurrentUser.Identity.Name);
                    if (result > 1)
                    {
                        throw new Exception($"More than one cluster has id {id}");
                    }

                    if (result < 1)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    return new { };
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Cluster with id {id} does not exist");
                    return Response.AsJson(new { error = $"Cluster with id {id} does not exist" }, HttpStatusCode.NotFound);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to remove cluster with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });
        }
    }
}
