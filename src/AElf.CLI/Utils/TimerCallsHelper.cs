using System.Threading;
using AElf.CLI.JS;
using ChakraCore.NET;

namespace AElf.CLI.Utils
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