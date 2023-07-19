using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Association;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;
using CreateOrganizationInput = AElf.Contracts.Parliament.CreateOrganizationInput;

namespace AElf.Contracts.MultiTokenCrossSideChain;

public class MultiTokenContractReferenceFeeTest : MultiTokenContractCrossChainTestBase
{
    public MultiTokenContractReferenceFeeTest()
    {
        AsyncHelper.RunSync(InitializeTokenContractAsync);
    }

    private async Task InitializeTokenContractAsync()
    {
        await TokenContractStub.InitializeAuthorizedController.SendAsync(new Empty());
    }

    [Fact]
    public async Task GetDefaultUserFeeController_Test()
    {
        var defaultParliament = await GetDefaultParliamentAddressAsync();
        var userFeeController = await TokenContractStub.GetUserFeeController.CallAsync(new Empty());
        userFeeController.RootController.ContractAddress.ShouldNotBeNull();
        userFeeController.ParliamentController.OwnerAddress.ShouldBe(defaultParliament);
        userFeeController.ParliamentController.ContractAddress.ShouldBe(ParliamentContractAddress);
    }

    [Fact]
    public async Task GetDefaultDeveloperFeeController_Test()
    {
        var defaultParliament = await GetDefaultParliamentAddressAsync();
        var developerFeeController = await TokenContractStub.GetDeveloperFeeController.CallAsync(new Empty());
        developerFeeController.RootController.ContractAddress.ShouldNotBeNull();
        developerFeeController.ParliamentController.OwnerAddress.ShouldBe(defaultParliament);
        developerFeeController.ParliamentController.ContractAddress.ShouldBe(ParliamentContractAddress);
    }

    [Fact]
    public async Task GetSymbolsToPayTXSizeFeeController_Test()
    {
        var defaultParliament = await GetDefaultParliamentAddressAsync();
        var controller = await TokenContractStub.GetSymbolsToPayTXSizeFeeController.CallAsync(new Empty());
        controller.OwnerAddress.ShouldBe(defaultParliament);
        controller.ContractAddress.ShouldBe(ParliamentContractAddress);
    }

    [Fact]
    public async Task ChangeSymbolsToPayTXSizeFeeController_Fail_Test()
    {
        // no authority
        var newAuthority = await CreateNewParliamentAddressAsync();
        var updateWithOutAuthorityRet =
            await TokenContractStub.ChangeSymbolsToPayTXSizeFeeController.SendWithExceptionAsync(newAuthority);
        updateWithOutAuthorityRet.TransactionResult.Error.ShouldContain("no permission");

        //invalid new organization
        var defaultParliamentAddress = await GetDefaultParliamentAddressAsync();
        var invalidAuthority = new AuthorityInfo
        {
            OwnerAddress = newAuthority.OwnerAddress,
            ContractAddress = AssociationContractAddress
        };
        var createProposalInput = new CreateProposalInput
        {
            ToAddress = TokenContractAddress,
            Params = invalidAuthority.ToByteString(),
            OrganizationAddress = defaultParliamentAddress,
            ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub
                .ChangeSymbolsToPayTXSizeFeeController),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        var ret = await MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(createProposalInput);
        ret.Error.ShouldContain("new controller does not exist");
    }

    [Fact]
    public async Task ChangeSymbolsToPayTXSizeFeeController_Success_Test()
    {
        var primaryToken = await TokenContractStub.GetPrimaryTokenSymbol.CallAsync(new Empty());
        var newSymbolList = new SymbolListToPayTxSizeFee();
        newSymbolList.SymbolsToPayTxSizeFee.Add(new SymbolToPayTxSizeFee
        {
            TokenSymbol = primaryToken.Value,
            AddedTokenWeight = 1,
            BaseTokenWeight = 1
        });

        // create a new organization to be replace the controller of SetSymbolsToPayTxSizeFee
        var newAuthority = await CreateNewParliamentAddressAsync();

        // get the default parliament that is the controller of SetSymbolsToPayTxSizeFee
        var defaultParliamentAddress = await GetDefaultParliamentAddressAsync();

        // create a proposal to replace the controller
        var createProposalInput = new CreateProposalInput
        {
            ToAddress = TokenContractAddress,
            Params = newAuthority.ToByteString(),
            OrganizationAddress = defaultParliamentAddress,
            ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub
                .ChangeSymbolsToPayTXSizeFeeController),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        await MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(createProposalInput);

        // the new controller try to send SetSymbolsToPayTxSizeFee
        var updateInput = new CreateProposalInput
        {
            ToAddress = TokenContractAddress,
            Params = newSymbolList.ToByteString(),
            OrganizationAddress = newAuthority.OwnerAddress,
            ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub.SetSymbolsToPayTxSizeFee),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        await MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(updateInput);
        var symbolSet = await TokenContractStub.GetSymbolsToPayTxSizeFee.CallAsync(new Empty());
        symbolSet.SymbolsToPayTxSizeFee.Count.ShouldBe(1);
    }

