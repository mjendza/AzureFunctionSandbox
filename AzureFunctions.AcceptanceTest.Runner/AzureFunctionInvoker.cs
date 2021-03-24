using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ExpressionTreeToString;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
                case "localhost":
                {
                    var key = GetConfigurationUrlKeyNameForFunction(func);
                    var url = config[key];
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

        private static string GetConfigurationUrlKeyNameForFunction(Expression<Func<HttpRequest, Task<IActionResult>>> func)
        {
            var asString = func.ToString("DebugView");

            var regex = new Regex(@".Call(.*)\(");
            var v = regex.Match(asString);
            if (v.Groups.Count != 2)
            {
                throw new ArgumentException("Can't find the calling Azure Function handler.");
            }
            var s = v.Groups[1].ToString();
            var names = s.Split(".");
            if (names.Count() < 2)
            {
                throw new ArgumentException("Can't find the calling Azure Function handler.");
            }

            var namesReverted = names.Reverse().ToArray();
            var methodName = namesReverted.First();
            var typeYouWant = FindType(namesReverted.Skip(1).First());

            if (typeYouWant == null)
            {
                throw new ArgumentException("There is no type for handler.Please create a PR with suggestion how to fix it.");
            }

            var name = typeYouWant.GetMethod(methodName).GetCustomAttribute<FunctionNameAttribute>();

            return name.Name;
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
            if (!string.IsNullOrEmpty(text))
            {
                request.AddParameter("application/json", text, ParameterType.RequestBody);
            }
            if (data.Query.Count > 0)
            {
                foreach (var item in data.Query)
                {
                    request.AddQueryParameter(item.Key, item.Value);
                }

            }
            var result = await client.ExecuteAsync(request);
            if (!result.IsSuccessful)
            {
                throw new Exception(result.StatusCode.ToString());
            }
            return new JsonResult(JsonConvert.DeserializeObject(result.Content));
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

        private static Type FindType(string fullName) =>
            AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName.EndsWith(fullName));
    }

    public class FunctionParameters
    {
        public string Endpoint { get; set; }
        public string[] Verbs { get; set; }
    }
}
