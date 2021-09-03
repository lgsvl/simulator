using System;

namespace Docker.DotNet
{
    internal interface IQueryStringConverterInstanceFactory
    {
        IQueryStringConverter GetConverterInstance(Type t);
    }
}