    private async Task<AuthorityInfo> CreateNewParliamentAddressAsync()
    {
        var newParliament = new CreateOrganizationInput
        {
            ProposerAuthorityRequired = false,
            ProposalReleaseThreshold = new ProposalReleaseThreshold
            {
                MaximalAbstentionThreshold = 1,
                MaximalRejectionThreshold = 1,
                MinimalApprovalThreshold = 1,
                MinimalVoteThreshold = 1
            },
            ParliamentMemberProposingAllowed = false
        };
        var parliamentCreateRet = await ParliamentContractStub.CreateOrganization.SendAsync(newParliament);
        var newParliamentAddress = parliamentCreateRet.Output;
        return new AuthorityInfo
        {
            ContractAddress = ParliamentContractAddress,
            OwnerAddress = newParliamentAddress
        };
    }

    //fee type : read = 0, storage = 1, write =2, traffic = 3
    [Theory]
    [InlineData(false, 3, new[] { 1 }, new[] { 1000000, 4, 3, 2 })]
    [InlineData(false, 0, new[] { 2 }, new[] { 999999, 1, 4, 2, 5, 250, 40 })]
    [InlineData(false, 0, new[] { 3 }, new[] { int.MaxValue, 2, 8, 2, 6, 300, 50 })]
    [InlineData(false, 2, new[] { 2, 3 }, new[] { 100, 1, 4, 10000 }, new[] { 1000000, 1, 4, 2, 2, 250, 50 })]
    [InlineData(true, 0, new[] { 2 }, new[] { int.MaxValue, 4, 3, 2 })]
    [InlineData(true, 0, new[] { 3 }, new[] { int.MaxValue, 2, 8, 2, 6, 300 })]
    [InlineData(true, 0, new[] { 3, 2 }, new[] { 1000, 4, 3, 2 }, new[] { int.MaxValue, 4, 3, 2 })]
    [InlineData(true, 0, new[] { 2, 3 }, new[] { 100, 4, 3, 2 })]
    [InlineData(true, 3, new[] { 1 }, new[] { 1000000, -1, 3, 2 })]
    [InlineData(true, 3, new[] { 1 }, new[] { 1000000, 4, -1, 2 })]
    [InlineData(true, 3, new[] { 1 }, new[] { 1000000, 4, 3, 0 })]
    public async Task Update_Coefficient_For_Contract_Test(bool isFail, int feeType, int[] pieceNumber,
        params int[][] newPieceFunctions)
    {
        var originalCoefficients = await GetCalculateFeeCoefficientsByFeeTypeAsync(feeType);
        var newPieceCoefficientList = newPieceFunctions.Select(x => new CalculateFeePieceCoefficients
        {
            Value = { x }
        }).ToList();
        var updateInput = new UpdateCoefficientsInput
        {
            PieceNumbers = { pieceNumber },
            Coefficients = new CalculateFeeCoefficients
            {
                FeeTokenType = feeType
            }
        };
        updateInput.Coefficients.PieceCoefficientsList.AddRange(newPieceCoefficientList);
        var proposalId = await CreateToRootForDeveloperFeeByTwoLayerAsync(updateInput,
            nameof(TokenContractImplContainer.TokenContractImplStub.UpdateCoefficientsForContract));
        await ApproveToRootForDeveloperFeeByTwoLayerAsync(proposalId);
        var middleApproveProposalId = await ApproveToRootForDeveloperFeeByMiddleLayerAsync(proposalId);
        await ApproveThenReleaseMiddleProposalForDeveloperAsync(middleApproveProposalId);
        await ReleaseToRootForDeveloperFeeByTwoLayerAsync(proposalId);
        var updatedCoefficients = await GetCalculateFeeCoefficientsByFeeTypeAsync(feeType);
        if (!isFail)
        {
            foreach (var newPieceFunction in newPieceFunctions)
            {
                var hasModified =
                    GetCalculateFeePieceCoefficients(updatedCoefficients.PieceCoefficientsList, newPieceFunction[0]);
                var newCoefficient = newPieceFunction.Skip(1).ToArray();
                hasModified.Value.Skip(1).ShouldBe(newCoefficient);
            }
        }
        else
        {
            var pieceCount = originalCoefficients.PieceCoefficientsList.Count;
            updatedCoefficients.PieceCoefficientsList.Count.ShouldBe(pieceCount);
            for (var i = 0; i < pieceCount; i++)
                originalCoefficients.PieceCoefficientsList[i]
                    .ShouldBe(updatedCoefficients.PieceCoefficientsList[i]);
        }
    }

    [Fact]
    public async Task GetCalculateFeeCoefficientsForContract_With_Invalid_FeeType_Test()
    {
        var invalidFeeType = -1;
        var ret = await TokenContractStub.GetCalculateFeeCoefficientsForContract.CallAsync(new Int32Value
        {
            Value = invalidFeeType
        });
        ret.ShouldBe(new CalculateFeeCoefficients());
    }

    [Fact]
    public async Task ChangeDeveloperController_Success_Test()
    {
        var newAuthority = await CreateNewParliamentAddressAsync();
        var proposalId = await CreateToRootForDeveloperFeeByTwoLayerAsync(newAuthority,
            nameof(TokenContractImplContainer.TokenContractImplStub.ChangeDeveloperController));
        await ApproveToRootForDeveloperFeeByTwoLayerAsync(proposalId);
        var middleApproveProposalId = await ApproveToRootForDeveloperFeeByMiddleLayerAsync(proposalId);
        await ApproveThenReleaseMiddleProposalForDeveloperAsync(middleApproveProposalId);
        await ReleaseToRootForDeveloperFeeByTwoLayerAsync(proposalId);
        var developerController =
            await TokenContractStub.GetDeveloperFeeController.CallAsync(new Empty());
        developerController.RootController.ContractAddress.ShouldBe(newAuthority.ContractAddress);
        developerController.RootController.OwnerAddress.ShouldBe(newAuthority.OwnerAddress);
    }

