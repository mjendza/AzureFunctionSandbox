
# Howto use it
## Acceptance test (MSTest) - check the AcceptanceTests project 
```
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
```
## Acceptance test configuration
Based on the convention on **FunctionName** Attribute we use tests configuration file to get the azure function url for remote or localhost call
### Azure Function sample
```
namespace CustomerFunctions
{
    public static class CustomerFunction
    {
        [FunctionName("customer")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
```
### appsetting.json configuration
```
{
    "environment":  "localhost",
    "customer": "http://localhost:7071/api/customer"
}
```

