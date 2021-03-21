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
        public async Task WhenJsonCallCustomerPostWithBody_ShouldReturn200_WithJsonResult()
        {
            //given
            var query = new Dictionary<string, StringValues>();
            var body =
                new {
                    name = "yamada",
                };
            var req = HttpRequestFactory.HttpRequestSetup(query, body, "POST");

            //when
            var result = await AzureFunctionInvoker.Invoke((request) => CustomerFunction.Run(request, logger), req);

            //then
            var resultObject = (JsonResult)result;
            Assert.IsNotNull(resultObject, "Result object as OkObjectResult can't be null");
            dynamic jsonResult = resultObject.Value;
            Assert.AreEqual("Hello, yamada. This HTTP triggered function executed successfully.", jsonResult.value.ToString());
        }

        [TestMethod]
        public async Task WhenCallCustomerGetWithQuery_ShouldReturn200_WithJsonResult()
        {
            //given
            var query = new Dictionary<string, StringValues>();
            query.Add("name", "yamada");
            var req = HttpRequestFactory.HttpRequestSetup(query, null, "GET");

            //when
            var result = await AzureFunctionInvoker.Invoke((request) => CustomerFunction.Run(request, logger), req);

            //then
            var resultObject = (JsonResult)result;
            Assert.IsNotNull(resultObject, "Result object as OkObjectResult can't be null");
            dynamic jsonResult = resultObject.Value;
            Assert.AreEqual("Hello, yamada. This HTTP triggered function executed successfully.", jsonResult.value.ToString());
        }
    }
}
