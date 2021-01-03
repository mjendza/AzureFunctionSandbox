using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CustomerFunctions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace AcceptanceTests
{
    public static class TestLogger
    {
        public static ILogger Create()
        {
            var logger = new ConsoleUnitLogger();
            return logger;
        }

        class ConsoleUnitLogger : ILogger, IDisposable
        {
            private readonly Action<string> output = Console.WriteLine;

            public void Dispose()
            {
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter) => output(formatter(state, exception));

            public bool IsEnabled(LogLevel logLevel) => true;

            public IDisposable BeginScope<TState>(TState state) => this;
        }
    }
    public abstract class FunctionTest
    {

        protected ILogger logger = TestLogger.Create();

        public HttpRequest HttpRequestSetup(Dictionary<string, StringValues> query, string body)
        {
            var reqMock = new Mock<HttpRequest>();

            reqMock.Setup(req => req.Query).Returns(new QueryCollection(query));
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(body);
            writer.Flush();
            stream.Position = 0;
            reqMock.Setup(req => req.Body).Returns(stream);
            return reqMock.Object;
        }

    }

    public class AsyncCollector<T> : IAsyncCollector<T>
    {
        public readonly List<T> Items = new List<T>();

        public Task AddAsync(T item, CancellationToken cancellationToken = default(CancellationToken))
        {

            Items.Add(item);

            return Task.FromResult(true);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(true);
        }

    }

    public static class AzureFunctionInvoker
    {
        public static async Task<IActionResult> Invoke(Func<HttpRequest, Task<IActionResult>> func, params object[] data)
        {
            switch (Environment.GetEnvironmentVariable("stage"))
            {
                case "remote":
                {
                    var url = Environment.GetEnvironmentVariable("url");
                    var client = new RestClient();
                    var request = new RestRequest($"customer", Method.GET);
                    var result = await client.ExecuteAsync(request);
                    return new ActionResult<object>(result.Content).Result;
                }
                case "localhost":
                {
                    var url = $"http://localhost:7071/api/";
                    var client = new RestClient();
                    var request = new RestRequest($"customer", Method.GET);
                    var result = await client.ExecuteAsync(request);
                    return await func(data[0] as HttpRequest);
                }
                case "debug":
                {
                    return await func(data[0] as HttpRequest);
                }
                default:
                {
                    throw new ArgumentException("Please set stage variable to run acceptance tests.");
                }
            }
            throw new ArgumentException("Please set stage variable to run acceptance tests.");
        }
    }
    [TestClass]
    public class CustomerAcceptanceTests: FunctionTest
    {
        [TestMethod]
        public async Task WhenCallCustomerGet_ShouldReturn201()
        {
            var query = new Dictionary<string, StringValues>();
            var body = "{\"name\":\"yamada\"}";
            var req = HttpRequestSetup(query, body);
            var result = await AzureFunctionInvoker.Invoke((request) => CustomerFunction.Run(request, logger), req, logger);
            var resultObject = (OkObjectResult)result;
            Assert.AreEqual("Hello, yamada", resultObject.Value);
        }
    }
}
