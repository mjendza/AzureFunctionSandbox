using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTests;
using CustomerFunctions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using Moq;

namespace ConsoleApp1
{
    class Program
    {
        public static HttpRequest HttpRequestSetup(Dictionary<string, StringValues> query, string body, string verb)
        {
            var reqMock = new Mock<HttpRequest>();

            reqMock.Setup(req => req.Query).Returns(new QueryCollection(query));
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(body);
            writer.Flush();
            stream.Position = 0;
            reqMock.Setup(req => req.Body).Returns(stream);
            reqMock.Setup(x => x.Method).Returns(verb);
            return reqMock.Object;
        }

        static async Task Main(string[] args)
        { 
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            var query = new Dictionary<string, StringValues>();
            var body = "{\"name\":\"yamada\"}";
            var req = HttpRequestSetup(query, body, "get");
            var logger = TestLogger.Create();
            await AzureFunctionInvoker.Invoke((request) => CustomerFunction.Run(request, logger), req);
        }
    }
}