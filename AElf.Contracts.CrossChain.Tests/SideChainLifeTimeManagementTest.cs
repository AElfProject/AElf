using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.CrossChain;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace AElf.Contract.CrossChain.Tests
{
    /*
     * Todo:
     * Side chain creation proposal is disable.
     * Lock resource is disable.
     * Token amount to check.
    */
    public class SideChainLifeTimeManagementTest : CrossChainContractTestBase
    {
        [Fact]
        public async Task Request_SideChain_Creation()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var balanceResult = GetBalanceOutput.Parser.ParseFrom(await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetBalance),
                new GetBalanceInput
                {
                    Owner = Address.FromPublicKey(Tester.KeyPair.PublicKey),
                    Symbol = "ELF"
                }));
            Assert.Equal(_balanceOfStarter, balanceResult.Balance);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            var txResult =await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContract.RequestChainCreation),
                sideChainInfo);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);
            var expectedChainId = ChainHelpers.GetChainId(1);
            Assert.Equal(expectedChainId, SInt32Value.Parser.ParseFrom(txResult.ReturnValue).Value);
        }
        
        [Fact]
        public async Task Request_SideChain_Creation_WithoutApprove()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 10;
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            var txResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName,
                sideChainInfo);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }
        
        [Fact]
        public async Task Request_SideChain_Creation_WithoutEnoughToken()
        {
            await InitializeCrossChainContract();
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = 1000_000L
            };
            var txResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName,
                sideChainInfo);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task Request_SideChain_Creation_WrongProposer()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 10;
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                LockedTokenAmount = lockedTokenAmount,
                Proposer = Address.Generate()
            };
            var txResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName,
                sideChainInfo);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }
        
        [Fact]
        public async Task Request_SideChain_Creation_WrongStatus()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Review,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress()
            };
            var txResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName,
                sideChainInfo);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }
        
        [Fact]
        public async Task Request_SideChain_Creation_Twice()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 20;
            await ApproveBalance(lockedTokenAmount * 2);

            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            var tx = await GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContract.RequestChainCreation),
                null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx});
            
            var txResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContract.RequestChainCreation),
                sideChainInfo);
            var expectedChainId = ChainHelpers.GetChainId(2);
            Assert.Equal(expectedChainId, SInt32Value.Parser.ParseFrom(txResult.ReturnValue).Value);
        }

        [Fact]
        public async Task Withdraw_ChainCreation()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx = await GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContract.RequestChainCreation),
                null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx});
            var expectedChainId = ChainHelpers.GetChainId(1);
            var txResult =
                await ExecuteContractWithMiningAsync(
                    CrossChainContractAddress,
                    nameof(CrossChainContract.WithdrawRequest),
                    new SInt32Value()
                    {
                        Value = expectedChainId
                    });
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);
        }
        
        [Fact]
        public async Task Withdraw_ChainCreation_WithWrongSender()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            
            var ecKeyPair = CryptoHelpers.GenerateKeyPair();
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = Address.FromPublicKey(ecKeyPair.PublicKey),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx = await GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContract.RequestChainCreation),
                ecKeyPair,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx});
            var expectedChainId = ChainHelpers.GetChainId(1);
            var txResult =
                await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                    nameof(CrossChainContract.WithdrawRequest),
                    new SInt32Value() {Value = expectedChainId});
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }
        
        [Fact]
        public async Task Withdraw_ChainCreation_ChainNotExist()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName, null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx});
            var expectedChainId = ChainHelpers.GetChainId(2);
            var txResult =
                await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                    nameof(CrossChainContract.WithdrawRequest),
                    new SInt32Value()
                    {
                        Value = expectedChainId
                    });
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }
        
        [Fact]
        public async Task Withdraw_ChainCreation_WrongStatus()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName, null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx});
            var chainId = ChainHelpers.GetChainId(1);
            await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContract.CreateSideChain),
                new SInt32Value()
                {
                    Value = chainId
                });
            var txResult =
                await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                    nameof(CrossChainContract.WithdrawRequest),
                    new SInt32Value()
                    {
                        Value = chainId
                    });
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task Create_SideChain()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName,null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx});
            var chainId = ChainHelpers.GetChainId(1);
            var txResult =
                await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                    nameof(CrossChainContract.CreateSideChain),
                    new SInt32Value()
                    {
                        Value = chainId
                    });
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);
            var rawDict = SideChainIdAndHeightDict.Parser.ParseFrom(await CallContractMethodAsync(CrossChainContractAddress,
                nameof(CrossChainContract.GetAllChainsIdAndHeight),
                new Empty()));
            var dict = new Dictionary<int, long>(rawDict.IdHeightDict);
            Assert.True(dict.ContainsKey(chainId));
        }
        
        [Fact]
        public async Task CheckLockedBalance()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var requestTransaction = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName,null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {requestTransaction});
            var chainId = ChainHelpers.GetChainId(1);
            var creationTransaction = await GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContract.CreateSideChain),
                null, new SInt32Value()
                {
                    Value = chainId
                });
            await MineAsync(new List<Transaction> {creationTransaction});
            var balance = SInt64Value.Parser.ParseFrom(await CallContractMethodAsync(CrossChainContractAddress,
                nameof(CrossChainContract.LockedBalance),
                new SInt32Value()
                {
                    Value = chainId
                })).Value;
            Assert.Equal(10, balance);
            
        }
        
        //TODO: Disable for param packer removing
