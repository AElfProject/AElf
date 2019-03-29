using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.DPoS.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types.CSharp;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel
{
    public class LibBestChainFoundEventHandlerTests : KernelWithChainTestBase
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IBlockchainService _blockchainService;
        private readonly BestChainFoundEventHandler _bestChainFoundEventHandler;
        private readonly KernelTestHelper _kernelTestHelper;
        
        private readonly Address _consensusAddress = Address.FromString("ConsensusAddress");

        public LibBestChainFoundEventHandlerTests()
        {
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _bestChainFoundEventHandler = GetRequiredService<BestChainFoundEventHandler>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();

            _smartContractAddressService.SetAddress(ConsensusSmartContractAddressNameProvider.Name, _consensusAddress);
        }

        [Fact]
        public async Task Test_HandleEvent()
        {
            {
                // Null event
                //             BestChainHeight: 11
                // LastIrreversibleBlockHeight: 5
                var chain = await _blockchainService.GetChainAsync();
                var currentLibHeight = chain.LastIrreversibleBlockHeight;
                var currentLibHash = chain.LastIrreversibleBlockHash;

                var eventData = new BestChainFoundEventData();
                await _bestChainFoundEventHandler.HandleEventAsync(eventData);

                LibShouldBe(currentLibHeight, currentLibHash);
            }

            {
                // Empty event 
                //             BestChainHeight: 11
                // LastIrreversibleBlockHeight: 5
                var chain = await _blockchainService.GetChainAsync();
                var currentLibHeight = chain.LastIrreversibleBlockHeight;
                var currentLibHash = chain.LastIrreversibleBlockHash;

                var eventData = new BestChainFoundEventData
                {
                    ExecutedBlocks = new List<Hash>()
                };
                await _bestChainFoundEventHandler.HandleEventAsync(eventData);

                LibShouldBe(currentLibHeight, currentLibHash);
            }

            {
                // Transaction execute failed
                //             BestChainHeight: 12
                // LastIrreversibleBlockHeight: 5
                var chain = await _blockchainService.GetChainAsync();
                var currentLibHeight = chain.LastIrreversibleBlockHeight;
                var currentLibHash = chain.LastIrreversibleBlockHash;

                var transaction = _kernelTestHelper.GenerateTransaction();
                var transactionResult =
                    _kernelTestHelper.GenerateTransactionResult(transaction, TransactionResultStatus.Failed);
                var newBlock = await _kernelTestHelper.AttachBlock(chain.BestChainHeight, chain.LongestChainHash, new
                    List<Transaction> {transaction}, new List<TransactionResult> {transactionResult});
                await _blockchainService.SetBestChainAsync(chain, newBlock.Height, newBlock.GetHash());

                var eventData = new BestChainFoundEventData
                {
                    BlockHash = newBlock.GetHash(),
                    BlockHeight = newBlock.Height,
                    ExecutedBlocks = new List<Hash> {newBlock.GetHash()}
                };
                await _bestChainFoundEventHandler.HandleEventAsync(eventData);

                LibShouldBe(currentLibHeight, currentLibHash);
            }

            {
                // Event not from consensus smart contract
                //             BestChainHeight: 13
                // LastIrreversibleBlockHeight: 5
                var chain = await _blockchainService.GetChainAsync();
                var currentLibHeight = chain.LastIrreversibleBlockHeight;
                var currentLibHash = chain.LastIrreversibleBlockHash;

                var transaction = _kernelTestHelper.GenerateTransaction();
                var logEvent = new LogEvent
                {
                    Address = Address.FromString("TokenContract"),
                    Indexed = {ByteString.CopyFrom(Hash.FromString("LIBFound").DumpByteArray())}
                };
                var transactionResult =
                    _kernelTestHelper.GenerateTransactionResult(transaction, TransactionResultStatus.Mined, logEvent);
                var newBlock = await _kernelTestHelper.AttachBlock(chain.BestChainHeight, chain.LongestChainHash,
                    new List<Transaction> {transaction}, new List<TransactionResult> {transactionResult});
                await _blockchainService.SetBestChainAsync(chain, newBlock.Height, newBlock.GetHash());

                var eventData = new BestChainFoundEventData
                {
                    BlockHash = newBlock.GetHash(),
                    BlockHeight = newBlock.Height,
                    ExecutedBlocks = new List<Hash> {newBlock.GetHash()}
                };
                await _bestChainFoundEventHandler.HandleEventAsync(eventData);

                LibShouldBe(currentLibHeight, currentLibHash);
            }

            {
                // Event from consensus smart contract, not contains 'LIBFound'
                //             BestChainHeight: 14
                // LastIrreversibleBlockHeight: 5
                var chain = await _blockchainService.GetChainAsync();
                var currentLibHeight = chain.LastIrreversibleBlockHeight;
                var currentLibHash = chain.LastIrreversibleBlockHash;

                var transaction = _kernelTestHelper.GenerateTransaction();
                var logEvent = new LogEvent
                {
                    Address = _consensusAddress,
                    Indexed = {ByteString.CopyFrom(Hash.FromString("ErrorEvent").DumpByteArray())}
                };
                var transactionResult =
                    _kernelTestHelper.GenerateTransactionResult(transaction, TransactionResultStatus.Mined, logEvent);
                var newBlock = await _kernelTestHelper.AttachBlock(chain.BestChainHeight, chain.LongestChainHash,
                    new List<Transaction> {transaction}, new List<TransactionResult> {transactionResult});
                await _blockchainService.SetBestChainAsync(chain, newBlock.Height, newBlock.GetHash());

                var eventData = new BestChainFoundEventData
                {
                    BlockHash = newBlock.GetHash(),
                    BlockHeight = newBlock.Height,
                    ExecutedBlocks = new List<Hash> {newBlock.GetHash()}
                };
                await _bestChainFoundEventHandler.HandleEventAsync(eventData);

                LibShouldBe(currentLibHeight, currentLibHash);
            }

            {
                // LIBFound
                //             BestChainHeight: 15
                // LastIrreversibleBlockHeight: 10
                var chain = await _blockchainService.GetChainAsync();

                var transaction = _kernelTestHelper.GenerateTransaction();
                var offset = 5;
                var logEvent = new LogEvent
                {
                    Address = _consensusAddress,
                    Indexed = {ByteString.CopyFrom(Hash.FromString("LIBFound").DumpByteArray())},
                    NonIndexed = ByteString.CopyFrom(ParamsPacker.Pack(offset))
                };
                var transactionResult =
                    _kernelTestHelper.GenerateTransactionResult(transaction, TransactionResultStatus.Mined, logEvent);
                var newBlock = await _kernelTestHelper.AttachBlock(chain.BestChainHeight, chain.BestChainHash,
                    new List<Transaction> {transaction}, new List<TransactionResult> {transactionResult});
                await _blockchainService.SetBestChainAsync(chain, newBlock.Height, newBlock.GetHash());

                var eventData = new BestChainFoundEventData
                {
                    BlockHash = newBlock.GetHash(),
                    BlockHeight = newBlock.Height,
                    ExecutedBlocks = new List<Hash> {newBlock.GetHash()}
                };

                var libHash = await _blockchainService.GetBlockHashByHeightAsync(chain, 10, chain.LongestChainHash);

                await _bestChainFoundEventHandler.HandleEventAsync(eventData);

                LibShouldBe(10, libHash);
            }
        }

        #region check methods

        private void LibShouldBe(long libHeight, Hash libHash)
        {
            var chain = _blockchainService.GetChainAsync().Result;
            chain.LastIrreversibleBlockHeight.ShouldBe(libHeight);
            chain.LastIrreversibleBlockHash.ShouldBe(libHash);
        }

        #endregion
    }
}