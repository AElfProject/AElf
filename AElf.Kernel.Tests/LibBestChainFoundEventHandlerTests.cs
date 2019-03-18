using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Consensus;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel
{
    public class LibBestChainFoundEventHandlerTests : KernelTestBase
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IBlockchainService _blockchainService;
        private readonly LibBestChainFoundEventHandler _libBestChainFoundEventHandler;
        private readonly ITransactionResultService _transactionResultService;
        private readonly Address _consensusAddress = Address.FromString("ConsensusAddress");
        
        public LibBestChainFoundEventHandlerTests()
        {
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _libBestChainFoundEventHandler = GetRequiredService<LibBestChainFoundEventHandler>();
            _transactionResultService = GetRequiredService<ITransactionResultService>();;
        }

        [Fact]
        public async Task Test_HandleEvent()
        {
            await PrepareTestData();
            
            {
                // Null event
                //             BestChainHeight: 11
                //          LongestChainHeight: 11
                // LastIrreversibleBlockHeight: 5
                var chain = await _blockchainService.GetChainAsync();
                var currentLibHeight = chain.LastIrreversibleBlockHeight;
                var currentLibHash = chain.LastIrreversibleBlockHash;

                var eventData = new BestChainFoundEventData();
                await _libBestChainFoundEventHandler.HandleEventAsync(eventData);

                LibShouldBe(currentLibHeight, currentLibHash);
            }

            {
                // Empty event 
                //             BestChainHeight: 11
                //          LongestChainHeight: 11
                // LastIrreversibleBlockHeight: 5
                var chain = await _blockchainService.GetChainAsync();
                var currentLibHeight = chain.LastIrreversibleBlockHeight;
                var currentLibHash = chain.LastIrreversibleBlockHash;
                
                var eventData = new BestChainFoundEventData
                {
                    ExecutedBlocks = new List<Hash>()
                };
                await _libBestChainFoundEventHandler.HandleEventAsync(eventData);

                LibShouldBe(currentLibHeight, currentLibHash);
            }

            {
                // Transaction execute failed
                //             BestChainHeight: 12
                //          LongestChainHeight: 12
                // LastIrreversibleBlockHeight: 5
                var chain = await _blockchainService.GetChainAsync();
                var currentLibHeight = chain.LastIrreversibleBlockHeight;
                var currentLibHash = chain.LastIrreversibleBlockHash;

                var transaction = GenerateTransaction();
                var transactionResult = GenerateTransactionResult(transaction, TransactionResultStatus.Failed);
                var newBlock = await AttachBlock(chain.BestChainHeight, chain.LongestChainHash, transaction,
                    transactionResult);
                await _blockchainService.SetBestChainAsync(chain, newBlock.Height, newBlock.GetHash());

                var eventData = new BestChainFoundEventData
                {
                    BlockHash = newBlock.GetHash(),
                    BlockHeight = newBlock.Height,
                    ExecutedBlocks = new List<Hash> {newBlock.GetHash()}
                };
                await _libBestChainFoundEventHandler.HandleEventAsync(eventData);

                LibShouldBe(currentLibHeight, currentLibHash);
            }

            {
                // Event not from consensus smart contract
                //             BestChainHeight: 13
                //          LongestChainHeight: 13
                // LastIrreversibleBlockHeight: 5
                var chain = await _blockchainService.GetChainAsync();
                var currentLibHeight = chain.LastIrreversibleBlockHeight;
                var currentLibHash = chain.LastIrreversibleBlockHash;
                
                var transaction = GenerateTransaction();
                var logEvent = new LogEvent
                {
                    Address = Address.FromString("TokenContract"),
                    Topics = {ByteString.CopyFrom(Hash.FromString("LIBFound").DumpByteArray())}
                };
                var transactionResult = GenerateTransactionResult(transaction, TransactionResultStatus.Mined, logEvent);
                var newBlock = await AttachBlock(chain.BestChainHeight, chain.LongestChainHash, transaction,
                    transactionResult);
                await _blockchainService.SetBestChainAsync(chain, newBlock.Height, newBlock.GetHash());

                var eventData = new BestChainFoundEventData
                {
                    BlockHash = newBlock.GetHash(),
                    BlockHeight = newBlock.Height,
                    ExecutedBlocks = new List<Hash> {newBlock.GetHash()}
                };
                await _libBestChainFoundEventHandler.HandleEventAsync(eventData);

                LibShouldBe(currentLibHeight, currentLibHash);
            }
            
            {
                // Event from consensus smart contract, not contains 'LIBFound'
                //             BestChainHeight: 14
                //          LongestChainHeight: 14
                // LastIrreversibleBlockHeight: 5
                var chain = await _blockchainService.GetChainAsync();
                var currentLibHeight = chain.LastIrreversibleBlockHeight;
                var currentLibHash = chain.LastIrreversibleBlockHash;
                
                var transaction = GenerateTransaction();
                var logEvent = new LogEvent
                {
                    Address = _consensusAddress,
                    Topics = {ByteString.CopyFrom(Hash.FromString("ErrorEvent").DumpByteArray())}
                };
                var transactionResult = GenerateTransactionResult(transaction, TransactionResultStatus.Mined, logEvent);
                var newBlock = await AttachBlock(chain.BestChainHeight, chain.LongestChainHash, transaction,
                    transactionResult);
                await _blockchainService.SetBestChainAsync(chain, newBlock.Height, newBlock.GetHash());

                var eventData = new BestChainFoundEventData
                {
                    BlockHash = newBlock.GetHash(),
                    BlockHeight = newBlock.Height,
                    ExecutedBlocks = new List<Hash> {newBlock.GetHash()}
                };
                await _libBestChainFoundEventHandler.HandleEventAsync(eventData);

                LibShouldBe(currentLibHeight, currentLibHash);
            }

            {
                // LIBFound
                //             BestChainHeight: 15
                //          LongestChainHeight: 15
                // LastIrreversibleBlockHeight: 10
                var chain = await _blockchainService.GetChainAsync();

                var transaction = GenerateTransaction();
                var logEvent = new LogEvent
                {
                    Address = _consensusAddress,
                    Topics = {ByteString.CopyFrom(Hash.FromString("LIBFound").DumpByteArray())},
                    Data = ByteString.CopyFrom(ParamsPacker.Pack(5))
                };
                var transactionResult = GenerateTransactionResult(transaction, TransactionResultStatus.Mined, logEvent);
                var newBlock = await AttachBlock(chain.BestChainHeight, chain.BestChainHash ,transaction,
                    transactionResult);
                await _blockchainService.SetBestChainAsync(chain, newBlock.Height, newBlock.GetHash());

                var eventData = new BestChainFoundEventData
                {
                    BlockHash = newBlock.GetHash(),
                    BlockHeight = newBlock.Height,
                    ExecutedBlocks = new List<Hash> {newBlock.GetHash()}
                };
                await _libBestChainFoundEventHandler.HandleEventAsync(eventData);

                var libHash = await _blockchainService.GetBlockHashByHeightAsync(chain, 10, chain.BestChainHash);

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
        
        #region private methods

        /// <summary>
        /// Create new chain, attach 10 blocks, set best chain and set lib.
        /// </summary>
        /// <returns>
        ///    BestChainHeight: 11
        /// LongestChainHeight: 11
        ///          LibHeight: 5
        /// </returns>
        private async Task<Chain> PrepareTestData()
        {
            var genesisBlock = new Block
            {
                Header = new BlockHeader
                {
                    Height = ChainConsts.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Empty
                },
                Body = new BlockBody()
            };
            var chain = await _blockchainService.CreateChainAsync(genesisBlock);

            _smartContractAddressService.SetAddress(ConsensusSmartContractAddressNameProvider.Name,
                _consensusAddress);

            var currentHeight = chain.BestChainHeight;
            var currentHash = chain.BestChainHash;
            for (var i = 0; i < 10; i++)
            {
                var transaction = GenerateTransaction();
                var transactionResult = GenerateTransactionResult(transaction, TransactionResultStatus.Mined);
                var block = await AttachBlock(currentHeight, currentHash, transaction, transactionResult);
                currentHeight = block.Height;
                currentHash = block.GetHash();

                chain = await _blockchainService.GetChainAsync();
                await _blockchainService.SetBestChainAsync(chain, currentHeight, currentHash);

                if (currentHeight < 6)
                {
                    await _blockchainService.SetIrreversibleBlockAsync(chain, currentHeight, currentHash);
                }
            }

            return chain;
        }

        private Transaction GenerateTransaction()
        {
            var transaction = new Transaction
            {
                From = Address.Zero,
                To = Address.Zero,
                MethodName = Guid.NewGuid().ToString()
            };

            return transaction;
        }

        private TransactionResult GenerateTransactionResult(Transaction transaction, TransactionResultStatus status,
            LogEvent logEvent = null)
        {
            var transactionResult = new TransactionResult
            {
                TransactionId = transaction.GetHash(),
                Status = status
            };

            if (logEvent != null)
            {
                transactionResult.Logs.Add(logEvent);
            }

            return transactionResult;
        }

        private async Task<Block> AttachBlock(long previousBlockHeight, Hash previousBlockHash,
            Transaction transaction, TransactionResult transactionResult)
        {
            var newBlock = new Block
            {
                Header = new BlockHeader
                {
                    Height = previousBlockHeight + 1,
                    PreviousBlockHash = previousBlockHash,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow)
                },
                Body = new BlockBody()
            };
            newBlock.AddTransaction(transaction);
            newBlock.Header.MerkleTreeRootOfTransactions = newBlock.Body.CalculateMerkleTreeRoots();
            
            await _blockchainService.AddBlockAsync(newBlock);
            var chain = await _blockchainService.GetChainAsync();
            await _blockchainService.AttachBlockToChainAsync(chain, newBlock);
            
            await _transactionResultService.AddTransactionResultAsync(transactionResult, newBlock.Header);

            return newBlock;
        }

        #endregion
        
    }
}