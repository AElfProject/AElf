using System;

namespace AElf.Kernel.Concurrency
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SmartContractFunctionAttribute : Attribute
    {
        public SmartContractFunctionAttribute(string functionSignature, string[] resources)
        {
            FunctionSignature = functionSignature;
            Resources = resources;
        }

        public string FunctionSignature { get; }
        public string[] Resources { get; }
    }
}