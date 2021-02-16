using System.Collections.Generic;
using System.Threading.Tasks;
using AzureFunctions.AcceptanceTest.Runner;
using CustomerFunctions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AcceptanceTests
{
    [TestClass]
    public class CustomerAcceptanceTests : BaseFunctionAcceptanceTests
    {
        [TestMethod]
        public async Task WhenCallCustomerGetWithBody_ShouldReturn200_WithResult()
        {
            //given
            var query = new Dictionary<string, StringValues>();
            var body =
                new {
                    name = "yamada",
                };
            var req = HttpRequestFactory.HttpRequestSetup(query, body, "get");

            //when
            var result = await AzureFunctionInvoker.Invoke((request) => CustomerFunction.Run(request, logger), req);

            //then
            var resultObject = (OkObjectResult)result;
            Assert.IsNotNull(resultObject, "Result object as OkObjectResult can't be null");
            Assert.AreEqual("Hello, yamada. This HTTP triggered function executed successfully.", resultObject.Value);
        }

        [TestMethod]
        public async Task WhenCallCustomerGetWithQuery_ShouldReturn200_WithResult()
        {
            //given
            var query = new Dictionary<string, StringValues>();
            query.Add("name", "yamada");
            var req = HttpRequestFactory.HttpRequestSetup(query, null, "get");

            //when
            var result = await AzureFunctionInvoker.Invoke((request) => CustomerFunction.Run(request, logger), req);

            //then
            var resultObject = (OkObjectResult)result;
            Assert.IsNotNull(resultObject, "Result object as OkObjectResult can't be null");
            Assert.AreEqual("Hello, yamada. This HTTP triggered function executed successfully.", resultObject.Value);
        }
    }
}
