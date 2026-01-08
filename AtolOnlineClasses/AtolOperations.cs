using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtolOnlineClasses
{
    public static class AtolOperations
    {
        private static readonly string url = "https://online.atol.ru/possystem/v4";
        private static readonly string getToken = "/getToken";
        private static readonly string sell = "/{0}/sell";
        public static readonly Dictionary<string, SellVatType> АтолСтавкиНДС = new Dictionary<string, SellVatType>()
        {
            {"Без НДС", SellVatType.none },
            {"10%", SellVatType.vat10 },
            {"20%", SellVatType.vat20 },
            {"18%", SellVatType.vat18 },
            {"0%", SellVatType.vat0 },
            {"22%", SellVatType.vat22 },
        };
        private static string SerializeObject(this object request)
        {
            return JsonConvert.SerializeObject(request,
                   Formatting.None,
                   new JsonSerializerSettings
                   {
                       ContractResolver = new CamelCasePropertyNamesContractResolver()
                   });
        }
        public static T DeserializeObject<T>(this string data)
        {
            return JsonConvert.DeserializeObject<T>(data,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
        }
        private static async Task<string> GetToken(string user, string password)
        {
            using var client = new HttpClient(); 
            var request = new HttpRequestMessage(HttpMethod.Post, url + getToken);
            var content = (new TokenRequest { login = user, pass = password }).SerializeObject();
            request.Content = new StringContent(content, Encoding.UTF8, "application/json");
            try
            {
                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var r = (await response.Content.ReadAsStringAsync()).DeserializeObject<TokenResponse>();
                    if (!string.IsNullOrWhiteSpace(r.token))
                        return r.token;
                }
            }
            catch
            {
            }
            return "";
        }
        public static async Task<string> СоздатьЧекПриход(string user, string password, string groupCode, SellRequest sellRequest)
        {
            string token = await GetToken(user, password);
            if (!string.IsNullOrEmpty(token))
            {
                using var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, string.Format(url + sell, groupCode));
                request.Headers.Add("Token", token);
                request.Content = new StringContent(sellRequest.SerializeObject(), Encoding.UTF8, "application/json");
                try
                {
                    var response = await client.SendAsync(request);
                    string responseString = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        var r = responseString.DeserializeObject<SellResponse>();
                        if (r.status == SellResponseStatus.wait)
                            return "";
                        else if ((r.error != null) && (!string.IsNullOrWhiteSpace(r.error.text)))
                            return r.error.text;
                        else
                            return "unknown error : " + responseString;
                    }
                    else
                        return "unknown error : " + responseString;
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
            return "Get Token failed";
        }
    }
}
