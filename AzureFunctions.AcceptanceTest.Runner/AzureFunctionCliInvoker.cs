using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AzureFunctions.AcceptanceTest.Runner
{
    public class AzureFunctionCliInvoker
    {
        private HostedRunspace _hosted;
        private string RunFunction => "func start";
        private string RunWithBuild => $"{RunFunction} --build";

        public async Task RunAzureFunction()
        {
            _hosted = new HostedRunspace();
            var scriptContents = new StringBuilder();
            scriptContents.AppendLine("Param($StrParam, $IntParam)");
            //scriptContents.AppendLine("Set-ExecutionPolicy ByPass");
            scriptContents.AppendLine("");
            //scriptContents.AppendLine("Write-Output \"Starting script\"");
            //scriptContents.AppendLine("Write-Output \"This is the value from the first param: $StrParam\"");
            //scriptContents.AppendLine("Write-Output \"This is the value from the second param: $IntParam\"");
            scriptContents.AppendLine("");
            scriptContents.AppendLine($" ../../../../CustomerFunctions/{RunWithBuild}");
            scriptContents.AppendLine(RunFunction);
            scriptContents.AppendLine("");

            var scriptParameters = new Dictionary<string, object>()
            {
                {"StrParam", "Hello from script"},
                {"IntParam", 7}
            };
            var options = new PowerShellParams()
            {
                ScriptContents = scriptContents.ToString(),
                ScriptParameters = scriptParameters
            };
            await RunAndWaitToFunction(options) ;
           
            //Console.Write($"PowerShell result: {string.Join(Environment.NewLine, result)}");
        }
        
         private async Task<string> RunAndWaitToFunction(PowerShellParams options)
        {
            
            var tcs = new TaskCompletionSource<string>();
            options.ErrorAdded += (sender, eventArgs) =>
            {
                var list = (PSDataCollection<ErrorRecord>)sender;
                var message = list[eventArgs.Index];
                TrySetResult(tcs, message.Exception?.Message ?? message.TargetObject?.ToString());
            };
            
            var psCallTask = _hosted.RunScript(options)
                .ContinueWith(result =>
                {
                    return result.Result;
                }, new CancellationToken(), TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext());
            
            string resultMessage = string.Empty;
            try
            {
                var readyTask = await Task.WhenAny(psCallTask, tcs.Task);
                resultMessage = readyTask == tcs.Task ? tcs.Task.Result : string.Join(Environment.NewLine, psCallTask.Result);
            }
            catch (Exception ex)
            {
                if (ex.InnerException is PSInvalidOperationException)
                {
                    throw ex.InnerException;
                }
                else if (IsError(ex.Message))
                {
                    throw;
                }
            }

            return resultMessage;

        }
        private void TrySetResult(TaskCompletionSource<string> tcs, string data)
        {
            if (IsFunctionStarted(data) && !tcs.Task.IsCompleted)
            {
                //Log.Write(this, TraceLevel.Info, $"powershell az login partial result - {data}");
                tcs.SetResult(data);
                return;
            }

            if (IsError(data))
            {
                //Log.Write(this, TraceLevel.Info, $"return error from current execution - {data}");
                if (!tcs.Task.IsCompleted)
                    tcs.SetException(new PSInvalidOperationException(data));
                
            }

            //Log.Write(this, TraceLevel.Info, $"add to error list - {data}");
        }

        private bool IsFunctionStarted(string data)
        {
            return data.Contains("For detailed output, run func with --verbose flag.");
        }

        private bool IsError(string message)
        {
            return !string.IsNullOrEmpty(message) && message.Contains("error", StringComparison.OrdinalIgnoreCase);
        }
        public void End()
        {
            _hosted.Finish();
        }
    }
}