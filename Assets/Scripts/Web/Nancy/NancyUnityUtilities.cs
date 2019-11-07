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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Simulator.Database.Services;
using Nancy.Authentication.Forms;
using Nancy.Session;
using Nancy.Cryptography;

namespace Simulator.Web
{
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

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            container.Register<IUserMapper, UserMapper>();
            container.Register<IMapService, MapService>();
            container.Register<IClusterService, ClusterService>();
            container.Register<IVehicleService, VehicleService>();
            container.Register<IDownloadService, DownloadService>();
            container.Register<ISimulationService, SimulationService>();
            container.Register<INotificationService, NotificationService>();
            container.Register<IUserService, UserService>();
            container.Register<ISessionService, SessionService>();
        }

        protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
        {
            if (!string.IsNullOrEmpty(Config.SessionGUID) && context.CurrentUser == null)
            {
                UserMapper mapper = new UserMapper();

                context.CurrentUser = mapper.GetUserFromIdentifier(Guid.Parse(Config.SessionGUID), context);
            }

            base.ConfigureRequestContainer(container, context);
        }

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            pipelines.AfterRequest += ctx =>
            {
                ctx.Response.Headers.Add("Access-Control-Allow-Origin", Config.CloudUrl);
                ctx.Response.Headers.Add("Access-Control-Allow-Methods", "GET,PUT,OPTIONS");
                ctx.Response.Headers.Add("Access-Control-Allow-Headers", "Accept, Origin, Content-type, Keep-Alive, Cache-Control");
                ctx.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
            };

            pipelines.OnError += (ctx, ex) =>
            {
                Debug.LogException(ex);
                return HttpStatusCode.InternalServerError;
            };

            var cryptographyConfiguration = new CryptographyConfiguration(
                new NoEncryptionProvider(),
                new DefaultHmacProvider(new PassphraseKeyGenerator("passphrase", Config.salt)));

            var formsAuthConfiguration = new FormsAuthenticationConfiguration()
            {
                CryptographyConfiguration = cryptographyConfiguration,
                DisableRedirect = true,
                UserMapper = container.Resolve<IUserMapper>(),
            };

            FormsAuthentication.FormsAuthenticationCookieName = "sim";

            FormsAuthentication.Enable(pipelines, formsAuthConfiguration);

            CookieBasedSessions.Enable(pipelines, cryptographyConfiguration);
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
