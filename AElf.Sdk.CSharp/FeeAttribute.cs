using System;

namespace AElf.Sdk.CSharp
{
    [AttributeUsage(AttributeTargets.Method)]
    public class FeeAttribute : Attribute
    {
        public ulong Amount { get; }

        public FeeAttribute(ulong amount)
        {
            Amount = amount;
        }
    }
}