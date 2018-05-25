using System;

namespace AElf.Kernel.Concurrency
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SmartContractFunctionAttribute : Attribute
    {
        public SmartContractFunctionAttribute(string functionFullName)
        {
            FunctionFullName = functionFullName;
        }

        public string FunctionFullName { get; }
    }
}