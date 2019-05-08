/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Nancy;
using Nancy.Bootstrapper;
using Nancy.Configuration;
using Nancy.Diagnostics;
using Nancy.TinyIoc;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace Simulator.Web
{
    public static class Config
    {
        public static string Root;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        static void Initialize()
        {
            Root = Path.Combine(Application.dataPath, "..");
        }
    }

    class UnityTypeCatalog : ITypeCatalog
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
            var types = (
                from a in AppDomain.CurrentDomain.GetAssemblies()
                where !a.IsDynamic
                from t in GetExportedTypesSafe(a)
                where type.IsAssignableFrom(t) && !t.IsAbstract && !t.ContainsGenericParameters && strategy(t)
                select t
            ).ToArray();
            return types;
        }
    }

    class UnityAssemblyCatalog : IAssemblyCatalog
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

    public class UnityRootPathProvider : IRootPathProvider
    {
        public string GetRootPath() => Config.Root;
    }

    public class UnityBootstrapper : DefaultNancyBootstrapper
    {
        protected override ITypeCatalog TypeCatalog => new UnityTypeCatalog();
        protected override IAssemblyCatalog AssemblyCatalog => new UnityAssemblyCatalog();
        protected override IRootPathProvider RootPathProvider => new UnityRootPathProvider();

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

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
                environment.Diagnostics(password: "simulator");
            }
        }
    }
}
