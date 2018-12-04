using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.ConsensusContracts;
using AElf.Contracts.Consensus.ConsensusContracts.FieldMapCollections;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;
using AElf.Common;
using Google.Protobuf;
using ServiceStack;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Consensus
{
    // ReSharper disable ClassNeverInstantiated.Global
    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedMember.Global
    public class ConsensusContract : CSharpSmartContract
    {
        private static Address TokenContractAddress => ContractHelpers.GetTokenContractAddress(Api.GetChainId());

        #region DPoS

        private readonly UInt64Field _currentRoundNumberField =
            new UInt64Field(GlobalConfig.AElfDPoSCurrentRoundNumber);

        private readonly PbField<OngoingMiners> _ongoingMinersField =
            new PbField<OngoingMiners>(GlobalConfig.AElfDPoSOngoingMinersString);

        private readonly PbField<Timestamp> _timeForProducingExtraBlockField =
            new PbField<Timestamp>(GlobalConfig.AElfDPoSExtraBlockTimeSlotString);

        private readonly Int32Field _miningIntervalFiled = new Int32Field(GlobalConfig.AElfDPoSMiningIntervalString);

        private readonly PbField<Candidates> _candidatesField =
            new PbField<Candidates>(GlobalConfig.AElfDPoSCandidatesString);

        private readonly Map<UInt64Value, Round> _dposInfoMap =
            new Map<UInt64Value, Round>(GlobalConfig.AElfDPoSInformationString);

        private readonly Map<UInt64Value, StringValue> _eBPMap =
            new Map<UInt64Value, StringValue>(GlobalConfig.AElfDPoSExtraBlockProducerString);

        private readonly Map<UInt64Value, StringValue> _firstPlaceMap =
            new Map<UInt64Value, StringValue>(GlobalConfig.AElfDPoSFirstPlaceOfEachRoundString);

        private readonly Map<Address, Tickets> _balanceMap =
            new Map<Address, Tickets>(GlobalConfig.AElfDPoSBalanceMapString);

        private readonly Map<UInt64Value, ElectionSnapshot> _snapshotMap =
            new Map<UInt64Value, ElectionSnapshot>(GlobalConfig.AElfDPoSSnapshotFieldString);

        private readonly Map<UInt64Value, UInt64Value> _dividendsMap =
            new Map<UInt64Value, UInt64Value>(GlobalConfig.AElfDPoSDividendsMapString);
        
        private DPoS DPoSConsensus => new DPoS(new AElfDPoSFieldMapCollection
        {
            CurrentRoundNumberField = _currentRoundNumberField,
            OngoingMinersField = _ongoingMinersField,
            TimeForProducingExtraBlockField = _timeForProducingExtraBlockField,
            MiningIntervalField = _miningIntervalFiled,
            CandidatesField = _candidatesField,

            DPoSInfoMap = _dposInfoMap,
            EBPMap = _eBPMap,
            FirstPlaceMap = _firstPlaceMap,
            BalanceMap = _balanceMap,
            SnapshotField = _snapshotMap,
            DividendsMap = _dividendsMap
        });

        public async Task InitializeAElfDPoS(byte[] blockProducer, byte[] dPoSInfo, byte[] miningInterval,
            byte[] logLevel)
        {
            await DPoSConsensus.Initialize(new List<byte[]> {blockProducer, dPoSInfo, miningInterval, logLevel});
        }

        public async Task UpdateAElfDPoS(byte[] currentRoundInfo, byte[] nextRoundInfo, byte[] nextExtraBlockProducer,
            byte[] roundId)
        {
            await DPoSConsensus.Update(new List<byte[]>
            {
                currentRoundInfo,
                nextRoundInfo,
                nextExtraBlockProducer,
                roundId
            });
        }

        public async Task<Int32Value> Validation(byte[] accountAddress, byte[] timestamp, byte[] roundId)
        {
            return new Int32Value
            {
                Value = await DPoSConsensus.Validation(new List<byte[]> {accountAddress, timestamp, roundId})
            };
        }

        public async Task PublishOutValueAndSignature(byte[] roundNumber, byte[] outValue, byte[] signature,
            byte[] roundId)
        {
            await DPoSConsensus.Publish(new List<byte[]>
            {
                roundNumber,
                outValue,
                signature,
                roundId
            });
        }

        public async Task PublishInValue(byte[] roundNumber, byte[] inValue, byte[] roundId)
        {
            await DPoSConsensus.Publish(new List<byte[]>
            {
                roundNumber,
                inValue,
                roundId
            });
        }

        public async Task QuitElection()
        {
            await DPoSConsensus.Election(new List<byte[]>());
        }

        public async Task Vote(byte[] candidateAddress, byte[] amount)
        {
            await DPoSConsensus.Election(new List<byte[]>
            {
                candidateAddress,
                amount,
                new BoolValue {Value = true}.ToByteArray()
            });
        }

        public async Task Regret(byte[] candidateAddress, byte[] amount)
        {
            await DPoSConsensus.Election(new List<byte[]>
            {
                candidateAddress,
                amount,
                new BoolValue {Value = false}.ToByteArray()
            });
        }

        public async Task Replace(byte[] roundNumber)
        {
            await DPoSConsensus.Election(new List<byte[]>
            {
                roundNumber
            });
        }

        public Miners GetCurrentMiners()
        {
            return DPoSConsensus.GetCurrentMiners();
        }

        public async Task AddTickets(Address addressToGetTickets, ulong amount)
        {
            Api.Assert(Api.GetTransaction().From == TokenContractAddress,
                "Only token contract can call AddTickets method.");

            await DPoSConsensus.HandleTickets(addressToGetTickets, amount);
        }

        public async Task AnnounceElection(Address candidateAddress)
        {
            await DPoSConsensus.AnnounceElection(candidateAddress);
        }

        public async Task Withdraw(Address address, ulong amount)
        {
            await DPoSConsensus.HandleTickets(address, amount, true);
        }

        #endregion
    }
}