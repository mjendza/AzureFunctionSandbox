using System;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RestSharp;
using ExpressionTreeToString;
using Newtonsoft.Json;

namespace AzureFunctions.AcceptanceTest.Runner
{
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.Azure.WebJobs;

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
                    var u = GetUrl(func);
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

        private static string GetUrl(Expression<Func<HttpRequest, Task<IActionResult>>> func)
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
            var assemblyName = names.First();
            var classWithNameSpace = string.Join(".", names.Skip(1));
            var typeYouWant = Type.GetType($"{classWithNameSpace}, {assemblyName}");
            var name = typeYouWant
                .GetAttributeValue((FunctionNameAttribute dna) => dna.Name);

            return name;
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

        private static Type FindType(string fullName)
        {
            return
                AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic)
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName.Equals(fullName));
        }
    }

    public static class AttributeExtensions
    {
        public static TValue GetAttributeValue<TAttribute, TValue>(
            this Type type,
            Func<TAttribute, TValue> valueSelector)
            where TAttribute : Attribute
        {
            var att = type.GetCustomAttributes(
                typeof(TAttribute), true
            ).FirstOrDefault() as TAttribute;
            if (att != null)
            {
                return valueSelector(att);
            }
            return default(TValue);
        }
    }



    public class FunctionParameters
    {
        public string Endpoint { get; set; }
        public string[] Verbs { get; set; }
    }
}
