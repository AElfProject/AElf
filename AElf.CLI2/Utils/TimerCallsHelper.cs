using System.Threading;
using AElf.CLI2.JS;
using ChakraCore.NET;

namespace AElf.CLI2.Utils
{
    public class TimerCallsHelper
    {
        private IJSEngine _engine;

        public TimerCallsHelper(IJSEngine engine)
        {
            _engine = engine;
        }

        public void RepeatedCalls(JSValue stopCallback, int interval, int repetition = int.MaxValue)
        {
            int count = 0;
            while (count < repetition)
            {
                if (stopCallback.CallFunction<bool>("call"))
                {
                    break;
                }
                count++;
                Thread.Sleep(interval);
            }
        }
    }
}