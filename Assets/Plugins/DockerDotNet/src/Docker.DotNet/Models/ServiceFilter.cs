using System;
using System.Collections.Generic;
using System.Linq;

namespace Docker.DotNet.Models
{
    public class ServicesListParameters
    {
        [QueryStringParameter("filters", false, typeof(MapQueryStringConverter))]
        public ServiceFilter Filters { get; set; }
    }
    public class ServiceFilter : Dictionary<string, string[]>
    {
        public string[] Id
        {
            get => this["id"];
            set => this["id"] = value;
        }
        public string[] Label
        {
            get => this["label"];
            set => this["label"] = value;
        }
        public ServiceCreationMode[] Mode
        {
            get => this["mode"]?.ToList().Select(m => (ServiceCreationMode)Enum.Parse(typeof(ServiceCreationMode), m)).ToArray();
            set => this["mode"] = value?.Select(m => m.ToString()).ToArray();
        }
        public string[] Name
        {
            get => this["name"];
            set => this["name"] = value;
        }
    }

    public enum ServiceCreationMode
    {
        Replicated,
        Global
    }
}