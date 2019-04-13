// ReSharper disable once CheckNamespace

using System;
using Google.Protobuf;
using AElf.Common;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class Chain
    {
        public Chain(int chainId, Hash genesisBlockHash)
        {
            Id = chainId;
            GenesisBlockHash = genesisBlockHash;
        }
        
        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
    }
}