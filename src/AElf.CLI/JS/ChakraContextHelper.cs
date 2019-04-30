using System;
using System.Runtime.InteropServices;
using System.Threading;
using ChakraCore.NET;
using ChakraCore.NET.API;

namespace AElf.CLI.JS
{
    public static class ChakraContextHelper
    {
        public static void RegisterEvalService(this ChakraContext context)
        {
            var svc = new ContextServiceWithHelper((ContextService) context.ServiceNode.GetService<IContextService>());
            context.ServiceNode.PushService<IHelperService>(svc);
        }

        public static JavaScriptValue Eval(this ChakraContext context, string script)
        {
            return context.ServiceNode.GetService<IHelperService>().Eval(script);
        }

        #region required types

        private interface IHelperService : IService
        {
            JavaScriptValue Eval(string script);
        }

        private class ContextServiceWithHelper : ContextService, IHelperService
        {
            public ContextServiceWithHelper(CancellationTokenSource shutdownCTS) : base(shutdownCTS)
            {
            }

            public ContextServiceWithHelper(ContextService contextService) : base(contextService.ContextShutdownCTS)
            {
            }

            public JavaScriptValue Eval(string script)
            {
                var debugService = CurrentNode.GetService<IRuntimeDebuggingService>();
                return contextSwitch.With(() =>
                    JavaScriptContext.RunScript(script, debugService.GetScriptContext("Script", script)));
            }
        }

        #endregion
    }
}