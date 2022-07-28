using HttpExtensions;

namespace WbClasses
{
    public static class Functions
    {
        public static string ParseError(this string errorText, List<string> response)
        {
            if ((response != null) && (response.Count > 0))
            {
                foreach (var error in response)
                {
                    if (!string.IsNullOrEmpty(errorText))
                        errorText += Environment.NewLine;
                    errorText += error;
                }
            }
            return errorText;
        }
        public static Dictionary<string, string> GetCustomHeaders(string authToken)
        {
            var result = new Dictionary<string, string>();
            result.Add("Authorization", authToken);
            return result;
        }
        public static async Task<Tuple<List<CatalogInfo>?, string?>> GetCatalogInfo(IHttpService httpService, string authToken,
            CancellationToken cancellationToken)
        {
            var result = await httpService.Exchange<List<CatalogInfo>, string>(
                "https://suppliers-api.wildberries.ru/public/api/v1/info",
                HttpMethod.Get,
                GetCustomHeaders(authToken),
                null,
                cancellationToken);
            string err = "";
            if (result.Item2 != null)
                err = ("WbGetCatalogInfoResponse : " + result.Item2);
            if (result.Item1 != null)
            {
                return new(result.Item1, err);
            }
            return new(null, err);
        }
        public static async Task<Tuple<bool, string?>> UpdatePrice(IHttpService httpService, string authToken,
            List<PriceRequest> priceData,
            CancellationToken cancellationToken)
        {
            var result = await httpService.Exchange<bool, List<string>>(
                "https://suppliers-api.wildberries.ru/public/api/v1/prices",
                HttpMethod.Post,
                GetCustomHeaders(authToken),
                priceData,
                cancellationToken);
            if (result.Item2 != null)
            {
                return new(false, "".ParseError(result.Item2));
            }
            return new(result.Item1, null);
        }
    }
}