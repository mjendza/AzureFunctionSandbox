using Microsoft.Extensions.Logging;
namespace AzureFunctions.AcceptanceTest.Runner
{
    public abstract class BaseFunctionAcceptanceTests
    {
        protected ILogger logger = TestLogger.Create();
    }
}
