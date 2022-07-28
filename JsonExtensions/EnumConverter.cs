using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonExtensions
{
    public class EnumConverter : StringEnumConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value != null)
            {
                Type type = value.GetType();

                if (type.IsEnum)
                {
                    Type underlyingType = Enum.GetUnderlyingType(type);

                    value = Convert.ChangeType(value, underlyingType);

                    writer.WriteValue(value);
                }
                return;
            }

            base.WriteJson(writer, value, serializer);
        }
    }
}
