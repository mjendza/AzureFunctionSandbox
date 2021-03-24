namespace AzureFunctions.AcceptanceTest.Runner
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.AspNetCore.Http.Internal;
    using Microsoft.Extensions.Primitives;
    using Newtonsoft.Json;

    public static class HttpRequestFactory
    {
        public static HttpRequest HttpRequestSetup(Dictionary<string, StringValues> queryData, object body, string verb)
        {
            var queryCollection = new QueryCollection(queryData);
            var query = new QueryFeature(queryCollection);

            var features = new FeatureCollection();
            features.Set<IQueryFeature>(query);
            var request = new HttpRequestFeature()
            {
                Method = verb,

            };
            if (body != null)
            {
                var json = JsonConvert.SerializeObject(body);
                request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
            }

            features.Set<IHttpRequestFeature>(request);

            var httpContext = new DefaultHttpContext(features);
            return httpContext.Request;
        }
    }
}
