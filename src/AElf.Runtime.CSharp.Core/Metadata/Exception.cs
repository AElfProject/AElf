using System;
using Mono.Cecil;

namespace AElf.Runtime.CSharp.Metadata
{
    public class MetadataNotDetectableException : Exception
    {
        public MetadataNotDetectableException(string message) : base(message)
        {
        }
    }

    public class StateTypeDeclaringNonPropertyMethodException : MetadataNotDetectableException
    {
        public StateTypeDeclaringNonPropertyMethodException(MethodReference methodReference)
            : base($@"Method ""{methodReference}"" declared in a state type but is not a property setter or getter.")
        {
        }
    }

    public class StateAccessedInNonStateOrContractTypeException : MetadataNotDetectableException
    {
        public StateAccessedInNonStateOrContractTypeException(MethodReference methodReference)
            : base($@"Method ""{methodReference}"" accesses state type.")
        {
        }
    }
}