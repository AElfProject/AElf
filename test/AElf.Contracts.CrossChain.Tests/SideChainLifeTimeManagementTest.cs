using System.Threading.Tasks;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
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
        public async Task Create_SideChain()
        {
            await InitializeCrossChainContractAsync();
            long lockedTokenAmount = 10;
            await ApproveBalanceAsync(lockedTokenAmount);

            // Create proposal and approve
            var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount, ByteString.CopyFromUtf8("Test"));
            await ApproveWithMinersAsync(proposalId);

            // release proposal
            var transactionResult = await ReleaseProposalAsync(proposalId);
            var chainId = CreationRequested.Parser.ParseFrom(transactionResult.Logs[0].NonIndexed).ChainId;

            var chainStatus = SInt32Value.Parser.ParseFrom(await CallContractMethodAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.GetChainStatus),
                new SInt32Value {Value = chainId})).Value;
            Assert.True(chainStatus == (int) SideChainStatus.Active);
        }

        [Fact]
        public async Task Create_SideChain_Failed()
        {
            await InitializeCrossChainContractAsync();
            long lockedTokenAmount = 10;
            await ApproveBalanceAsync(lockedTokenAmount);

            {
                var proposalId =
                    await CreateSideChainProposalAsync(10, lockedTokenAmount, ByteString.CopyFromUtf8("Test"));
                await ApproveWithMinersAsync(proposalId);

                var transactionResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Release), proposalId);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Invalid chain creation request.").ShouldBeTrue();
            }

            {
                var proposalId = await CreateSideChainProposalAsync(10, 0, ByteString.CopyFromUtf8("Test"));
                await ApproveWithMinersAsync(proposalId);

                var transactionResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Release), proposalId);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Invalid chain creation request.").ShouldBeTrue();
            }
            {
                var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount, ByteString.Empty);
                await ApproveWithMinersAsync(proposalId);

                var transactionResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Release), proposalId);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Invalid chain creation request.").ShouldBeTrue();
            }
        }

        [Fact]
        public async Task CheckLockedBalance()
        {
            await InitializeCrossChainContractAsync();
            long lockedTokenAmount = 10;
            await ApproveBalanceAsync(lockedTokenAmount);

            var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount, ByteString.CopyFromUtf8("Test"));
            await ApproveWithMinersAsync(proposalId);
            var transactionResult = await ReleaseProposalAsync(proposalId);
            var chainId = CreationRequested.Parser.ParseFrom(transactionResult.Logs[0].NonIndexed).ChainId;

            var balance = SInt64Value.Parser.ParseFrom(await CallContractMethodAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.LockedBalance),
                new SInt32Value()
                {
                    Value = chainId
                })).Value;
            Assert.Equal(10, balance);
        }

        [Fact]
        public async Task Create_SideChain_NotAuthorized()
        {
            long lockedTokenAmount = 10;
            await InitializeCrossChainContractAsync();
            await ApproveBalanceAsync(lockedTokenAmount);

            var chainId = ChainHelpers.GetChainId(5);
            var transactionResult =
                await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                    nameof(CrossChainContractContainer.CrossChainContractStub.CreateSideChain),
                    new SInt32Value()
                    {
                        Value = chainId
                    });
            var status = transactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            var checkErrorMessage = transactionResult.Error.Contains("Not authorized to do this.");
            Assert.True(checkErrorMessage);
        }


