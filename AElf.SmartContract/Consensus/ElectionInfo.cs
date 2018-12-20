using System;
using System.Collections.Generic;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Kernel.Storages;

namespace AElf.SmartContract.Consensus
{
    public class ElectionInfo : IElectionInfo
    {
        private readonly ContractInfoReader _contractInfoReader;

        private static Address ConsensusContractAddress =>
            ContractHelpers.GetConsensusContractAddress(Hash.LoadByteArray(ChainConfig.Instance.ChainId.DecodeBase58()));

        private static Address DividendsContractAddress =>
            ContractHelpers.GetDividendsContractAddress(Hash.LoadByteArray(ChainConfig.Instance.ChainId.DecodeBase58()));
        
        private static Address TokenContractAddress =>
            ContractHelpers.GetTokenContractAddress(Hash.LoadByteArray(ChainConfig.Instance.ChainId.DecodeBase58()));
        
        public ElectionInfo(IStateStore stateStore)
        {
            var chainId = Hash.LoadBase58(ChainConfig.Instance.ChainId);
            _contractInfoReader = new ContractInfoReader(chainId, stateStore);
        }
        
        public bool IsCandidate(string publicKey)
        {
            var candidatesBytes = _contractInfoReader.GetBytes<Candidates>(ConsensusContractAddress,
                Hash.FromString(GlobalConfig.AElfDPoSCandidatesString));
            var candidates = Candidates.Parser.ParseFrom(candidatesBytes);
            return candidates.PublicKeys.Contains(publicKey);
        }

        public Tickets GetVotingInfo(string publicKey)
        {
            var ticketsBytes = _contractInfoReader.GetBytes<Tickets>(ConsensusContractAddress,
                Hash.FromMessage(publicKey.ToStringValue()), GlobalConfig.AElfDPoSCandidatesString);
            var tickets = Tickets.Parser.ParseFrom(ticketsBytes);
            return tickets;
        }

        public Tuple<ulong, ulong> GetVotesGeneral()
        {
            throw new NotImplementedException();
        }

        public Round GetRoundInfo(ulong roundNumber)
        {
            var roundBytes = _contractInfoReader.GetBytes<Round>(ConsensusContractAddress,
                Hash.FromMessage(roundNumber.ToUInt64Value()), GlobalConfig.AElfDPoSRoundsMapString);
            var round = Round.Parser.ParseFrom(roundBytes);
            return round;
        }

        public List<string> GetCurrentMines()
        {
            throw new NotImplementedException();
        }
    }
}