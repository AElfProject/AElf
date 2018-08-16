namespace AElf.Kernel
{
    public partial class LogEvent
    {
        public Bloom GetBloom()
        {
            var bloom = new Bloom();
            bloom.AddValue(Address);
            foreach (var t in Topics)
            {
                bloom.AddValue(t.ToByteArray());
            }

            return bloom;
        }
    }
}