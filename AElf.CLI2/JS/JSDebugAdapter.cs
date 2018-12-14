using System;
using AElf.Common.Attributes;
using ChakraCore.NET;
using ChakraCore.NET.Debug;
using Newtonsoft.Json;
using NLog;

namespace AElf.CLI2.JS
{
    public class JSDebugAdapter : IDebugAdapter
    {
        private ILogger _logger = LogManager.GetLogger("js-debug");

        public void Init(IRuntimeDebuggingService debuggingService)
        {
            debuggingService.OnException += (sender, exception) =>
            {
                _logger.Fatal(
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