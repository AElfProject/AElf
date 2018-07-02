﻿using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel
{
    public interface IDataProvider
    {
        IDataProvider GetDataProvider(string name);

        Task<Change> SetAsync(Hash keyHash, byte[] obj);

        Task<byte[]> GetAsync(Hash keyHash);
        
        Task<byte[]> GetAsync(Hash keyHash, Hash preBlockHash);

        Hash GetPathFor(Hash keyHash);

        Hash GetHash();
    }
}