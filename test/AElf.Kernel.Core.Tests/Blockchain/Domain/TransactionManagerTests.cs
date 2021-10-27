﻿using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Types;
using Xunit;
using Shouldly;

namespace AElf.Kernel.Blockchain.Domain
{
    public sealed class TransactionManagerTests : AElfKernelTestBase
    {
        private ITransactionManager _transactionManager;
        private KernelTestHelper _kernelTestHelper;

        public TransactionManagerTests()
        {
            _transactionManager = GetRequiredService<ITransactionManager>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task Insert_Transaction_Test()
        {
            var transaction = _kernelTestHelper.GenerateTransaction();
            var hash = await _transactionManager.AddTransactionAsync(transaction);
            hash.ShouldNotBeNull();
        }

        [Fact]
        public async Task Insert_MultipleTx_Test()
        {
            var t1 = _kernelTestHelper.GenerateTransaction(1, HashHelper.ComputeFrom("tx1"));
            var t2 = _kernelTestHelper.GenerateTransaction(2, HashHelper.ComputeFrom("tx2"));
            var key1 = await _transactionManager.AddTransactionAsync(t1);
            var key2 = await _transactionManager.AddTransactionAsync(t2);
            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public async Task Remove_Transaction_Test()
        {
            var t1 = _kernelTestHelper.GenerateTransaction(1, HashHelper.ComputeFrom("tx1"));
            var t2 = _kernelTestHelper.GenerateTransaction(2, HashHelper.ComputeFrom("tx2"));

            var key1 = await _transactionManager.AddTransactionAsync(t1);
            var key2 = await _transactionManager.AddTransactionAsync(t2);

            var td1 = await _transactionManager.GetTransactionAsync(key1);
            Assert.Equal(t1, td1);

            await _transactionManager.RemoveTransactionAsync(key2);
            var td2 = await _transactionManager.GetTransactionAsync(key2);
            Assert.Null(td2);
        }
    }
}