    [Fact]
    public async Task ChangeDeveloperController_Fail_Test()
    {
        var updateMethodName = nameof(TokenContractImplContainer.TokenContractImplStub.ChangeDeveloperController);
        var newAuthority = await CreateNewParliamentAddressAsync();
        var updateRet = await TokenContractStub.ChangeDeveloperController.SendWithExceptionAsync(newAuthority);
        updateRet.TransactionResult.Error.ShouldContain("no permission");

        var invalidAuthority = new AuthorityInfo
        {
            OwnerAddress = newAuthority.OwnerAddress,
            ContractAddress = AssociationContractAddress
        };

        var proposalId = await CreateToRootForDeveloperFeeByTwoLayerAsync(invalidAuthority, updateMethodName);
        await ApproveToRootForDeveloperFeeByTwoLayerAsync(proposalId);
        var middleApproveProposalId = await ApproveToRootForDeveloperFeeByMiddleLayerAsync(proposalId);
        await ApproveThenReleaseMiddleProposalForDeveloperAsync(middleApproveProposalId);
        var invalidRet = await ReleaseToRootForDeveloperFeeByTwoLayerAsync(proposalId);
        invalidRet.Error.ShouldContain("Invalid authority input");
    }


    //fee type : read = 0, storage = 1, write =2, traffic = 3
    [Theory]
    [InlineData(false, new[] { 1 }, new[] { 100, 4, 3, 2 })]
    [InlineData(false, new[] { 2, 3 }, new[] { 5000001, 1, 4, 10000 }, new[] { 5000002, 1, 4, 2, 2, 250, 50 })]
    [InlineData(true, new[] { 2 }, new[] { int.MaxValue, 4, 3, 2 })]
    [InlineData(true, new[] { 1 }, new[] { 100, 4, 3 })]
    [InlineData(true, new[] { 2, 3 }, new[] { 5000001, 1, 4, 10000 }, new[] { 5000001, 1, 4, 2, 2, 250, 50 })]
    [InlineData(true, new[] { 3, 2 }, new[] { 5000001, 1, 4, 10000 }, new[] { 5000002, 1, 4, 2, 2, 250, 50 })]
    [InlineData(true, new[] { 2, 3 }, new[] { 5000002, 4, 3, 2 })]
    public async Task Update_Coefficient_For_Sender_Test(bool isFail, int[] pieceNumber,
        params int[][] newPieceFunctions)
    {
        var feeType = (int)FeeTypeEnum.Tx;
        var primaryTokenSymbol = await GetThePrimaryTokenAsync();
        await IssuePrimaryTokenToMainChainTesterAsync();
        var originalCoefficients = await GetCalculateFeeCoefficientsByFeeTypeAsync(feeType);
        var newPieceCoefficientList = newPieceFunctions.Select(x => new CalculateFeePieceCoefficients
        {
            Value = { x }
        }).ToList();
        var updateInput = new UpdateCoefficientsInput
        {
            PieceNumbers = { pieceNumber },
            Coefficients = new CalculateFeeCoefficients
            {
                FeeTokenType = feeType
            }
        };
        updateInput.Coefficients.PieceCoefficientsList.AddRange(newPieceCoefficientList);

        var proposalId = await CreateToRootForUserFeeByTwoLayerAsync(updateInput,
            nameof(TokenContractImplContainer.TokenContractImplStub.UpdateCoefficientsForSender));
        await ApproveToRootForUserFeeByTwoLayerAsync(proposalId);
        await VoteToReferendumAsync(proposalId, primaryTokenSymbol);
        await ReleaseToRootForUserFeeByTwoLayerAsync(proposalId);

        var updatedCoefficients = await GetCalculateFeeCoefficientsByFeeTypeAsync(feeType);
        if (!isFail)
        {
            foreach (var newPieceFunction in newPieceFunctions)
            {
                var hasModified =
                    GetCalculateFeePieceCoefficients(updatedCoefficients.PieceCoefficientsList, newPieceFunction[0]);
                var newCoefficient = newPieceFunction.Skip(1).ToArray();
                hasModified.Value.Skip(1).ShouldBe(newCoefficient);
            }
        }
        else
        {
            var pieceCount = originalCoefficients.PieceCoefficientsList.Count;
            updatedCoefficients.PieceCoefficientsList.Count.ShouldBe(pieceCount);
            for (var i = 0; i < pieceCount; i++)
                originalCoefficients.PieceCoefficientsList[i]
                    .ShouldBe(updatedCoefficients.PieceCoefficientsList[i]);
        }
    }

    [Fact]
    public async Task ChangeUserFeeController_Success_Test()
    {
        var primaryTokenSymbol = await GetThePrimaryTokenAsync();
        await IssuePrimaryTokenToMainChainTesterAsync();
        var updateMethodName = nameof(TokenContractImplContainer.TokenContractImplStub.ChangeUserFeeController);
        var newAuthority = await CreateNewParliamentAddressAsync();

        var proposalId = await CreateToRootForUserFeeByTwoLayerAsync(newAuthority, updateMethodName);
        await ApproveToRootForUserFeeByTwoLayerAsync(proposalId);
        await VoteToReferendumAsync(proposalId, primaryTokenSymbol);
        await ReleaseToRootForUserFeeByTwoLayerAsync(proposalId);
        var userFeeController = await TokenContractStub.GetUserFeeController.CallAsync(new Empty());
        userFeeController.RootController.ContractAddress.ShouldBe(newAuthority.ContractAddress);
        userFeeController.RootController.OwnerAddress.ShouldBe(newAuthority.OwnerAddress);
    }

