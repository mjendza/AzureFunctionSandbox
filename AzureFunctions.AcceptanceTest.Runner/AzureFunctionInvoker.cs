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
                    throw new NotImplementedException("Need to implement this feature...");
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
            var request = new RestRequest($"customer", FromString(data.Method));
            StreamReader reader = new StreamReader(data.Body);
            string text = await reader.ReadToEndAsync();
            request.AddJsonBody(text);
            var result = await client.ExecuteAsync(request);
            return new ActionResult<object>(result.Content).Result;
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
}