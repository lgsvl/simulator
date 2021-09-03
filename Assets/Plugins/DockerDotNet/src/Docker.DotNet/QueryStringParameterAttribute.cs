using System;
using System.Linq;
#if (NETSTANDARD1_3 || NETSTANDARD1_6 || NETSTANDARD2_0)
using System.Reflection;
#endif

namespace Docker.DotNet
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class QueryStringParameterAttribute : Attribute
    {
        public string Name { get; private set; }

        public bool IsRequired { get; private set; }

        public Type ConverterType { get; private set; }

        public QueryStringParameterAttribute(string name, bool required) : this(name, required, null)
        {
        }

        public QueryStringParameterAttribute(string name, bool required, Type converterType)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (converterType != null && !converterType.GetInterfaces().Contains(typeof (IQueryStringConverter)))
            {
                throw new ArgumentException($"Provided query string converter type is not {typeof(IQueryStringConverter).FullName}", nameof(converterType));
            }

            this.Name = name;
            this.IsRequired = required;
            this.ConverterType = converterType;
        }
    }
}