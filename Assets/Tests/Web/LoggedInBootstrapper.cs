/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Nancy;
using Nancy.Bootstrapper;
using Nancy.Testing;
using Nancy.TinyIoc;
using System;
using System.Security.Claims;
using System.Security.Principal;

public class LoggedInBootstrapper : ConfigurableBootstrapper
{
    public LoggedInBootstrapper(Action<ConfigurableBootstrapperConfigurator> configuration) : base(configuration)
    {

    }

    protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
    {
        base.RequestStartup(container, pipelines, context);

        context.CurrentUser = new ClaimsPrincipal(new GenericIdentity("Test User"));
    }
}
