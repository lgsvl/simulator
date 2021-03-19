/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Sensors.Postprocessing
{
    using System;
    using System.Collections.Generic;
    using JetBrains.Annotations;
    using Newtonsoft.Json.Linq;

    [AttributeUsage(AttributeTargets.Class)]
    public class DefaultPostprocessingAttribute : Attribute
    {
        public const string PostprocessingFieldName = nameof(CameraSensorBase.Postprocessing);

        private readonly List<PostProcessData> defaultInstances;

        public List<PostProcessData> GetDefaultInstances => new List<PostProcessData>(defaultInstances);

        public DefaultPostprocessingAttribute([NotNull] params Type[] types)
        {
            defaultInstances = new List<PostProcessData>();
            var ppDataType = typeof(PostProcessData);
            
            foreach (var type in types)
            {
                if (!ppDataType.IsAssignableFrom(type))
                    throw new Exception($"{nameof(DefaultPostprocessingAttribute)} can only specify types derived from {nameof(PostProcessData)}. {type.Name} is invalid");

                var instance = Activator.CreateInstance(type) as PostProcessData;
                if (instance == null)
                    throw new Exception($"Unable to instantiate {type.Name} as {nameof(PostProcessData)} with parameterless constructor.");

                defaultInstances.Add(instance);
            }
        }

        public JToken GetDefaultInstanceJToken()
        {
            var jArr = new JArray();

            foreach (var instance in defaultInstances)
            {
                var jObj = JObject.FromObject(instance);
                jObj.AddFirst(new JProperty("type", instance.GetType().Name));
                jArr.Add(jObj);
            }

            return jArr;
        }

        public static JToken GetEmptyInstanceJToken()
        {
            return JToken.Parse("[]");
        }
    }
}