    [Fact]
    public async Task ChangeUserFeeController_Fail_Test()
    {
        var primaryTokenSymbol = await GetThePrimaryTokenAsync();
        await IssuePrimaryTokenToMainChainTesterAsync();
        var updateMethodName = nameof(TokenContractImplContainer.TokenContractImplStub.ChangeUserFeeController);
        var newAuthority = await CreateNewParliamentAddressAsync();
        var updateRet =
            await TokenContractStub.ChangeUserFeeController.SendWithExceptionAsync(newAuthority);
        updateRet.TransactionResult.Error.ShouldContain("no permission");

        var invalidAuthority = new AuthorityInfo
        {
            OwnerAddress = newAuthority.OwnerAddress,
            ContractAddress = AssociationContractAddress
        };
        var proposalId = await CreateToRootForUserFeeByTwoLayerAsync(invalidAuthority, updateMethodName);
        await ApproveToRootForUserFeeByTwoLayerAsync(proposalId);
        await VoteToReferendumAsync(proposalId, primaryTokenSymbol);
        var invalidRet = await ReleaseToRootForUserFeeByTwoLayerAsync(proposalId);
        invalidRet.Error.ShouldContain("Invalid authority input");
    }

    private async Task<CalculateFeeCoefficients> GetCalculateFeeCoefficientsByFeeTypeAsync(int feeType)
    {
        if (feeType == (int)FeeTypeEnum.Tx)
        {
            var userCoefficient =
                await TokenContractStub.GetCalculateFeeCoefficientsForSender.CallAsync(new Empty());
            return userCoefficient;
        }

        var developerCoefficient = await TokenContractStub.GetCalculateFeeCoefficientsForContract.CallAsync(
            new Int32Value
            {
                Value = feeType
            });
        return developerCoefficient;
    }

    private CalculateFeePieceCoefficients GetCalculateFeePieceCoefficients(
        IEnumerable<CalculateFeePieceCoefficients> calculateFeePieceCoefficients, int pieceUpperBound)
    {
        return calculateFeePieceCoefficients.SingleOrDefault(c => c.Value[0] == pieceUpperBound);
    }

    [Fact]
    public async Task MethodFeeController_Test_Fail()
    {
        var updateMethodName =
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ChangeMethodFeeController);
        // no authority
        var newAuthority = await CreateNewParliamentAddressAsync();
        //invalid new organization
        var defaultParliamentAddress = await GetDefaultParliamentAddressAsync();
        var invalidAuthority = new AuthorityInfo
        {
            OwnerAddress = newAuthority.OwnerAddress,
            ContractAddress = AssociationContractAddress
        };
        var createProposalInput = new CreateProposalInput
        {
            ToAddress = TokenContractAddress,
            Params = invalidAuthority.ToByteString(),
            OrganizationAddress = defaultParliamentAddress,
            ContractMethodName = updateMethodName,
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        var ret = await MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(createProposalInput);
        ret.Error.ShouldContain("Invalid authority input");
    }


    [Fact]
    public async Task MethodFeeController_Test_Success()
    {
        var defaultController = await TokenContractStub.GetMethodFeeController.CallAsync(new Empty());
        var createOrganizationResult = await ParliamentContractStub.CreateOrganization.SendAsync(
            new CreateOrganizationInput
            {
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = 1000,
                    MinimalVoteThreshold = 1000
                }
            });
        var organizationAddress = createOrganizationResult.Output;

