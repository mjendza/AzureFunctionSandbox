using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CustomerFunctions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;

namespace AcceptanceTests
{
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

    [TestClass]
    public class CustomerAcceptanceTests : FunctionTest
    {
        [TestMethod]
        public async Task WhenCallCustomerGet_ShouldReturn201()
        {
            var query = new Dictionary<string, StringValues>();
            var body = "{\"name\":\"yamada\"}";
            var req = HttpRequestSetup(query, body, "get");
            var result = await AzureFunctionInvoker.Invoke((request) => CustomerFunction.Run(request, logger), req);
            var resultObject = (OkObjectResult) result;
            Assert.AreEqual("Hello, yamada. This HTTP triggered function executed successfully.", resultObject.Value);
        }
    }
}