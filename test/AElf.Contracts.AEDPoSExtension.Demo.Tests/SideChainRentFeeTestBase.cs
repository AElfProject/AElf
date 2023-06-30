using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Association;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.GovernmentSystem;
using AElf.Kernel;
using AElf.Kernel.Proposal;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;
using InitializeInput = AElf.Contracts.Parliament.InitializeInput;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests;

public class SideChainRentFeeTestBase<T> : ContractTestBase<T> where T : ContractTestModule
{
    protected const string NativeTokenSymbol = "ELF";
    protected ECKeyPair DefaultSenderKeyPair => Accounts[0].KeyPair;
    protected Address DefaultSender => Accounts[0].Address;

    protected List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
        Accounts.Take(1).Select(a => a.KeyPair).ToList();

    protected IBlockTimeProvider BlockTimeProvider =>
        Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();

    protected new Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();
    protected Address TokenContractAddress { get; set; }
    protected Address AssociationContractAddress { get; set; }
    protected Address ParliamentContractAddress { get; set; }
    protected Address ConsensusContractAddress { get; set; }

    internal BasicContractZeroImplContainer.BasicContractZeroImplStub BasicContractZeroStub { get; set; }
    internal TokenContractImplContainer.TokenContractImplStub TokenContractStub { get; set; }
    internal AssociationContractImplContainer.AssociationContractImplStub AssociationContractStub { get; set; }
    internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub { get; set; }

    internal AEDPoSContractImplContainer.AEDPoSContractImplStub AEDPoSContractStub { get; set; }
    private byte[] AssociationContractCode => Codes.Single(kv => kv.Key.Contains("Association")).Value;
    private byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
    private byte[] ParliamentContractCode => Codes.Single(kv => kv.Key.Contains("Parliament")).Value;
    private byte[] ConsensusContractCode => Codes.Single(kv => kv.Key.Contains("Consensus.AEDPoS")).Value;

    private byte[] ElectionContractCode => Codes.Single(kv => kv.Key.Contains("Election")).Value;
    private byte[] ReferendumContractCode => Codes.Single(kv => kv.Key.Contains("Referendum")).Value;

    protected void DeployContracts()
    {
        BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);

        //deploy Association contract
        AssociationContractAddress = AsyncHelper.RunSync(() =>
            DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                AssociationContractCode,
                AssociationSmartContractAddressNameProvider.Name,
                DefaultSenderKeyPair
            ));

        AssociationContractStub = GetAssociationContractTester(DefaultSenderKeyPair);
        TokenContractAddress = AsyncHelper.RunSync(() =>
            DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                TokenContractCode,
                TokenSmartContractAddressNameProvider.Name,
                DefaultSenderKeyPair));
        TokenContractStub = GetTokenContractTester(DefaultSenderKeyPair);
        // AsyncHelper.RunSync(async () => await InitializeTokenAsync());

        ParliamentContractAddress = AsyncHelper.RunSync(() =>
            DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                ParliamentContractCode,
                ParliamentSmartContractAddressNameProvider.Name,
                DefaultSenderKeyPair
            ));
        ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);
        AsyncHelper.RunSync(async () => await InitializeParliamentContract());

        ConsensusContractAddress = AsyncHelper.RunSync(() =>
            DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                ConsensusContractCode,
                HashHelper.ComputeFrom("AElf.ContractNames.Consensus"),
                DefaultSenderKeyPair
            ));
        AEDPoSContractStub = GetConsensusContractTester(DefaultSenderKeyPair);
        AsyncHelper.RunSync(async () => await InitializeAElfConsensus());

        AsyncHelper.RunSync(() =>
            DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                ElectionContractCode,
                ElectionSmartContractAddressNameProvider.Name,
                DefaultSenderKeyPair));

        AsyncHelper.RunSync(() =>
            DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                ReferendumContractCode,
                ReferendumSmartContractAddressNameProvider.Name,
                DefaultSenderKeyPair));
    }

    internal BasicContractZeroImplContainer.BasicContractZeroImplStub GetContractZeroTester(ECKeyPair keyPair)
    {
        return GetTester<BasicContractZeroImplContainer.BasicContractZeroImplStub>(ContractZeroAddress, keyPair);
    }

    internal TokenContractImplContainer.TokenContractImplStub GetTokenContractTester(ECKeyPair keyPair)
    {
        return GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, keyPair);
    }

    internal AssociationContractImplContainer.AssociationContractImplStub GetAssociationContractTester(
        ECKeyPair keyPair)
    {
        return GetTester<AssociationContractImplContainer.AssociationContractImplStub>(AssociationContractAddress,
            keyPair);
    }

    internal ParliamentContractImplContainer.ParliamentContractImplStub GetParliamentContractTester(ECKeyPair keyPair)
    {
        return GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
            keyPair);
    }

    internal AEDPoSContractImplContainer.AEDPoSContractImplStub GetConsensusContractTester(ECKeyPair keyPair)
    {
        return GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(ConsensusContractAddress, keyPair);
    }

    private async Task InitializeParliamentContract()
    {
        var initializeResult = await ParliamentContractStub.Initialize.SendAsync(new InitializeInput
        {
            PrivilegedProposer = DefaultSender,
            ProposerAuthorityRequired = true
        });
        if (!string.IsNullOrEmpty(initializeResult.TransactionResult.Error))
            throw new Exception(initializeResult.TransactionResult.Error);
    }

    protected async Task InitializeAElfConsensus()
    {
        {
            await AEDPoSContractStub.InitialAElfConsensusContract.SendAsync(
                new InitialAElfConsensusContractInput
                {
                    PeriodSeconds = 604800L,
                    MinerIncreaseInterval = 31536000
                });
        }
        {
            await AEDPoSContractStub.FirstRound.SendAsync(
                GenerateFirstRoundOfNewTerm(
                    new MinerList
                        { Pubkeys = { InitialCoreDataCenterKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey)) } },
                    4000, TimestampHelper.GetUtcNow()));
        }
    }

    private Round GenerateFirstRoundOfNewTerm(MinerList minerList, int miningInterval,
        Timestamp currentBlockTime, long currentRoundNumber = 0, long currentTermNumber = 0)
    {
        var sortedMiners = minerList.Pubkeys.Select(x => x.ToHex()).ToList();
        var round = new Round();

        for (var i = 0; i < sortedMiners.Count; i++)
        {
            var minerInRound = new MinerInRound();

            // The third miner will be the extra block producer of first round of each term.
            if (i == 0) minerInRound.IsExtraBlockProducer = true;

            minerInRound.Pubkey = sortedMiners[i];
            minerInRound.Order = i + 1;
            minerInRound.ExpectedMiningTime = currentBlockTime.AddMilliseconds(i * miningInterval + miningInterval);
            // Should be careful during validation.
            minerInRound.PreviousInValue = Hash.Empty;
            round.RealTimeMinersInformation.Add(sortedMiners[i], minerInRound);
        }

        round.RoundNumber = currentRoundNumber + 1;
        round.TermNumber = currentTermNumber + 1;
        round.IsMinerListJustChanged = true;
        round.ExtraBlockProducerOfPreviousRound = sortedMiners[0];

        return round;
    }
}