        //change controller
        var newController = new AuthorityInfo
        {
            ContractAddress = defaultController.ContractAddress,
            OwnerAddress = organizationAddress
        };
        var createProposalInput = new CreateProposalInput
        {
            ToAddress = TokenContractAddress,
            Params = newController.ToByteString(),
            OrganizationAddress = defaultController.OwnerAddress,
            ContractMethodName =
                nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ChangeMethodFeeController),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        await MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(createProposalInput);
        var queryController = await TokenContractStub.GetMethodFeeController.CallAsync(new Empty());
        queryController.ShouldBe(newController);
    }

    [Fact]
    public async Task ChangeCrossChainTokenContractRegistrationController_Success_Test()
    {
        var defaultController =
            await TokenContractStub.GetCrossChainTokenContractRegistrationController.CallAsync(new Empty());
        var defaultParliamentAddress = await GetDefaultParliamentAddressAsync();
        defaultController.OwnerAddress.ShouldBe(defaultParliamentAddress);

        var newAuthority = await CreateNewParliamentAddressAsync();
        var proposalId = await CreateProposalAsync(ParliamentContractStub,
            nameof(TokenContractImplContainer.TokenContractImplStub
                .ChangeCrossChainTokenContractRegistrationController), newAuthority.ToByteString(),
            TokenContractAddress);

        await ApproveWithMinersAsync(proposalId);
        var txResult = await ReleaseProposalAsync(proposalId);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var newController =
            await TokenContractStub.GetCrossChainTokenContractRegistrationController.CallAsync(new Empty());
        Assert.True(newController.OwnerAddress == newAuthority.OwnerAddress);
    }

    [Fact]
    public async Task ChangeCrossChainTokenContractRegistrationController_Fail_Test()
    {
        var newAuthority = await CreateNewParliamentAddressAsync();
        var result =
            await TokenContractStub.ChangeCrossChainTokenContractRegistrationController.SendWithExceptionAsync(
                newAuthority);
        result.TransactionResult.Error.Contains("No permission.").ShouldBeTrue();

        var invalidAuthority = new AuthorityInfo
        {
            OwnerAddress = newAuthority.OwnerAddress,
            ContractAddress = AssociationContractAddress
        };
        var proposalId = await CreateProposalAsync(ParliamentContractStub,
            nameof(TokenContractImplContainer.TokenContractImplStub
                .ChangeCrossChainTokenContractRegistrationController), invalidAuthority.ToByteString(),
            TokenContractAddress);

        await ApproveWithMinersAsync(proposalId);
        var txResult = await ReleaseProposalAsync(proposalId);
        txResult.Error.ShouldContain("Invalid authority input");
    }

    [Fact]
    public async Task SetSymbolsToPayTxSizeFee_With_Invalid_Weight_Test()
    {
        var theDefaultController = await GetDefaultParliamentAddressAsync();
        var primaryTokenSymbol = await GetThePrimaryTokenAsync();
        var feeToken = "FEETOKEN";
        await CreateSeedNftCollection(TokenContractStub, DefaultAccount.Address);
        var input = new CreateInput
        {
            Symbol = feeToken,
            TokenName = "name",
            Issuer = DefaultAccount.Address,
            TotalSupply = 100_000,
            IsBurnable = true,
            Owner = DefaultAccount.Address
        };
        await CreateSeedNftAsync(TokenContractStub, input, TokenContractAddress);
        await TokenContractStub.Create.SendAsync(input);
        var newSymbolList = new SymbolListToPayTxSizeFee();
        newSymbolList.SymbolsToPayTxSizeFee.Add(new SymbolToPayTxSizeFee
        {
            TokenSymbol = primaryTokenSymbol,
            AddedTokenWeight = 2,
            BaseTokenWeight = 1
        });
        var result = await VerifyTheSymbolList(theDefaultController, newSymbolList);
        result.ShouldBe(false);

        newSymbolList.SymbolsToPayTxSizeFee[0].AddedTokenWeight = 1;
        newSymbolList.SymbolsToPayTxSizeFee.Add(new SymbolToPayTxSizeFee
        {
            TokenSymbol = feeToken,
            AddedTokenWeight = 0,
            BaseTokenWeight = 1
        });
        result = await VerifyTheSymbolList(theDefaultController, newSymbolList);
        result.ShouldBe(false);
    }

    [Fact]
    public async Task SetSymbolsToPayTxSizeFee_With_Repeat_Token_Test()
    {
        var theDefaultController = await GetDefaultParliamentAddressAsync();
        var primaryTokenSymbol = await GetThePrimaryTokenAsync();
        var feeToken = "FEETOKEN";
        await CreateSeedNftCollection(TokenContractStub, DefaultAccount.Address);
        var input = new CreateInput
        {
            Symbol = feeToken,
            TokenName = "name",
            Issuer = DefaultAccount.Address,
            TotalSupply = 100_000,
            Owner = DefaultAccount.Address
        };
        await CreateSeedNftAsync(TokenContractStub, input, TokenContractAddress);
        await TokenContractStub.Create.SendAsync(input);
        var newSymbolList = new SymbolListToPayTxSizeFee
        {
            SymbolsToPayTxSizeFee =
            {
                new SymbolToPayTxSizeFee
                {
                    TokenSymbol = primaryTokenSymbol,
                    AddedTokenWeight = 1,
                    BaseTokenWeight = 1
                },
                new SymbolToPayTxSizeFee
                {
                    TokenSymbol = feeToken,
                    AddedTokenWeight = 1,
                    BaseTokenWeight = 1
                },
                new SymbolToPayTxSizeFee
                {
                    TokenSymbol = feeToken,
                    AddedTokenWeight = 1,
                    BaseTokenWeight = 1
                }
            }
        };

        var result = await VerifyTheSymbolList(theDefaultController, newSymbolList);
        result.ShouldBe(false);
    }

    [Fact]
    public async Task SetSymbolsToPayTxSizeFee_Without_PrimaryToken_Test()
    {
        var theDefaultController = await GetDefaultParliamentAddressAsync();
        var feeToken = "FEETOKEN";
        await CreateSeedNftCollection(TokenContractStub, DefaultAccount.Address);
        var input = new CreateInput
        {
            Symbol = feeToken,
            TokenName = "name",
            Issuer = DefaultAccount.Address,
            TotalSupply = 100_000,
            Owner = DefaultAccount.Address
        };
        await CreateSeedNftAsync(TokenContractStub, input, TokenContractAddress);
        await TokenContractStub.Create.SendAsync(input);
        var newSymbolList = new SymbolListToPayTxSizeFee();
        newSymbolList.SymbolsToPayTxSizeFee.Add(new SymbolToPayTxSizeFee
        {
            TokenSymbol = feeToken,
            AddedTokenWeight = 2,
            BaseTokenWeight = 1
        });
        var result = await VerifyTheSymbolList(theDefaultController, newSymbolList);
        result.ShouldBe(false);
    }

    [Fact]
    public async Task SetSymbolsToPayTxSizeFee_Without_Profitable_Token_Test()
    {
        var theDefaultController = await GetDefaultParliamentAddressAsync();
        var primaryTokenSymbol = await GetThePrimaryTokenAsync();
        const string feeToken = "FEETOKEN";
        await CreateSeedNftCollection(TokenContractStub, DefaultAccount.Address);
        var input = new CreateInput
        {
            Symbol = feeToken,
            TokenName = "name",
            Issuer = DefaultAccount.Address,
            TotalSupply = 100_000,
            Owner = DefaultAccount.Address
        };
        await CreateSeedNftAsync(TokenContractStub, input, TokenContractAddress);
        await TokenContractStub.Create.SendAsync(input);
        var newSymbolList = new SymbolListToPayTxSizeFee
        {
            SymbolsToPayTxSizeFee =
            {
                new SymbolToPayTxSizeFee
                {
                    TokenSymbol = primaryTokenSymbol,
                    AddedTokenWeight = 1,
                    BaseTokenWeight = 1
                },
                new SymbolToPayTxSizeFee
                {
                    TokenSymbol = feeToken,
                    AddedTokenWeight = 1,
                    BaseTokenWeight = 1
                }
            }
        };
        var result = await VerifyTheSymbolList(theDefaultController, newSymbolList);
        result.ShouldBe(false);
    }

    [Fact(Skip = "Now we remove related logic temporaryly.")]
    public async Task SetSymbolsToPayTxSizeFee_With_Overflow_Input_Test()
    {
        var primaryToken = await TokenContractStub.GetPrimaryTokenSymbol.CallAsync(new Empty());
        var primaryTokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
        {
            Symbol = primaryToken.Value
        });
        var newSymbolList = new SymbolListToPayTxSizeFee();
        newSymbolList.SymbolsToPayTxSizeFee.Add(new SymbolToPayTxSizeFee
        {
            TokenSymbol = primaryToken.Value,
            AddedTokenWeight = 1,
            BaseTokenWeight = 1
        });
        var feeToken = "FEETOKEN";
        var newTokenTotalSupply = 1000_000_00000;
        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = feeToken,
            TokenName = "name",
            Issuer = TokenContractAddress,
            TotalSupply = newTokenTotalSupply,
            IsBurnable = true,
            Owner = TokenContractAddress
        });
        var invalidBaseTokenWeight = (int)long.MaxValue.Div(newTokenTotalSupply).Add(1);
        newSymbolList.SymbolsToPayTxSizeFee.Add(new SymbolToPayTxSizeFee
        {
            TokenSymbol = feeToken,
            AddedTokenWeight = 1,
            BaseTokenWeight = invalidBaseTokenWeight
        });

        var defaultParliamentAddress = await GetDefaultParliamentAddressAsync();
        var createProposalInput = new CreateProposalInput
        {
            ToAddress = TokenContractAddress,
            Params = newSymbolList.ToByteString(),
            OrganizationAddress = defaultParliamentAddress,
            ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub
                .SetSymbolsToPayTxSizeFee),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        var result = await MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(createProposalInput);
        result.Status.ShouldBe(TransactionResultStatus.Failed);
        result.Error.ShouldContain($"the weight of token {primaryToken.Value} is set too large");

        invalidBaseTokenWeight = (int)long.MaxValue.Div(primaryTokenInfo.TotalSupply).Add(1);
        newSymbolList.SymbolsToPayTxSizeFee[1].BaseTokenWeight = 1;
        newSymbolList.SymbolsToPayTxSizeFee[1].AddedTokenWeight = invalidBaseTokenWeight;
        createProposalInput = new CreateProposalInput
        {
            ToAddress = TokenContractAddress,
            Params = newSymbolList.ToByteString(),
            OrganizationAddress = defaultParliamentAddress,
            ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub
                .SetSymbolsToPayTxSizeFee),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        result = await MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(createProposalInput);
        result.Status.ShouldBe(TransactionResultStatus.Failed);
        result.Error.ShouldContain($"the weight of token {feeToken} is set too large");
    }

    [Fact]
    public async Task SetSymbolsToPayTxSizeFee_Success_Test()
    {
        var theDefaultController = await GetDefaultParliamentAddressAsync();
        var primaryTokenSymbol = await GetThePrimaryTokenAsync();
        const string feeToken = "FEETOKEN";
        await CreateSeedNftCollection(TokenContractStub, DefaultAccount.Address);
        var input = new CreateInput
        {
            Symbol = feeToken,
            TokenName = "name",
            Issuer = DefaultAccount.Address,
            TotalSupply = 100_000,
            IsBurnable = true,
            Owner = DefaultAccount.Address
        };
        await CreateSeedNftAsync(TokenContractStub, input, TokenContractAddress);
        await TokenContractStub.Create.SendAsync(input);
        var newSymbolList = new SymbolListToPayTxSizeFee
        {
            SymbolsToPayTxSizeFee =
            {
                new SymbolToPayTxSizeFee
                {
                    TokenSymbol = primaryTokenSymbol,
                    AddedTokenWeight = 1,
                    BaseTokenWeight = 1
                },
                new SymbolToPayTxSizeFee
                {
                    TokenSymbol = feeToken,
                    AddedTokenWeight = 1,
                    BaseTokenWeight = 2
                }
            }
        };
        var result = await VerifyTheSymbolList(theDefaultController, newSymbolList);
        result.ShouldBe(true);
    }

    [Fact]
    public async Task ClaimTransactionFee_Without_Authorized_Test()
    {
        var input = new TotalTransactionFeesMap();
        var tokenContractStub =
            GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, Accounts.Last().KeyPair);
        var claimRet = await tokenContractStub.ClaimTransactionFees.SendWithExceptionAsync(input);
        claimRet.TransactionResult.Error.ShouldContain("No permission");
    }

    private async Task<TransactionResult> MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(
        CreateProposalInput proposalInput)
    {
        var parliamentCreateProposal = await ParliamentContractStub.CreateProposal.SendAsync(proposalInput);
        var parliamentProposalId = parliamentCreateProposal.Output;
        await ApproveWithMinersAsync(parliamentProposalId);
        var releaseRet = await ReleaseProposalAsync(parliamentProposalId);
        //releaseRet.Status.ShouldBe(TransactionResultStatus.Mined);
        return releaseRet;
    }

    private async Task<Hash> CreateToRootForDeveloperFeeByTwoLayerAsync(IMessage input, string methodName)
    {
        var organizations = await GetControllerForDeveloperFeeAsync();
        var createNestProposalInput = new CreateProposalInput
        {
            ToAddress = TokenContractAddress,
            Params = input.ToByteString(),
            OrganizationAddress = organizations.RootController.OwnerAddress,
            ContractMethodName = methodName,
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        var createProposalInput = new CreateProposalInput
        {
            ToAddress = AssociationContractAddress,
            Params = createNestProposalInput.ToByteString(),
            OrganizationAddress = organizations.ParliamentController.OwnerAddress,
            ContractMethodName = nameof(AssociationContractImplContainer.AssociationContractImplStub.CreateProposal),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };

        var releaseRet = await MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(createProposalInput);
        var id = ProposalCreated.Parser
            .ParseFrom(releaseRet.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                .NonIndexed).ProposalId;
        return id;
    }

    private async Task ApproveToRootForDeveloperFeeByTwoLayerAsync(Hash input)
    {
        var organizations = await GetControllerForDeveloperFeeAsync();
        var approveProposalInput = new CreateProposalInput
        {
            ToAddress = AssociationContractAddress,
            Params = input.ToByteString(),
            OrganizationAddress = organizations.ParliamentController.OwnerAddress,
            ContractMethodName = nameof(AssociationContractImplContainer.AssociationContractImplStub.Approve),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        await MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(approveProposalInput);
    }

    private async Task<TransactionResult> ReleaseToRootForDeveloperFeeByTwoLayerAsync(Hash input)
    {
        var organizations = await GetControllerForDeveloperFeeAsync();
        var releaseProposalInput = new CreateProposalInput
        {
            ToAddress = AssociationContractAddress,
            Params = input.ToByteString(),
            OrganizationAddress = organizations.ParliamentController.OwnerAddress,
            ContractMethodName = nameof(AssociationContractImplContainer.AssociationContractImplStub.Release),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        return await MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(releaseProposalInput);
    }

    private async Task<Hash> ApproveToRootForDeveloperFeeByMiddleLayerAsync(Hash input)
    {
        var organizations = await GetControllerForDeveloperFeeAsync();
        var approveMidProposalInput = new CreateProposalInput
        {
            ToAddress = AssociationContractAddress,
            Params = input.ToByteString(),
            OrganizationAddress = organizations.DeveloperController.OwnerAddress,
            ContractMethodName = nameof(AssociationContractImplContainer.AssociationContractImplStub.Approve),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        var approveLeafProposalInput = new CreateProposalInput
        {
            ToAddress = AssociationContractAddress,
            Params = approveMidProposalInput.ToByteString(),
            OrganizationAddress = organizations.ParliamentController.OwnerAddress,
            ContractMethodName = nameof(AssociationContractImplContainer.AssociationContractImplStub.CreateProposal),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        var newCreateProposalRet =
            await MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(approveLeafProposalInput);
        var middleProposalId = ProposalCreated.Parser
            .ParseFrom(newCreateProposalRet.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                .NonIndexed).ProposalId;
        return middleProposalId;
    }

    private async Task ApproveThenReleaseMiddleProposalForDeveloperAsync(Hash input)
    {
        var organizations = await GetControllerForDeveloperFeeAsync();
        var approveLeafProposalInput = new CreateProposalInput
        {
            ToAddress = AssociationContractAddress,
            Params = input.ToByteString(),
            OrganizationAddress = organizations.ParliamentController.OwnerAddress,
            ContractMethodName = nameof(AssociationContractImplContainer.AssociationContractImplStub.Approve),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        await MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(approveLeafProposalInput);

        approveLeafProposalInput = new CreateProposalInput
        {
            ToAddress = AssociationContractAddress,
            Params = input.ToByteString(),
            OrganizationAddress = organizations.ParliamentController.OwnerAddress,
            ContractMethodName = nameof(AssociationContractImplContainer.AssociationContractImplStub.Release),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        await MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(approveLeafProposalInput);
    }

    private async Task<Hash> CreateToRootForUserFeeByTwoLayerAsync(IMessage input, string methodName)
    {
        var organizations = await GetControllerForUserFeeAsync();
        var createNestProposalInput = new CreateProposalInput
        {
            ToAddress = TokenContractAddress,
            Params = input.ToByteString(),
            OrganizationAddress = organizations.RootController.OwnerAddress,
            ContractMethodName = methodName,
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };

        var createProposalInput = new CreateProposalInput
        {
            ToAddress = AssociationContractAddress,
            Params = createNestProposalInput.ToByteString(),
            OrganizationAddress = organizations.ParliamentController.OwnerAddress,
            ContractMethodName = nameof(AssociationContractImplContainer.AssociationContractImplStub.CreateProposal),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        var releaseRet = await MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(createProposalInput);

        var id = ProposalCreated.Parser
            .ParseFrom(releaseRet.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                .NonIndexed).ProposalId;
        return id;
    }

    private async Task ApproveToRootForUserFeeByTwoLayerAsync(Hash input)
    {
        var organizations = await GetControllerForUserFeeAsync();
        var approveProposalInput = new CreateProposalInput
        {
            ToAddress = AssociationContractAddress,
            Params = input.ToByteString(),
            OrganizationAddress = organizations.ParliamentController.OwnerAddress,
            ContractMethodName = nameof(AssociationContractImplContainer.AssociationContractImplStub.Approve),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        await MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(approveProposalInput);
    }

    private async Task VoteToReferendumAsync(Hash input, string primaryTokenSymbol)
    {
        var organizations = await GetControllerForUserFeeAsync();

        var referendumProposal = new CreateProposalInput
        {
            ToAddress = AssociationContractAddress,
            Params = input.ToByteString(),
            OrganizationAddress = organizations.ReferendumController.OwnerAddress,
            ContractMethodName = nameof(AuthorizationContractContainer.AuthorizationContractStub.Approve),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };

        var parliamentProposal = new CreateProposalInput
        {
            ToAddress = ReferendumContractAddress,
            Params = referendumProposal.ToByteString(),
            OrganizationAddress = organizations.ParliamentController.OwnerAddress,
            ContractMethodName = nameof(AuthorizationContractContainer.AuthorizationContractStub.CreateProposal),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        var ret = await MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(parliamentProposal);
        var referendumProposalId = ProposalCreated.Parser
            .ParseFrom(ret.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                .NonIndexed).ProposalId;
        var proposalVirtualAddress =
            await ReferendumContractStub.GetProposalVirtualAddress.CallAsync(referendumProposalId);
        var approveResult = await TokenContractStub.Approve.SendAsync(new ApproveInput
        {
            Spender = proposalVirtualAddress,
            Symbol = primaryTokenSymbol,
            Amount = 100000
        });
        await ReferendumContractStub.Approve.SendAsync(referendumProposalId);

        parliamentProposal = new CreateProposalInput
        {
            ToAddress = ReferendumContractAddress,
            Params = referendumProposalId.ToByteString(),
            OrganizationAddress = organizations.ParliamentController.OwnerAddress,
            ContractMethodName = nameof(AuthorizationContractContainer.AuthorizationContractStub.Release),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        await MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(parliamentProposal);
    }

    private async Task<TransactionResult> ReleaseToRootForUserFeeByTwoLayerAsync(Hash input)
    {
        var organizations = await GetControllerForUserFeeAsync();
        var parliamentProposal = new CreateProposalInput
        {
            ToAddress = AssociationContractAddress,
            Params = input.ToByteString(),
            OrganizationAddress = organizations.ParliamentController.OwnerAddress,
            ContractMethodName = nameof(AuthorizationContractContainer.AuthorizationContractStub.Release),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        return await MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(parliamentProposal);
    }

    private async Task<UserFeeController> GetControllerForUserFeeAsync()
    {
        var organizationInfo = await TokenContractStub.GetUserFeeController.CallAsync(new Empty());
        return organizationInfo;
    }

    private async Task<DeveloperFeeController> GetControllerForDeveloperFeeAsync()
    {
        var organizationInfo = await TokenContractStub.GetDeveloperFeeController.CallAsync(new Empty());
        return organizationInfo;
    }

    private async Task<Address> GetDefaultParliamentAddressAsync()
    {
        var defaultParliamentAddress =
            await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        return defaultParliamentAddress;
    }

    private async Task IssuePrimaryTokenToMainChainTesterAsync()
    {
        var callOwner = DefaultAccount.Address;
        var primaryTokenSymbol = await GetThePrimaryTokenAsync();
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Amount = 100000,
            To = callOwner,
            Symbol = primaryTokenSymbol
        });
    }

    private async Task<string> GetThePrimaryTokenAsync()
    {
        var primaryTokenSymbol = await TokenContractStub.GetPrimaryTokenSymbol.CallAsync(new Empty());
        return primaryTokenSymbol.Value;
    }

    private async Task<bool> VerifyTheSymbolList(Address defaultController, SymbolListToPayTxSizeFee newSymbolList)
    {
        var createProposalInput = new CreateProposalInput
        {
            ToAddress = TokenContractAddress,
            Params = newSymbolList.ToByteString(),
            OrganizationAddress = defaultController,
            ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub
                .SetSymbolsToPayTxSizeFee),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        await MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(createProposalInput);
        var symbolListToPayTxSizeFee = await TokenContractStub.GetSymbolsToPayTxSizeFee.CallAsync(new Empty());
        return symbolListToPayTxSizeFee.SymbolsToPayTxSizeFee.Count != 0;
    }
}