# SpecGen

A tool that reflects the Docker client [engine-api](https://github.com/docker/engine-api) in order to generate C# classes that match its model for [Docker.DotNet Models](/src/Docker.DotNet/Models).

----

## How to use:

To update the source repositories please use the following from your `$GOPATH`:

```
> go get -u github.com/docker/docker@<release-tag>
```

Note: Since the docker library is not a go module the version go generates will look something like this  v17.12.0-ce-rc1.0.20200916142827-bd33bbf0497b+incompatible even though this is for v19.03.13. The commit hash bd33bbf0497b matches the commit hash of docker v 19.03.13

Once you have the latest engine-api. Calling:

```
> update-generated-code.cmd
```

Should result in changes to the Docker.DotNet/Models directory if any exist.

----

## About the structure of the tool:

Many of Docker's engine-api types are used for both the query string and json body. Because there is no way to attribute this on the engine-api types themselves we have broken the tool into a few specific areas:

`Csharptype.go`: Contains the translation/serialization code for writing the C# classes.

`Modeldefs.go` : Contains the parts of engine-api that are used as parameters or require custom serialization that needs to be explicitly handled differently.

`Specgen.go`   : Contains the majority of the code that reflects the engine-api structs and converts them to the C# in-memory abstractions.

----

## About the structure of the output:

The resulting C# type contains both the `QueryString` parameters as well as the `JSON` body models in one object. This simplifies the calling API quite dramatically. For example:

```C#
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ContainerAttachParameters // (main.ContainerAttachParameters)
    {
        [QueryStringParameter("stream", false, typeof(BoolQueryStringConverter))]
        public bool? Stream { get; set; }

        [QueryStringParameter("stdin", false, typeof(BoolQueryStringConverter))]
        public bool? Stdin { get; set; }
    }
}
```

What you are seeing here is that in order to interact with the remote API the query string allows `optional` `stream` and `stdin` boolean parameters. Because they are optional the generated code adds the `?` to signify the absence of the value versus passing a `false` as the value.

```C#
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class Config // (container.Config)
    {
        [DataMember(Name = "Hostname", EmitDefaultValue = false)]
        public string Hostname { get; set; }

        [DataMember(Name = "Domainname", EmitDefaultValue = false)]
        public string Domainname { get; set; }
        
        // etc...
    }
}
```

Here you are actually seeing that the field values are marshalled in the request body based on the `DataMember` attribute. The resulting `JSON` will not contain the field if its value is equal to its default value in C#.

A few customizations are taken in order to simplify the API even more. Take for example [RestartPolicyKind.cs](https://github.com/ahmetalpbalkan/Docker.DotNet/blob/master/Docker.DotNet/Models/RestartPolicyKind.cs). You will see the generated model contains: 

```C#
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class RestartPolicy // (container.RestartPolicy)
    {
        [DataMember(Name = "Name", EmitDefaultValue = false)]
        public RestartPolicyKind Name { get; set; }

        [DataMember(Name = "MaximumRetryCount", EmitDefaultValue = false)]
        public long MaximumRetryCount { get; set; }
    }
}
```

The property `Name` actually uses the enum value instead of its integer value. In order to do this because Go does not have enum values if you look at `specgen.go` you will see a `typeCustomizations` map where this field has been explicitly overridden in how its generated. You can use this model to accomplish more of the same where you see fit.
