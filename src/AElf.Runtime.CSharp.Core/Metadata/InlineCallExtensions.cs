using AElf.Kernel.SmartContract.Metadata;

namespace AElf.Runtime.CSharp.Metadata
{
    public static class InlineCallExtensions
    {
        public static InlineCall WithPrefix(this InlineCall original, string prefix)
        {
            var output = new InlineCall()
            {
                AddressPath = {prefix},
                MethodName = original.MethodName
            };
            output.AddressPath.AddRange(original.AddressPath);
            return output;
        }
    }
}