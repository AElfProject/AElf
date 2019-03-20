namespace AElf.Kernel
{
    public static class LogEventExtensions
    {
        //TODO Add GetBloom test case [Case]
        public static Bloom GetBloom(this LogEvent logEvent)
        {
            var bloom = new Bloom();
            bloom.AddValue(logEvent.Address);
            foreach (var t in logEvent.Topics)
            {
                bloom.AddValue(t.ToByteArray());
            }

            return bloom;
        }
    }
}