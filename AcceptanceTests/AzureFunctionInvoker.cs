using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace AcceptanceTests
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
                    var url = Environment.GetEnvironmentVariable("url");
                    return await RestCall(data, url);
                }
                case "localhost":
                {
                    var url = $"http://localhost:7071/api/";
                    return await RestCall(data, url);
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

        private static FunctionParameters GetParams(Expression<Func<HttpRequest, Task<IActionResult>>> func)
        {
            var debug = GetDebugView(func);
            return new FunctionParameters()
            {
                Endpoint = "customer",
                Verbs = new[] {"get", "post"}
            };
        }

        public static string GetDebugView(Expression exp)
        {
            if (exp == null)
                return null;

            var propertyInfo =
                typeof(Expression).GetProperty("DebugView", BindingFlags.Instance | BindingFlags.NonPublic);
            return propertyInfo.GetValue(exp) as string;
        }
    }

    public class FunctionParameters
    {
        public string Endpoint { get; set; }
        public string[] Verbs { get; set; }
    }
}