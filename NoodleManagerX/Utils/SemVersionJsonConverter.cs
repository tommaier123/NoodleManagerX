using Newtonsoft.Json;
using Semver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Utils
{
    public class SemVersionJsonConverter : JsonConverter<SemVersion>
    {
        public override SemVersion ReadJson(JsonReader reader, Type objectType, SemVersion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string s = (string)reader.Value;
            var parsed = SemVersion.Parse(s, SemVersionStyles.OptionalMinorPatch);
            return parsed;
        }

        public override void WriteJson(JsonWriter writer, SemVersion value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
