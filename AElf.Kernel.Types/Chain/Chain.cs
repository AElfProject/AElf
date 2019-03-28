﻿// ReSharper disable once CheckNamespace

using AElf.Common;
using Google.Protobuf;

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