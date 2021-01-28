using System;
using System.IO;
using System.Threading.Tasks;
using CustomerFunctions.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CustomerFunctions
{
    public static class Function1
    {
        [FunctionName("customer")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var env = Environment.GetEnvironmentVariable("environment");
            log.LogInformation($"environment: {env}");
            string name = req.Query["name"];

            log.LogInformation("POST C# HTTP request.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation($"POST C# HTTP request body {requestBody}.");
            var request = JsonConvert.DeserializeObject<CreateCustomer>(requestBody);
            
            var validator = new CustomerValidator();
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException(validationResult.Errors.ToString());
            }

            var responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}
