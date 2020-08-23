using System.Collections.Generic;
using System.Numerics;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.AEPoW.Application
{
    public interface INonceProvider
    {
        void SetNonce(BlockIndex blockIndex, BigInteger nonce);
        BigInteger GetNonce(BlockIndex blockIndex);
    }

    public class NonceProvider : INonceProvider, ISingletonDependency
    {
        private readonly Dictionary<BlockIndex, BigInteger> _nonces = new Dictionary<BlockIndex, BigInteger>();

        public void SetNonce(BlockIndex blockIndex, BigInteger nonce)
        {
            if (_nonces.ContainsKey(blockIndex)) return;
            _nonces.Add(blockIndex, nonce);
        }

        public BigInteger GetNonce(BlockIndex blockIndex)
        {
            _nonces.TryGetValue(blockIndex, out var nonce);
            return nonce;
        }
    }
}