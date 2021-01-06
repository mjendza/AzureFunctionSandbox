using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AzureFunctions.AcceptanceTest.Runner
{
    public class AzureFunctionCliInvoker
    {
        private string RunFunction => "func start --build";

        public async Task RunAzureFunction()
        {
            using var hosted = new HostedRunspace();
            var scriptContents = new StringBuilder();
            scriptContents.AppendLine("Param($StrParam, $IntParam)");
            scriptContents.AppendLine("");
            scriptContents.AppendLine("Write-Output \"Starting script\"");
            scriptContents.AppendLine("Write-Output \"This is the value from the first param: $StrParam\"");
            scriptContents.AppendLine("Write-Output \"This is the value from the second param: $IntParam\"");
            scriptContents.AppendLine("");
            scriptContents.AppendLine(@"set-location ..\..\..\..\CustomerFunctions\");
            scriptContents.AppendLine(RunFunction);
            scriptContents.AppendLine("");

            var scriptParameters = new Dictionary<string, object>()
            {
                { "StrParam", "Hello from script" },
                { "IntParam", 7 }
            };
                
            var result = await hosted.RunScript(scriptContents.ToString(), scriptParameters);
            Console.Write($"PowerShell result: {string.Join(Environment.NewLine, result)}");
        } 
    }
}