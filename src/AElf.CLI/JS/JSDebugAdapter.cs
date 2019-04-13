using System;

using ChakraCore.NET;
using ChakraCore.NET.Debug;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
namespace AElf.CLI.JS
{
    public class JSDebugAdapter : IDebugAdapter
    {
        public ILogger<JSDebugAdapter> Logger { get; set; }

        public JSDebugAdapter()
        {
            Logger = NullLogger<JSDebugAdapter>.Instance;
        }
        
        public void Init(IRuntimeDebuggingService debuggingService)
        {
            debuggingService.OnException += (sender, exception) =>
            {
                Logger.LogCritical(
                    $"Javascript side raise an uncaught exception.\n${JsonConvert.SerializeObject(exception)}\n");
            };
            debuggingService.OnAsyncBreak += (sender, point) => { };
            debuggingService.OnBreakPoint += (sender, point) => { };
            debuggingService.OnDebugEvent += (sender, arguments) => { };
            debuggingService.OnEngineReady += (sender, args) => { };
            debuggingService.OnScriptLoad += (sender, code) => { };
            debuggingService.OnStepComplete += (sender, point) => { };
            debuggingService.StartDebug();
        }
    }
}