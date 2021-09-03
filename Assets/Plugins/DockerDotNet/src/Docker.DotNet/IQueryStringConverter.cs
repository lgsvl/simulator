using System;

namespace Docker.DotNet
{
    internal interface IQueryStringConverter
    {
        bool CanConvert(Type t);

        string[] Convert(object o);
    }
}