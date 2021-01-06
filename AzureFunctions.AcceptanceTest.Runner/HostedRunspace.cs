﻿using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;

namespace AzureFunctions.AcceptanceTest.Runner
{
    public class PowerShellParams
    {
        public string ScriptContents { get; set; }
        public Dictionary<string, object> ScriptParameters { get; set; }
        public EventHandler<DataAddedEventArgs> ErrorAdded { get; set; }
    }
    /// <summary>
    /// Contains functionality for executing PowerShell scripts.
    /// </summary>
    public class HostedRunspace 
    {
        private IAsyncResult _result;

        public HostedRunspace()
        {
            InitializeRunspaces(2, 10, new string[]{});
        }
        /// <summary>
        /// The PowerShell runspace pool.
        /// </summary>
        private RunspacePool RsPool { get; set; }
        private PowerShell ps { get; set; }
        /// <summary>
        /// Initialize the runspace pool.
        /// </summary>
        /// <param name="minRunspaces"></param>
        /// <param name="maxRunspaces"></param>
        public void InitializeRunspaces(int minRunspaces, int maxRunspaces, string[] modulesToLoad)
        {
            // create the default session state.
            // session state can be used to set things like execution policy, language constraints, etc.
            // optionally load any modules (by name) that were supplied.
       
            var defaultSessionState = InitialSessionState.CreateDefault();
            defaultSessionState.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Unrestricted;
       
            foreach (var moduleName in modulesToLoad)
            {
                defaultSessionState.ImportPSModule(moduleName);
            }
       
            // use the runspace factory to create a pool of runspaces
            // with a minimum and maximum number of runspaces to maintain.
       
            RsPool = RunspaceFactory.CreateRunspacePool(defaultSessionState);
            RsPool.SetMinRunspaces(minRunspaces);
            RsPool.SetMaxRunspaces(maxRunspaces);
       
            // set the pool options for thread use.
            // we can throw away or re-use the threads depending on the usage scenario.
       
            RsPool.ThreadOptions = PSThreadOptions.UseNewThread;
       
            // open the pool. 
            // this will start by initializing the minimum number of runspaces.
       
            RsPool.Open();
        }
     
        /// <summary>
        /// Runs a PowerShell script with parameters and prints the resulting pipeline objects to the console output. 
        /// </summary>
        /// <param name="scriptContents">The script file contents.</param>
        /// <param name="scriptParameters">A dictionary of parameter names and parameter values.</param>
        public async Task<PSDataCollection<PSObject>> RunScript(PowerShellParams options)
        {
            if (RsPool == null)
            {
                throw new ApplicationException("Runspace Pool must be initialized before calling RunScript().");
            }
       
            // create a new hosted PowerShell instance using a custom runspace.
            // wrap in a using statement to ensure resources are cleaned up.
            ps = PowerShell.Create();
            
                // use the runspace pool.
                ps.RunspacePool = RsPool;
         
                // specify the script code to run.
                ps.AddScript(options.ScriptContents);
         
                // specify the parameters to pass into the script.
                ps.AddParameters(options.ScriptParameters);
         
                // subscribe to events from some of the streams
                ps.Streams.Error.DataAdded += Error_DataAdded;
                ps.Streams.Error.DataAdded += options.ErrorAdded;
                ps.Streams.Warning.DataAdded += Warning_DataAdded;
                ps.Streams.Warning.DataAdded += options.ErrorAdded;
                ps.Streams.Information.DataAdded += Information_DataAdded;
                ps.Streams.Information.DataAdded += options.ErrorAdded;
                
         
                // execute the script and await the result.
                
                var pipelineObjects = await ps.InvokeAsync().ConfigureAwait(false);
         
                // print the resulting pipeline objects to the console.
                Console.WriteLine("----- Pipeline Output below this point -----");
                foreach (var item in pipelineObjects)
                {
                    Console.WriteLine(item.BaseObject.ToString());
                }

                return pipelineObjects;
        }   
        
     
        /// <summary>
        /// Handles data-added events for the information stream.
        /// </summary>
        /// <remarks>
        /// Note: Write-Host and Write-Information messages will end up in the information stream.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Information_DataAdded(object sender, DataAddedEventArgs e)
        {
            var streamObjectsReceived = sender as PSDataCollection<InformationRecord>;
            var currentStreamRecord = streamObjectsReceived[e.Index];
       
            Console.WriteLine($"InfoStreamEvent: {currentStreamRecord.MessageData}");
        }
     
        /// <summary>
        /// Handles data-added events for the warning stream.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Warning_DataAdded(object sender, DataAddedEventArgs e)
        {
            var streamObjectsReceived = sender as PSDataCollection<WarningRecord>;
            var currentStreamRecord = streamObjectsReceived[e.Index];
       
            Console.WriteLine($"WarningStreamEvent: {currentStreamRecord.Message}");
        }
     
        /// <summary>
        /// Handles data-added events for the error stream.
        /// </summary>
        /// <remarks>
        /// Note: Uncaught terminating errors will stop the pipeline completely.
        /// Non-terminating errors will be written to this stream and execution will continue.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Error_DataAdded(object sender, DataAddedEventArgs e)
        {
            var streamObjectsReceived = sender as PSDataCollection<ErrorRecord>;
            var currentStreamRecord = streamObjectsReceived[e.Index];
       
            Console.WriteLine($"ErrorStreamEvent: {currentStreamRecord.Exception}");
        }

        public void Finish()
        {
            ps.Stop();
            RsPool.Close();
        }
    }
}