﻿using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;

namespace AzureFunctions.AcceptanceTest.Runner
{
    public abstract class BaseFunctionAcceptanceTests
    {
        protected ILogger logger = TestLogger.Create();

        public HttpRequest HttpRequestSetup(Dictionary<string, StringValues> query, string body, string verb)
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
    }
}