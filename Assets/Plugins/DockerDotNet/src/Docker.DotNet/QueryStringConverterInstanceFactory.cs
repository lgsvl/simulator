using System;
using System.Collections.Concurrent;

namespace Docker.DotNet
{
    internal class QueryStringConverterInstanceFactory : IQueryStringConverterInstanceFactory
    {
        private static readonly ConcurrentDictionary<Type, IQueryStringConverter> ConverterInstanceRegistry = new ConcurrentDictionary<Type, IQueryStringConverter>();

        public IQueryStringConverter GetConverterInstance(Type t)
        {
            return ConverterInstanceRegistry.GetOrAdd(
                t,
                InitializeConverter);
        }

        private IQueryStringConverter InitializeConverter(Type t)
        {
            var instance = Activator.CreateInstance(t) as IQueryStringConverter;
            if (instance == null)
            {
                throw new InvalidOperationException($"Could not get instance of {t.FullName}");
            }
            return instance;
        }
    }
}