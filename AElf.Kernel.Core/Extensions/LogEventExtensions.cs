using System;
using AElf.Common;

namespace AElf.Kernel
{
    public static class LogEventExtensions
    {
        //TODO Add GetBloom test case [Case]
        public static Bloom GetBloom(this LogEvent logEvent)
        {
            var bloom = new Bloom();
            bloom.AddValue(logEvent.Address);
            bloom.AddValue(logEvent.Name.GetBytes());
            foreach (var t in logEvent.Indexed)
            {
                bloom.AddValue(t.ToByteArray());
            }

            return bloom;
        }
    }
}