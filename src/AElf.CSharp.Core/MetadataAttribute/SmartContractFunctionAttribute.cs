using System;

namespace AElf.CSharp.Core.MetadataAttribute
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SmartContractFunctionAttribute : Attribute
    {
        public SmartContractFunctionAttribute(string functionSignature, string[] callingSet, string[] localResources)
        {
            FunctionSignature = functionSignature;
            LocalResources = localResources;
            CallingSet = callingSet;
        }

        public string FunctionSignature { get; }
        public string[] LocalResources { get; }
        public string[] CallingSet { get; }
    }
}