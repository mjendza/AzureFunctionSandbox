using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AzureFunctions.AcceptanceTest.Runner;
using CustomerFunctions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;

namespace AcceptanceTests
{
    [TestClass]
    public class CustomerAcceptanceTests : BaseFunctionAcceptanceTests
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