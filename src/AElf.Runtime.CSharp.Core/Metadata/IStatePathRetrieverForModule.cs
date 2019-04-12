using System.Collections.Generic;
using AElf.Kernel.SmartContract.Metadata;
using Google.Protobuf.Collections;
using Mono.Cecil;

namespace AElf.Runtime.CSharp.Metadata
{
    public interface IStatePathRetrieverForModule
    {
        Dictionary<MethodDefinition, RepeatedField<DataAccessPath>> GetPaths();
    }
}