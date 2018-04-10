using AElf.Kernel.Merkle;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.Storages;

namespace AElf.Kernel
{
    public class DataProvider : IDataProvider
    {
        private readonly Hash _accountHash;
        private readonly Hash _chainId;
        private readonly ulong _increasementId;
        
        private readonly Dictionary<Hash, IChangesStore> _changesDictionary;
        private readonly IPointerStore _pointerStore;
        
        public DataProvider(IAccountDataContext accountDataContext, IPointerStore pointerStore, 
            Dictionary<Hash, IChangesStore> changesDictionary)
        {
            _changesDictionary = changesDictionary;
            _pointerStore = pointerStore;
            _accountHash = accountDataContext.Address;
            _chainId = accountDataContext.ChainId;
            _increasementId = accountDataContext.IncreasementId;
        }

        private Path GetPath()
        {
            return new Path()
                .SetChainHash(_chainId)
                .SetAccount(_accountHash)
                .SetDataProvider(GetHash());
        }

        private Hash GetHash()
        {
            return new Hash(_chainId.CalculateHashWith(_accountHash));
        }
        
        public IDataProvider GetDataProvider(string name)
        {
            throw new NotImplementedException();
        }

        public void SetDataProvider(string name)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> GetAsync(IHash key)
        {
            throw new NotImplementedException();
        }

        public Task SetAsync(IHash key, byte[] obj)
        {
            throw new NotImplementedException();
        }

    }
}
