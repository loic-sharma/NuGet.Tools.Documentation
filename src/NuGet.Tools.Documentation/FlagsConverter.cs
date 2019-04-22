using Newtonsoft.Json;
using System;
using System.Linq;

namespace NuGet.Tools.Documentation
{
    // From: https://stackoverflow.com/questions/43143175/c-sharp-json-net-render-flags-enum-as-string-array
    public class FlagConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, Object existingValue, JsonSerializer serializer)
        {
            //If you need to deserialize, fill in the code here
            return null;
        }

        public override void WriteJson(JsonWriter writer, Object value, JsonSerializer serializer)
        {
            if ((int)value == 0)
            {
                writer.WriteRawValue("[]");
                return;
            }

            var flags = value.ToString()
                .Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                .Select(f => $"\"{f}\"");

            writer.WriteRawValue($"[{string.Join(", ", flags)}]");
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}
