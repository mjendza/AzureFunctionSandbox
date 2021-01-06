using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AcceptanceTests
{
    public abstract class FunctionTest
    {
        [ClassInitialize]
        public void ClassSetUp()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        }
        
        [TestInitialize]
        public void TestSetUp()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        }
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