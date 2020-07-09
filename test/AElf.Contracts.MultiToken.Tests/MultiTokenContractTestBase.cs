using System;
using System.Collections.Generic;
using System.Linq;
using Acs2;
using System.Threading.Tasks;
using Acs3;
using Acs7;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Profit;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Contracts.Parliament;
using AElf.Contracts.Referendum;
using AElf.Contracts.TestBase;
using AElf.CrossChain;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.Types;
using AElf.Contracts.Treasury;
using AElf.Contracts.TokenConverter;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.GovernmentSystem;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Contracts.MultiToken
{
    public class MultiTokenContractTestBase : ContractTestKit.ContractTestBase<MultiTokenContractTestAElfModule>
    {
        public byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
        protected long AliceCoinTotalAmount => 1_000_000_000_0000000L;
        protected Address TokenContractAddress { get; set; }
        internal TokenContractImplContainer.TokenContractImplStub TokenContractStub;
        protected ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
        protected Address DefaultAddress => Accounts[0].Address;
        protected ECKeyPair User1KeyPair => Accounts[10].KeyPair;
        protected Address User1Address => Accounts[10].Address;
        protected Address User2Address => Accounts[11].Address;
        protected const string DefaultSymbol = "ELF";
        public byte[] TreasuryContractCode => Codes.Single(kv => kv.Key.Contains("Treasury")).Value;
        protected Address TreasuryContractAddress { get; set; }

        internal TreasuryContractContainer.TreasuryContractStub TreasuryContractStub;
        public byte[] ProfitContractCode => Codes.Single(kv => kv.Key.Contains("Profit")).Value;
        protected Address ProfitContractAddress { get; set; }

        internal ProfitContractContainer.ProfitContractStub ProfitContractStub;
        public byte[] TokenConverterContractCode => Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value;

        public byte[] ReferendumContractCode => Codes.Single(kv => kv.Key.Contains("Referendum")).Value;
        public byte[] ParliamentCode => Codes.Single(kv => kv.Key.Contains("Parliament")).Value;
        public byte[] ConsensusContractCode => Codes.Single(kv => kv.Key.Contains("Consensus.AEDPoS")).Value;
        public byte[] AssociationContractCode => Codes.Single(kv => kv.Key.Contains("Association")).Value;
        protected Address TokenConverterContractAddress { get; set; }
        protected Address ConsensusContractAddress { get; set; }

        internal TokenConverterContractContainer.TokenConverterContractStub TokenConverterContractStub;
        internal ReferendumContractContainer.ReferendumContractStub ReferendumContractStub;
        internal ParliamentContractContainer.ParliamentContractStub ParliamentContractStub;
        internal AEDPoSContractImplContainer.AEDPoSContractImplStub AEDPoSContractStub { get; set; }

        internal ParliamentContractContainer.ParliamentContractStub GetParliamentContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<ParliamentContractContainer.ParliamentContractStub>(ParliamentContractAddress,
                keyPair);
        }

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub GetConsensusContractTester(ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(ConsensusContractAddress, keyPair);
        }

        protected List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
            Accounts.Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).Select(a => a.KeyPair).ToList();
        protected Address ReferendumContractAddress { get; set; }

        internal ACS2BaseContainer.ACS2BaseStub Acs2BaseStub;

        protected Address BasicFunctionContractAddress { get; set; }

        protected Address OtherBasicFunctionContractAddress { get; set; }
        protected Address ParliamentContractAddress { get; set; }

        internal BasicFunctionContractContainer.BasicFunctionContractStub BasicFunctionContractStub { get; set; }

        internal BasicFunctionContractContainer.BasicFunctionContractStub OtherBasicFunctionContractStub { get; set; }
        protected byte[] BasicFunctionContractCode => Codes.Single(kv => kv.Key.EndsWith("BasicFunction")).Value;

        protected byte[] OtherBasicFunctionContractCode =>
            Codes.Single(kv => kv.Key.Contains("BasicFunctionWithParallel")).Value;

        protected Hash BasicFunctionContractName => HashHelper.ComputeFrom("AElf.TestContractNames.BasicFunction");
        protected Hash OtherBasicFunctionContractName => HashHelper.ComputeFrom("AElf.TestContractNames.OtherBasicFunction");

        protected Address Address => Accounts[0].Address;

        protected const string SymbolForTest = "ELF";

        protected const long Amount = 100;

        protected void CheckResult(TransactionResult result)
        {
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new Exception(result.Error);
            }
        }

        protected async Task InitializeParliamentContract()
        {
            var initializeResult = await ParliamentContractStub.Initialize.SendAsync(new Parliament.InitializeInput()
            {
                PrivilegedProposer = DefaultAddress,
                ProposerAuthorityRequired = true
            });
            CheckResult(initializeResult.TransactionResult);
        }

        protected async Task InitializeAElfConsensus()
        {
            {
                var result = await AEDPoSContractStub.InitialAElfConsensusContract.SendAsync(
                    new InitialAElfConsensusContractInput
                    {
                        PeriodSeconds = 604800L,
                        MinerIncreaseInterval = 31536000
                    });
                CheckResult(result.TransactionResult);
            }
            {
                var result = await AEDPoSContractStub.FirstRound.SendAsync(
                    new MinerList
                    {
                        Pubkeys = {InitialCoreDataCenterKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey))}
                    }.GenerateFirstRoundOfNewTerm(4000, TimestampHelper.GetUtcNow()));
                CheckResult(result.TransactionResult);
            }
        }
    }
}