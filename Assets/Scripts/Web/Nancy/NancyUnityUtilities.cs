using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

using Nancy;
using Nancy.Conventions;
using Nancy.Configuration;
using Nancy.TinyIoc;
using Nancy.Bootstrapper;

namespace Web
{
    class MyTypeCatalog : ITypeCatalog
    {

        static Type[] GetExportedTypesSafe(Assembly asm)
        {
            try
            {
                return asm.GetExportedTypes();
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        public IReadOnlyCollection<Type> GetTypesAssignableTo(Type type, TypeResolveStrategy strategy)
        {
            return (
                from a in AppDomain.CurrentDomain.GetAssemblies()
                where !a.IsDynamic
                from t in GetExportedTypesSafe(a)
                where type.IsAssignableFrom(t) && !t.IsAbstract && strategy(t)
                select t
            ).ToArray();
        }
    }

    class MyAssemblyCatalog : IAssemblyCatalog
    {
        public IReadOnlyCollection<Assembly> GetAssemblies()
        {
            return (
                from a in AppDomain.CurrentDomain.GetAssemblies()
                where !a.IsDynamic
                select a
            ).ToArray();
        }
    }

    class MyRootPathProvider : IRootPathProvider
    {
        public string GetRootPath()  => MainMenu.ApplicationRoot;
    }

    class MyBootstrapper : DefaultNancyBootstrapper
    {
        protected override ITypeCatalog TypeCatalog => new MyTypeCatalog();
        protected override IAssemblyCatalog AssemblyCatalog => new MyAssemblyCatalog();
        protected override IRootPathProvider RootPathProvider => new MyRootPathProvider();

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            pipelines.AfterRequest += ctx =>
            {
                ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                ctx.Response.Headers.Add("Access-Control-Allow-Methods", "POST,GET,PUT,DELETE,OPTIONS");
                ctx.Response.Headers.Add("Access-Control-Allow-Headers", "Accept, Origin, Content-type, Keep-Alive, Cache-Control");
            };

            pipelines.OnError += (ctx, ex) =>
            {
                Debug.LogException(ex);
                return HttpStatusCode.InternalServerError;
            };
        }

        public override void Configure(INancyEnvironment environment)
        {
            base.Configure(environment);
            if (Application.isEditor)
            {
                environment.Tracing(enabled: true, displayErrorTraces: true);
            }
        }
    }
}
