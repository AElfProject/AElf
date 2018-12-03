using System.Collections.Generic;
using System.Threading;
using AElf.CLI2.JS;
using ChakraCore.NET;

namespace AElf.CLI2.Utils
{
    public class TimerProxy
    {
        private IJSEngine _engine;

        public TimerProxy(IJSEngine engine)
        {
            _engine = engine;
        }

        public void SetTimeout(JSValue callback, int milliSeconds)
        {
            var jsTimer = callback.InitTimer();
            jsTimer.SetInterval(()=>callback.CallMethod("call"), milliSeconds);
        }

//        public void CallCallback(object callback)
//        {
//            ((JSValue) callback).CallMethod("call");
//        }
    }
}