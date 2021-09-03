using System;
using Newtonsoft.Json;

namespace Docker.DotNet
{
    internal class TimeSpanNanosecondsConverter : JsonConverter
    {
        const int MiliSecondToNanoSecond = 1000000;
        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            var timeSpan = value as TimeSpan?;
            if (timeSpan == null)
            {
                return;
            }

            writer.WriteValue((long)(timeSpan.Value.TotalMilliseconds * MiliSecondToNanoSecond));
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TimeSpan) || objectType == typeof(TimeSpan?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            var valueInNanoSeconds = (long?)reader.Value;

            if (!valueInNanoSeconds.HasValue)
            {
                return null;
            }
            var miliSecondValue = valueInNanoSeconds.Value / MiliSecondToNanoSecond;

            return TimeSpan.FromMilliseconds((long)miliSecondValue);
        }
    }
}