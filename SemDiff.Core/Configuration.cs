// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using Newtonsoft.Json;
using System;

namespace SemDiff.Core
{
    class Configuration
    {
        [JsonProperty("username")]
        public string Username { get; set; }
        [JsonProperty("authtoken")]
        public string AuthToken { get; set; }
        [JsonProperty("line_ending")]
        [JsonConverter(typeof(LineEndingTypeEnumConverter))]
        public LineEndingType LineEnding { get; set; }
    }
    public enum LineEndingType
    {
        crlf,
        lf
    }
    public class LineEndingTypeEnumConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var enumString = (string)reader.Value;
            enumString = enumString.ToLower();
            if (enumString == "lf")
            {
                return LineEndingType.lf;
            }
            return LineEndingType.crlf;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var lineEnding = (LineEndingType)value;
            switch (lineEnding)
            {
                case LineEndingType.lf:
                    writer.WriteValue("lf");
                    break;
                default:
                    writer.WriteValue("crlf");
                    break;
            }
        }
    }
}
