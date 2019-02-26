//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using AElf.Common;
//using AElf.Contracts.Consensus.DPoS;
//using AElf.Contracts.Consensus.DPoS.Extensions;
//using AElf.Contracts.Genesis;
//using AElf.Cryptography;
//using AElf.Cryptography.ECDSA;
//using AElf.Kernel;
//using AElf.Kernel.Blockchain.Application;
//using AElf.Kernel.KernelAccount;
//using AElf.Kernel.Node.Application;
//using AElf.Kernel.SmartContract.Application;
//using AElf.Kernel.SmartContractExecution.Application;
//using AElf.Kernel.SmartContractExecution.Domain;
//using AElf.OS.Node.Application;
//using AElf.Types.CSharp;
//using Google.Protobuf;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Options;
//using Volo.Abp;
//using Volo.Abp.Threading;
//using Xunit;
//
//namespace AElf.Contracts.Consensus.Tests
//{
//    /// <summary>
//    /// In these test cases, we just care about the sequences, not the time slots.
//    /// </summary>
//    public class ConsensusProcessTest
//    {
//        private ITransactionExecutingService _transactionExecutingService;
//        private IBlockchainService _blockchainService;
//        private readonly List<ECKeyPair> _miners = new List<ECKeyPair>();
//
//        public int ChainId { get; set; } = ChainHelpers.ConvertBase58ToChainId("AELF");
//
//        private int MiningInterval => 1;
//
//        public ConsensusProcessTest()
//        {
//            
//        }
//
//        private void InitialMiners()
//        {
//            for (var i = 0; i < 17; i++)
//            {
//                _miners.Add(CryptoHelpers.GenerateKeyPair());
//            }
//        }
//
//        public IAbpApplicationWithInternalServiceProvider GetApplication()
//        {
//            var application =
//                AbpApplicationFactory.Create<ConsensusContractTestAElfModule>(options => { options.UseAutofac(); });
//            application.Initialize();
//
//            return application;
//        }
//
//        [Fact]
//        public void InitialTermTest()
//        {
//            InitialMiners();
//
//            var application = GetApplication();
//            
//            _transactionExecutingService = application.ServiceProvider.GetService<TransactionExecutingService>();
//
//            var transactions = GetGenesisTransactions(ChainId);
//            var dto = new OsBlockchainNodeContextStartDto()
//            {
//                BlockchainNodeContextStartDto = new BlockchainNodeContextStartDto()
//                {
//                    ChainId = ChainId,
//                    Transactions = transactions
//                }
//            };
//            var blockchainNodeContextService = application.ServiceProvider.GetService<BlockchainNodeContextService>();
//
//            _blockchainService = application.ServiceProvider.GetService<IBlockchainService>();
//
//            AsyncHelper.RunSync(() => blockchainNodeContextService.StartAsync(dto.BlockchainNodeContextStartDto));
//
//            var result = InitialTerm(_miners[0]);
//            Assert.True(result.Success);
//        }
//        
//        public Transaction[] GetGenesisTransactions(int chainId)
//        {
//            var transactions = new List<Transaction>();
//            transactions.Add(GetTransactionForDeployment(chainId, typeof(BasicContractZero)));
//            transactions.Add(GetTransactionForDeployment(chainId, typeof(AElf.Contracts.Consensus.DPoS.Contract)));
//            // TODO: Add initialize transactions
//            return transactions.ToArray();
//        }
//
//        private Transaction GetTransactionForDeployment(int chainId, Type contractType)
//        {
//            var zeroAddress = Address.BuildContractAddress(chainId, 0);
//            var code = File.ReadAllBytes(contractType.Assembly.Location);
//            return new Transaction()
//            {
//                From = zeroAddress,
//                To = zeroAddress,
//                MethodName = nameof(ISmartContractZero.DeploySmartContract),
//                // TODO: change cagtegory to 0
//                Params = ByteString.CopyFrom(ParamsPacker.Pack(2, code))
//            };
//        }
//
//        private ActionResult InitialTerm(ECKeyPair starterKeyPair)
//        {
//            var initialTerm =
//                new Miners {PublicKeys = {_miners.Select(m => m.PublicKey.ToHex())}}.GenerateNewTerm(MiningInterval);
//            var result = AsyncHelper.RunSync(() =>
//                ExecuteAsync(Address.BuildContractAddress(ChainId, 1), "InitialTerm", starterKeyPair, initialTerm));
//            return ActionResult.Parser.ParseFrom(result);
//        }
//
//        private async Task<ByteString> ExecuteAsync(Address contractAddress, string methodName, ECKeyPair callerKeyPair,
//            params object[] objects)
//        {
//            var tx = new Transaction
//            {
//                From = GetAddress(callerKeyPair),
//                To = contractAddress,
//                MethodName = methodName,
//                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects))
//            };
//
//            var signature = CryptoHelpers.SignWithPrivateKey(callerKeyPair.PrivateKey, tx.GetHash().DumpByteArray());
//            tx.Sigs.Add(ByteString.CopyFrom(signature));
//
//            var preBlock = await _blockchainService.GetBestChainLastBlock(ChainId);
//            var executionReturnSets = await _transactionExecutingService.ExecuteAsync(new ChainContext
//                {
//                    ChainId = ChainId,
//                    BlockHash = preBlock.GetHash(),
//                    BlockHeight = preBlock.Height
//                }, 
//                new List<Transaction> {tx},
//                DateTime.UtcNow, new CancellationToken());
//            return executionReturnSets.Last().ReturnValue;
//        }
//        
//        private Address GetAddress(ECKeyPair keyPair)
//        {
//            return Address.FromPublicKey(keyPair.PublicKey);
//        }
///*
//
//        [Fact(Skip = "Skip for now.")]
//        public void PackageOutValueTest()
//        {
//            InitialMiners();
//
//            InitialTerm(_miners[0]);
//            var firstRound = _contracts.GetRoundInfo(1);
//
//            Assert.Equal((ulong) 1, firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].ProducedBlocks);
//
//            var outValue = Hash.Generate();
//            var signatureOfInitialization = firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].Signature;
//            var signature = Hash.Generate(); // Should be update to round info, we'll see.
//            var toPackage = new ToPackage
//            {
//                OutValue = outValue,
//                RoundId = firstRound.RoundId,
//                Signature = signature
//            };
//            _contracts.PackageOutValue(_miners[0], toPackage);
//            Assert.Equal(string.Empty, _contracts.TransactionContext.Trace.StdErr);
//
//            // Check the round information.
//            firstRound = _contracts.GetRoundInfo(1);
//            // Signature not changed.
//            Assert.True(firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].Signature ==
//                        signatureOfInitialization);
//            Assert.True(firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].OutValue == outValue);
//            Assert.True(firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].InValue == null);
//            Assert.Equal((ulong) 2, firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].ProducedBlocks);
//        }
//
//        [Fact(Skip = "Skip for now.")]
//        public void PackageOutValueTest_RoundIdNotMatched()
//        {
//            InitialMiners();
//
//            InitialTerm(_miners[0]);
//            var firstRound = _contracts.GetRoundInfo(1);
//
//            var toPackage = new ToPackage
//            {
//                OutValue = Hash.Generate(),
//                RoundId = firstRound.RoundId + 1, // Wrong round id.
//                Signature = Hash.Generate()
//            };
//
//            try
//            {
//                _contracts.PackageOutValue(_miners[0], toPackage);
//            }
//            catch (Exception)
//            {
//                Assert.Equal(DPoSContractConsts.RoundIdNotMatched, _contracts.TransactionContext.Trace.StdErr);
//            }
//        }
//
//        [Fact(Skip = "Skip for now.")]
//        public void BroadcastInValueTest()
//        {
//            InitialMiners();
//
//            var inValue = Hash.Generate();
//            var outValue = Hash.FromMessage(inValue);
//
//            // Before
//            var firstRound = InitialTermAndPackageOutValue(_miners[0], outValue);
//            Assert.True(firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].OutValue == outValue);
//            Assert.True(firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].InValue == null);
//
//            _contracts.BroadcastInValue(_miners[0], new ToBroadcast
//            {
//                InValue = inValue,
//                RoundId = firstRound.RoundId
//            });
//            Assert.Equal(string.Empty, _contracts.TransactionContext.Trace.StdErr);
//
//            // After
//            firstRound = _contracts.GetRoundInfo(1);
//            Assert.True(firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].OutValue == outValue);
//            Assert.True(firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].InValue == inValue);
//        }
//
//        [Fact(Skip = "Skip for now.")]
//        public void BroadcastInValueTest_OutValueIsNull()
//        {
//            InitialMiners();
//
//            var inValue = Hash.Generate();
//            var outValue = Hash.FromMessage(inValue);
//
//            InitialTerm(_miners[0]);
//
//            var firstRound = _contracts.GetRoundInfo(1);
//            try
//            {
//                _contracts.BroadcastInValue(_miners[0], new ToBroadcast
//                {
//                    InValue = outValue,
//                    RoundId = firstRound.RoundId
//                });
//            }
//            catch (Exception)
//            {
//                Assert.Equal(DPoSContractConsts.OutValueIsNull, _contracts.TransactionContext.Trace.StdErr);
//            }
//        }
//
//        [Fact(Skip = "Skip for now.")]
//        public void BroadcastInValueTest_InValueNotMatchToOutValue()
//        {
//            InitialMiners();
//
//            var inValue = Hash.Generate();
//            var outValue = Hash.FromMessage(inValue);
//            var notMatchOutValue = Hash.FromMessage(outValue);
//
//            var firstRound = InitialTermAndPackageOutValue(_miners[0], notMatchOutValue);
//
//            try
//            {
//                _contracts.BroadcastInValue(_miners[0], new ToBroadcast
//                {
//                    InValue = inValue,
//                    RoundId = firstRound.RoundId
//                });
//            }
//            catch (Exception)
//            {
//                Assert.Equal(DPoSContractConsts.InValueNotMatchToOutValue,
//                    _contracts.TransactionContext.Trace.StdErr);
//            }
//        }
//
//        [Fact(Skip = "Skip for now.")]
//        public void NextRoundTest()
//        {
//            InitialMiners();
//
//            InitialTerm(_miners[0]);
//
//            var firstRound = _contracts.GetRoundInfo(1);
//
//            // Generate in values and out values.
//            var inValuesList = new Stack<Hash>();
//            var outValuesList = new Stack<Hash>();
//            for (var i = 0; i < GlobalConfig.BlockProducerNumber; i++)
//            {
//                var inValue = Hash.Generate();
//                inValuesList.Push(inValue);
//                outValuesList.Push(Hash.FromMessage(inValue));
//            }
//
//            // Actually their go one round.
//            foreach (var keyPair in _miners)
//            {
//                _contracts.PackageOutValue(keyPair, new ToPackage
//                {
//                    OutValue = outValuesList.Pop(),
//                    RoundId = firstRound.RoundId,
//                    Signature = Hash.Default
//                });
//
//                _contracts.BroadcastInValue(keyPair, new ToBroadcast
//                {
//                    InValue = inValuesList.Pop(),
//                    RoundId = firstRound.RoundId
//                });
//            }
//
//            // Extra block.
//            firstRound = _contracts.GetRoundInfo(1);
//            var suppliedFirstRound = firstRound.SupplementForFirstRound();
//            var secondRound = new Miners
//            {
//                TermNumber = 1,
//                PublicKeys = {_miners.Select(m => m.PublicKey.ToHex())}
//            }.GenerateNextRound(_mock.ChainId, suppliedFirstRound);
//            _contracts.NextRound(_miners[0], new Forwarding
//            {
//                CurrentAge = 1,
//                CurrentRound = suppliedFirstRound,
//                NextRound = secondRound
//            });
//
//            Assert.Equal(string.Empty, _contracts.TransactionContext.Trace.StdErr);
//
//            Assert.Equal((ulong) 2, _contracts.GetCurrentRoundNumber());
//        }
//
//        private void InitialTerm(ECKeyPair starterKeyPair)
//        {
//            var initialTerm =
//                new Miners {PublicKeys = {_miners.Select(m => m.PublicKey.ToHex())}}.GenerateNewTerm(MiningInterval);
//            _contracts.InitialTerm(starterKeyPair, initialTerm);
//        }
//
//        private Round InitialTermAndPackageOutValue(ECKeyPair starterKeyPair, Hash outValue)
//        {
//            InitialTerm(starterKeyPair);
//            var firstRound = _contracts.GetRoundInfo(1);
//            _contracts.PackageOutValue(starterKeyPair, new ToPackage
//            {
//                OutValue = outValue,
//                RoundId = firstRound.RoundId,
//                Signature = Hash.Default
//            });
//
//            return _contracts.GetRoundInfo(1);
//        }*/
//    }
//}