using System;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace AzureFunctions.AcceptanceTest.Runner
{
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.AspNetCore.Http.Internal;
    using Microsoft.Extensions.Primitives;
    using Newtonsoft.Json;

    public static class AzureFunctionInvoker
    {
        public static async Task<IActionResult> Invoke(Expression<Func<HttpRequest, Task<IActionResult>>> func,
            HttpRequest data)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var config = builder.Build();
            var stage = config["environment"];
            switch (stage)
            {
                case "remote":
                {
                    var url = config["url"];
                    return await RestCall(data, url);
                }
                case "localhost":
                {
                    var url = config["url"];
                    return await RestCall(data, url);
                    //throw new NotImplementedException("Need to implement this feature...");
                }
                case "debug":
                {
                    return await func.Compile().Invoke(data);
                }
                default:
                {
                    throw new ArgumentException("Please set stage variable to run acceptance tests.");
                }
            }
        }

        private static async Task<IActionResult> RestCall(HttpRequest data, string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ApplicationException("Remote call exception url not defined");
            }

            var client = new RestClient(url);
            var request = new RestRequest($"", FromString(data.Method));
            var reader = new StreamReader(data.Body);
            var text = await reader.ReadToEndAsync();
            dynamic jsonResponse = JsonConvert.DeserializeObject(text);
            if (!string.IsNullOrEmpty(text))
            {
                request.AddParameter("application/json", text, ParameterType.RequestBody);
            }

            var result = await client.ExecuteAsync(request);
            if (!result.IsSuccessful)
            {
                throw new Exception(result.StatusCode.ToString());
            }

            return new OkObjectResult(result.Content);
        }

        private static Method FromString(string method)
        {
            switch (method.ToUpper())
            {
                case "POST":
                    return Method.POST;
                case "GET":
                    return Method.GET;
                case "PUT":
                    return Method.PUT;
            }

            throw new NotImplementedException($"Not Supported Method: {method}");
        }
    }

    public class FunctionParameters
    {
        public string Endpoint { get; set; }
        public string[] Verbs { get; set; }
    }

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