//        [Fact]
//        public async Task Request_SideChain_Disposal()
//        {
//            long lockedTokenAmount = 10;           
//            await InitializeCrossChainContractAsync();
//            await ApproveBalanceAsync(lockedTokenAmount);
//            
//            var sideChainCreationRequest = CreateSideChainCreationRequest(1, lockedTokenAmount, ByteString.CopyFromUtf8("Test"));
//            var requestTxResult =await ExecuteContractWithMiningAsync(CrossChainContractAddress,
//                nameof(CrossChainContractContainer.CrossChainContractStub.RequestChainCreation),
//                sideChainCreationRequest);
//            await ApproveWithMinersAsync(RequestChainCreationOutput.Parser.ParseFrom(requestTxResult.ReturnValue).ProposalId);
//            var chainId = RequestChainCreationOutput.Parser.ParseFrom(requestTxResult.ReturnValue).ChainId;
//            
//            var txResult =
//                await ExecuteContractWithMiningAsync(CrossChainContractAddress, 
//                    nameof(CrossChainContractContainer.CrossChainContractStub.RequestChainDisposal), 
//                    new SInt32Value()
//                    {
//                        Value = chainId
//                    });
//            var status = txResult.Status;
//            Assert.True(status == TransactionResultStatus.Mined);
//        }
//
//        [Fact]
//        public async Task Request_SideChain_Disposal_NotAuthorized()
//        {
//            long lockedTokenAmount = 10;           
//            await InitializeCrossChainContractAsync();
//            await ApproveBalanceAsync(lockedTokenAmount);
//            
//            var sideChainCreationRequest = CreateSideChainCreationRequest(1, lockedTokenAmount, ByteString.CopyFromUtf8("Test"));
//            var requestTxResult =await ExecuteContractWithMiningAsync(CrossChainContractAddress,
//                nameof(CrossChainContractContainer.CrossChainContractStub.RequestChainCreation),
//                sideChainCreationRequest);
//            await ApproveWithMinersAsync(RequestChainCreationOutput.Parser.ParseFrom(requestTxResult.ReturnValue).ProposalId);
//            var chainId = RequestChainCreationOutput.Parser.ParseFrom(requestTxResult.ReturnValue).ChainId;
//            
//            var ecKeyPair = CryptoHelpers.GenerateKeyPair();
//            var other = Tester.CreateNewContractTester(ecKeyPair);
//            var txResult =
//                await other.ExecuteContractWithMiningAsync(CrossChainContractAddress, 
//                    nameof(CrossChainContractContainer.CrossChainContractStub.RequestChainDisposal), 
//                    new SInt32Value()
//                    {
//                        Value = chainId
//                    });
//            var status = txResult.Status;
//            Assert.True(status == TransactionResultStatus.Failed);
//            var checkErrorMessage = txResult.Error.Contains("Not authorized to dispose.");
//            Assert.True(checkErrorMessage);
//        }
//
//        [Fact]
//        public async Task Request_SideChain_Disposal_NotFound()
//        {
//            await InitializeCrossChainContractAsync();
//            var txResult =
//                await ExecuteContractWithMiningAsync(CrossChainContractAddress, 
//                    nameof(CrossChainContractContainer.CrossChainContractStub.RequestChainDisposal), 
//                    new SInt32Value()
//                    {
//                        Value = ChainHelpers.GetChainId(5)
//                    });
//            var status = txResult.Status;
//            Assert.True(status == TransactionResultStatus.Failed);
//            var checkErrorMessage = txResult.Error.Contains("Side chain not found");
//            Assert.True(checkErrorMessage);
//        }
//
//        [Fact]
//        public async Task Request_SideChain_Disposal_WrongStatus()
//        {
//            long lockedTokenAmount = 10;           
//            await InitializeCrossChainContractAsync();
//            await ApproveBalanceAsync(lockedTokenAmount);
//            
//            var sideChainCreationRequest = CreateSideChainCreationRequest(1, lockedTokenAmount, ByteString.CopyFromUtf8("Test"));
//            var requestTxResult =await ExecuteContractWithMiningAsync(CrossChainContractAddress,
//                nameof(CrossChainContractContainer.CrossChainContractStub.RequestChainCreation),
//                sideChainCreationRequest);
//            var chainId = RequestChainCreationOutput.Parser.ParseFrom(requestTxResult.ReturnValue).ChainId;
//            
//            var txResult =
//                await ExecuteContractWithMiningAsync(CrossChainContractAddress, 
//                    nameof(CrossChainContractContainer.CrossChainContractStub.RequestChainDisposal), 
//                    new SInt32Value()
//                    {
//                        Value = chainId
//                    });
//            var status = txResult.Status;
//            Assert.True(status == TransactionResultStatus.Failed);
//            var checkErrorMessage = txResult.Error.Contains("Side chain not found");
//            Assert.True(checkErrorMessage);
//        }
//        
//        [Fact]
//        public async Task Dispose_SideChain()
//        {
//            long lockedTokenAmount = 10;           
//            await InitializeCrossChainContractAsync();
//            await ApproveBalanceAsync(lockedTokenAmount);
//            
//            var sideChainCreationRequest = CreateSideChainCreationRequest(1, lockedTokenAmount, ByteString.CopyFromUtf8("Test"));
//
//            var requestChainCreationResult =await ExecuteContractWithMiningAsync(CrossChainContractAddress,
//                nameof(CrossChainContractContainer.CrossChainContractStub.RequestChainCreation),
//                sideChainCreationRequest);
//            await ApproveWithMinersAsync(RequestChainCreationOutput.Parser.ParseFrom(requestChainCreationResult.ReturnValue).ProposalId);
//            var chainId = ChainHelpers.GetChainId(1);
//            
//            var requestChainDisposalResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress, 
//                nameof(CrossChainContractContainer.CrossChainContractStub.RequestChainDisposal), 
//                new SInt32Value()
//                {
//                    Value = chainId
//                });
//            await ApproveWithMinersAsync(Hash.Parser.ParseFrom(requestChainDisposalResult.ReturnValue));
//            var chainStatus = SInt32Value.Parser.ParseFrom(await CallContractMethodAsync(CrossChainContractAddress,
//                nameof(CrossChainContractContainer.CrossChainContractStub.GetChainStatus), new SInt32Value{Value = chainId})).Value;
//            Assert.True(chainStatus == (int) SideChainStatus.Terminated);
//            
//            var status =SInt32Value.Parser.ParseFrom(await CallContractMethodAsync(CrossChainContractAddress, 
//                nameof(CrossChainContractContainer.CrossChainContractStub.GetChainStatus),
//                new SInt32Value()
//                {
//                    Value = chainId
//                })).Value;
//            Assert.Equal(4, status);
//        }
//
//        [Fact]
//        public async Task Dispose_SideChain_NotAuthorized()
//        {
//            await InitializeCrossChainContractAsync();
//            var txResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
//                nameof(CrossChainContractContainer.CrossChainContractStub.DisposeSideChain),new SInt32Value{Value = ChainHelpers.GetChainId(1)});
//            Assert.True(txResult.Status == TransactionResultStatus.Failed);
//            var checkErrorMessage = txResult.Error.Contains("Not authorized to do this.");
//            Assert.True(checkErrorMessage);
//        }
//
//        [Fact]
//        public async Task Dispose_SideChain_NotExistedChain()
//        {
//            await InitializeCrossChainContractAsync();
//            //create proposal
//            var chainId = ChainHelpers.GetChainId(5);
//            var proposalId = await CreateProposalAsync(chainId, "DisposeSideChain");
//            
//            var approveTransaction1 = await GenerateTransactionAsync(ParliamentAddress,
//                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), Tester.InitialMinerList[0], new Acs3.ApproveInput
//                {
//                    ProposalId = proposalId
//                });
//            await Tester.MineAsync(new List<Transaction> {approveTransaction1});
//            var tx1Result = await GetTransactionResultAsync(approveTransaction1.GetHash());
//            Assert.True(tx1Result.Status == TransactionResultStatus.Mined);
//            var approveTransaction2 = await GenerateTransactionAsync(ParliamentAddress,
//                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), Tester.InitialMinerList[1], new Acs3.ApproveInput
//                {
//                    ProposalId = proposalId
//                });
//            await Tester.MineAsync(new List<Transaction> {approveTransaction2});
//            var tx2Result = await GetTransactionResultAsync(approveTransaction2.GetHash());
//            Assert.True(tx2Result.Status == TransactionResultStatus.Failed);
//            var checkErrorMessage = tx2Result.Error.Contains("Not existed side chain.");
//            Assert.True(checkErrorMessage);
//        }
//
//        [Fact]
//        public async Task Dispose_SideChain_WrongStatus()
//        {
//            long lockedTokenAmount = 10;           
//            await InitializeCrossChainContractAsync();
//            await ApproveBalanceAsync(lockedTokenAmount);
//            
//            var sideChainCreationRequest = CreateSideChainCreationRequest(1, lockedTokenAmount, ByteString.CopyFromUtf8("Test"));
//
//            var requestChainCreationResult =await ExecuteContractWithMiningAsync(CrossChainContractAddress,
//                nameof(CrossChainContractContainer.CrossChainContractStub.RequestChainCreation),
//                sideChainCreationRequest);
//            var chainId = RequestChainCreationOutput.Parser.ParseFrom(requestChainCreationResult.ReturnValue).ChainId;
//            var proposalId = await CreateProposalAsync(chainId, "DisposeSideChain");
//            
//            var approveTransaction1 = await GenerateTransactionAsync(ParliamentAddress,
//                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), Tester.InitialMinerList[0], new Acs3.ApproveInput
//                {
//                    ProposalId = proposalId
//                });
//            await Tester.MineAsync(new List<Transaction> {approveTransaction1});
//            var tx1Result = await GetTransactionResultAsync(approveTransaction1.GetHash());
//            Assert.True(tx1Result.Status == TransactionResultStatus.Mined);
//            var approveTransaction2 = await GenerateTransactionAsync(ParliamentAddress,
//                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), Tester.InitialMinerList[1], new Acs3.ApproveInput
//                {
//                    ProposalId = proposalId
//                });
//            await Tester.MineAsync(new List<Transaction> {approveTransaction2});
//            var tx2Result = await GetTransactionResultAsync(approveTransaction2.GetHash());
//            Assert.True(tx2Result.Status == TransactionResultStatus.Failed);
//            var checkErrorMessage = tx2Result.Error.Contains("Unable to dispose this side chain.");
//            Assert.True(checkErrorMessage);
//        }
//
        [Fact]
        public async Task GetChainStatus_NotExist()
        {
            var chainId = ChainHelpers.GetChainId(1);
            var txResult = await Tester.ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.GetChainStatus),
                new SInt32Value()
                {
                    Value = chainId
                });
            txResult.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult.Error.Contains("Not existed side chain.").ShouldBeTrue();
        }

        [Fact]
        public async Task Get_SideChain_Height()
        {
            await InitializeCrossChainContractAsync();
            long lockedTokenAmount = 10;
            await ApproveBalanceAsync(lockedTokenAmount);
            
//            var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount, ByteString.CopyFromUtf8("Test"));
//            await ApproveWithMinersAsync(proposalId);
//            var transactionResult = await ReleaseProposalAsync(proposalId);
//            var chainId = CreationRequested.Parser.ParseFrom(transactionResult.Logs[0].NonIndexed).ChainId;
            var chainId = ChainHelpers.GetChainId(1);
            var height =SInt64Value.Parser.ParseFrom(await CallContractMethodAsync(CrossChainContractAddress, 
                nameof(CrossChainContractContainer.CrossChainContractStub.GetSideChainHeight),
                new SInt32Value()
                {
                    Value = chainId
                })).Value;
            Assert.True(height == 0);
        }
    }
}