//        [Fact]
//        public async Task Create_SideChain_FireEvent()
//        {
//            long lockedTokenAmount = 10;           
//            await InitializeCrossChainContract();
//            await ApproveBalance(lockedTokenAmount);
//            
//            var sideChainInfo = new SideChainInfo
//            {
//                SideChainStatus = SideChainStatus.Apply,
//                ContractCode = ByteString.Empty,
//                IndexingPrice = 1,
//                Proposer = CrossChainContractTestHelper.GetAddress(),
//                LockedTokenAmount = lockedTokenAmount
//            };
//            
//            var tx = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName, null,
//                sideChainInfo);
//            await MineAsync(new List<Transaction> {tx});
//            var txRes = await GetTransactionResult(tx.GetHash());
//            var chainId = ChainHelpers.GetChainId(1);
//            var txResult =
//                await ExecuteContractWithMiningAsync(CrossChainContractAddress,
//                    nameof(CrossChainContract.CreateSideChain),
//                    new SInt32Value()
//                    {
//                        Value = chainId
//                    });
//            Assert.True(txResult.Status == TransactionResultStatus.Mined);
//            object[] data = ParamsPacker.Unpack(txResult.Logs.First().Data.ToByteArray(),
//                new[] {typeof(Address), typeof(int)});
//            var actualChainId = (int) data[1];
//            var actualSender = (Address) data[0];
//            Assert.True(chainId == actualChainId);
//            Assert.Equal(CrossChainContractTestHelper.GetAddress(), actualSender);    
//        }
        
        [Fact]
        public async Task Create_SideChain_ChainNotExist()
        {
            long lockedTokenAmount = 10;           
            await InitializeCrossChainContract();
            await ApproveBalance(lockedTokenAmount);
            
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress()
            };
            
            var tx = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName, null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx});
            var chainId = ChainHelpers.GetChainId(2);
            var txResult =
                await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                    nameof(CrossChainContract.CreateSideChain),
                    new SInt32Value()
                {
                    Value = chainId
                });
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task Request_SideChain_Disposal()
        {
            long lockedTokenAmount = 10;           
            await InitializeCrossChainContract();
            await ApproveBalance(lockedTokenAmount);
            
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx1 = await GenerateTransactionAsync(CrossChainContractAddress, 
                nameof(CrossChainContract.RequestChainCreation),
                null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx1});
            var chainId = ChainHelpers.GetChainId(1);
            var tx2 = await GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContract.CreateSideChain),
                null, 
                new SInt32Value()
                {
                    Value = chainId
                });
            await MineAsync(new List<Transaction> {tx2});
            var txResult =
                await ExecuteContractWithMiningAsync(CrossChainContractAddress, 
                    nameof(CrossChainContract.RequestChainDisposal), 
                    new SInt32Value()
                    {
                        Value = chainId
                    });
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);
        }
        
        [Fact]
        public async Task Dispose_SideChain()
        {
            long lockedTokenAmount = 10;           
            await InitializeCrossChainContract();
            await ApproveBalance(lockedTokenAmount);
            
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx1 = await GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContract.RequestChainCreation),
                null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx1});
            var chainId = ChainHelpers.GetChainId(1);
            var tx2 = await GenerateTransactionAsync(CrossChainContractAddress, 
                nameof(CrossChainContract.CreateSideChain), null,
                new SInt32Value()
                {
                    Value = chainId
                });
            await MineAsync(new List<Transaction> {tx2});
            await ExecuteContractWithMiningAsync(CrossChainContractAddress, 
                nameof(CrossChainContract.RequestChainDisposal), 
                new SInt32Value()
                {
                    Value = chainId
                });
            var disposedChainIdBytes =
                await CallContractMethodAsync(CrossChainContractAddress, 
                    nameof(CrossChainContract.DisposeSideChain), 
                    new SInt32Value()
                    {
                        Value = chainId
                    });
            var disposedChainId = SInt64Value.Parser.ParseFrom(disposedChainIdBytes).Value;
            Assert.Equal(chainId, disposedChainId);
        }

        [Fact]
        public async Task GetChainStatus_Review()
        {
            long lockedTokenAmount = 10;           
            await InitializeCrossChainContract();
            await ApproveBalance(lockedTokenAmount);
            
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx1 = await GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContract.RequestChainCreation),
                null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx1});
            var chainId = ChainHelpers.GetChainId(1);
            
            var status =SInt32Value.Parser.ParseFrom(await CallContractMethodAsync(CrossChainContractAddress, 
                nameof(CrossChainContract.GetChainStatus),
                new SInt32Value()
                {
                    Value = chainId
                })).Value;
            Assert.Equal(1, status);
        }
        
        [Fact]
        public async Task GetChainStatus_Active()
        {
            long lockedTokenAmount = 10;           
            await InitializeCrossChainContract();
            await ApproveBalance(lockedTokenAmount);
            
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx1 = await GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContract.RequestChainCreation),
                null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx1});
            var chainId = ChainHelpers.GetChainId(1);
            await ExecuteContractWithMiningAsync(CrossChainContractAddress, 
                nameof(CrossChainContract.CreateSideChain),
                new SInt32Value()
                {
                    Value = chainId
                });
            var status =SInt32Value.Parser.ParseFrom(await CallContractMethodAsync(CrossChainContractAddress, 
                nameof(CrossChainContract.GetChainStatus),
                new SInt32Value()
                {
                    Value = chainId
                })).Value;
            Assert.True(status == 2);
        }
        
        [Fact]
        public async Task GetChainStatus_ChainNotExist()
        {
            long lockedTokenAmount = 10;           
            await InitializeCrossChainContract();
            await ApproveBalance(lockedTokenAmount);
            
            var chainId = ChainHelpers.GetChainId(1);
            var status = await CallContractMethodAsync(CrossChainContractAddress, 
                nameof(CrossChainContract.GetChainStatus), 
                new SInt32Value()
                {
                    Value = chainId
                });
            Assert.Equal(ByteString.Empty, status);
        }

        [Fact]
        public async Task Get_SideChain_Height()
        {
            long lockedTokenAmount = 10;           
            await InitializeCrossChainContract();
            await ApproveBalance(lockedTokenAmount);
            
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx1 = await GenerateTransactionAsync(CrossChainContractAddress, 
                nameof(CrossChainContract.RequestChainCreation), null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx1});
            var chainId = ChainHelpers.GetChainId(1);
            await ExecuteContractWithMiningAsync(CrossChainContractAddress, 
                nameof(CrossChainContract.CreateSideChain),
                new SInt32Value()
                {
                    Value = chainId
                });
            var height =SInt64Value.Parser.ParseFrom(await CallContractMethodAsync(CrossChainContractAddress, 
                nameof(CrossChainContract.GetSideChainHeight),
                new SInt32Value()
                {
                    Value = chainId
                })).Value;
            Assert.True(height == 0);
        }
        
        [Fact]
        public async Task Get_SideChain_Height_ChainNotExist()
        {
            long lockedTokenAmount = 10;           
            await InitializeCrossChainContract();
            await ApproveBalance(lockedTokenAmount);
            var chainId = ChainHelpers.GetChainId(1);
            var height = await CallContractMethodAsync(CrossChainContractAddress, 
                nameof(CrossChainContract.GetSideChainHeight),
                new SInt32Value()
                {
                    Value = chainId
                });
            Assert.Equal(ByteString.Empty, height);
        }
    }
}