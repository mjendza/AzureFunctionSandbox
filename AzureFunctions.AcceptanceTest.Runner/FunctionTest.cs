using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;

namespace AzureFunctions.AcceptanceTest.Runner
{
    using System.Net.Mime;
    using System.Text;
    using Microsoft.AspNetCore.Http.Features;
    using Newtonsoft.Json;

    public abstract class BaseFunctionAcceptanceTests
    {
        protected ILogger logger = TestLogger.Create();


    }
}
