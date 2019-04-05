using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Nancy;
using UnityEngine;

class MyTypeCatalog : ITypeCatalog
{
    public IReadOnlyCollection<Type> GetTypesAssignableTo(Type type, TypeResolveStrategy strategy)
    {
        return (
            from a in AppDomain.CurrentDomain.GetAssemblies()
            where !a.IsDynamic
            from t in a.GetExportedTypes()
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
    public string GetRootPath()
    {
        return Path.Combine(Application.dataPath, "..", "views/");
    }
}

class MyBootstrapper : DefaultNancyBootstrapper
{
    protected override ITypeCatalog TypeCatalog => new MyTypeCatalog();
    protected override IAssemblyCatalog AssemblyCatalog => new MyAssemblyCatalog();
    protected override IRootPathProvider RootPathProvider => new MyRootPathProvider();
}
