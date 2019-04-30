namespace AElf.Kernel
{
    public static class StateKeyHelper
    {
        public static string ToStorageKey(string contractAddressStorageKey, string path)
        {
            return contractAddressStorageKey + "/" + path;
        }
    }
}