using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonExtensions
{
    public static class Extensions
    {
        public static string SerializeObject(this object request)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(request,
                   Newtonsoft.Json.Formatting.None,
                   new Newtonsoft.Json.JsonSerializerSettings
                   {
                       ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                   });
        }
        public static T DeserializeObject<T>(this byte[] data)
        {
            //string json = System.IO.File.ReadAllText(@"f:\tmp\31\text.txt", Encoding.UTF8);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data),//(json, //
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                });
        }
    }
}
