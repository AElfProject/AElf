using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.TestKit;
using AElf.Cryptography;
using AElf.Kernel.Account.Infrastructure;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public class TestDataProvider : ITestDataProvider
    {
        private readonly IBlockTimeProvider _blockTimeProvider;
        private readonly ITransactionListProvider _transactionListProvider;
        private readonly IAElfAsymmetricCipherKeyPairProvider _keyPairProvider;

        public TestDataProvider(IBlockTimeProvider blockTimeProvider, ITransactionListProvider transactionListProvider,
            IAElfAsymmetricCipherKeyPairProvider keyPairProvider)
        {
            _blockTimeProvider = blockTimeProvider;
            _transactionListProvider = transactionListProvider;
            _keyPairProvider = keyPairProvider;
        }

        public Timestamp GetBlockTime()
        {
            return _blockTimeProvider.GetBlockTime();
        }

        public void SetBlockTime(Timestamp blockTime)
        {
            _blockTimeProvider.SetBlockTime(blockTime);
        }

        public async Task AddTransactionListAsync(List<Transaction> transactions)
        {
            await _transactionListProvider.AddTransactionListAsync(transactions);
        }

        public async Task<List<Transaction>> GetTransactionListAsync()
        {
            return await _transactionListProvider.GetTransactionListAsync();
        }

        public async Task ResetAsync()
        {
            await _transactionListProvider.ResetAsync();
        }

        public void SetKeyPair(IAElfAsymmetricCipherKeyPair ecKeyPair)
        {
            _keyPairProvider.SetKeyPair(ecKeyPair);
        }

        public IAElfAsymmetricCipherKeyPair GetKeyPair()
        {
            return _keyPairProvider.GetKeyPair();
        }
    }
}