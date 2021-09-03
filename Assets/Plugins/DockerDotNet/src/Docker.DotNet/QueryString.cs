using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Docker.DotNet
{
    internal class QueryString<T> : IQueryString
    {
        private T Object { get; }

        private Tuple<PropertyInfo, QueryStringParameterAttribute>[] AttributedPublicProperties { get; }

        private IQueryStringConverterInstanceFactory QueryStringConverterInstanceFactory { get; }

        public QueryString(T value)
        {
            if (EqualityComparer<T>.Default.Equals(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            this.Object = value;
            this.QueryStringConverterInstanceFactory = new QueryStringConverterInstanceFactory();
            this.AttributedPublicProperties = FindAttributedPublicProperties<T, QueryStringParameterAttribute>();
        }

        public IDictionary<string, string[]> GetKeyValuePairs()
        {
            var queryParameters = new Dictionary<string, string[]>();
            foreach (var pair in this.AttributedPublicProperties)
            {
                var property = pair.Item1;
                var attribute = pair.Item2;
                var value = property.GetValue(this.Object, null);

                // 'Required' check
                if (attribute.IsRequired && value == null)
                {
                    string propertyFullName = $"{property.GetType().FullName}.{property.Name}";
                    throw new ArgumentException("Got null/unset value for a required query parameter.", propertyFullName);
                }

                // Serialize
                if (attribute.IsRequired || !IsDefaultOfType(value))
                {
                    var keyStr = attribute.Name;
                    string[] valueStr;
                    if (attribute.ConverterType == null)
                    {
                        valueStr = new[] { value.ToString() };
                    }
                    else
                    {
                        var converter = this.QueryStringConverterInstanceFactory.GetConverterInstance(attribute.ConverterType);
                        valueStr = this.ConvertValue(converter, value);

                        if (valueStr == null)
                        {
                            throw new InvalidOperationException($"Got null from value converter '{attribute.ConverterType.FullName}'");
                        }
                    }

                    queryParameters[keyStr] = valueStr;
                }
            }

            return queryParameters;
        }

        /// <summary>
        /// Returns formatted query string.
        /// </summary>
        /// <returns></returns>
        public string GetQueryString()
        {
            return string.Join("&",
                GetKeyValuePairs().Select(
                    pair => string.Join("&",
                        pair.Value.Select(
                            v => $"{Uri.EscapeUriString(pair.Key)}={Uri.EscapeDataString(v)}"))));
        }

        private string[] ConvertValue(IQueryStringConverter converter, object value)
        {
            if (!converter.CanConvert(value.GetType()))
            {
                throw new InvalidOperationException(
                    $"Cannot convert type {value.GetType().FullName} using {converter.GetType().FullName}.");
            }
            return converter.Convert(value);
        }

        private Tuple<PropertyInfo, TAttribType>[] FindAttributedPublicProperties<TValue, TAttribType>() where TAttribType : Attribute
        {
            var t = typeof(TValue);
            var ofAttributeType = typeof(TAttribType);

            var properties = t.GetProperties();
            var publicProperties = properties.Where(p => p.GetGetMethod(false).IsPublic);
            if (!publicProperties.Any())
            {
                throw new InvalidOperationException($"No public property getters found on type {t.FullName}.");
            }

            var attributedPublicProperties = properties.Where(p => p.GetCustomAttribute<TAttribType>() != null).ToArray();
            if (!attributedPublicProperties.Any())
            {
                throw new InvalidOperationException(
                    $"No public properties attributed with [{ofAttributeType.FullName}] found on type {t.FullName}.");
            }

            return attributedPublicProperties.Select(pi =>
                new Tuple<PropertyInfo, TAttribType>(pi, pi.GetCustomAttribute<TAttribType>())).ToArray();
        }

        private static bool IsDefaultOfType(object o)
        {
            if (o is ValueType)
            {
                return o.Equals(Activator.CreateInstance(o.GetType()));
            }

            return o == null;
        }
    }

    /// <summary>
    /// Generates query string formatted as:
    /// [url]?key=value1&key=value2&key=value3...
    /// </summary>
    internal class EnumerableQueryString : IQueryString
    {
        private readonly string _key;
        private readonly string[] _data;

        public EnumerableQueryString(string key, string[] data)
        {
            _key = key;
            _data = data;
        }

        /// <summary>
        /// Returns formatted query string.
        /// </summary>
        /// <returns></returns>
        public string GetQueryString()
        {
            return string.Join("&",
                        _data.Select(
                            v => $"{Uri.EscapeUriString(_key)}={Uri.EscapeDataString(v)}"));
        }
    }
}