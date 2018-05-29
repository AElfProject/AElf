using System;

namespace AElf.Kernel.Concurrency.Metadata
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SmartContractFunctionAttribute : Attribute
    {
        public SmartContractFunctionAttribute(string functionSignature, string[] callingSet, string[] resources)
        {
            FunctionSignature = functionSignature;
            Resources = resources;
            CallingSet = callingSet;
        }

        public string FunctionSignature { get; }
        public string[] Resources { get; }
        public string[] CallingSet { get; }
    }
}