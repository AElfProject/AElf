using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        
        public async Task<bool> IsCandidate(string publicKey)
        {
            var candidatesBytes = await _contractInfoReader.GetBytesAsync<Candidates>(ConsensusContractAddress,
                Hash.FromString(GlobalConfig.AElfDPoSCandidatesString));
            var candidates = Candidates.Parser.ParseFrom(candidatesBytes);
            return candidates.PublicKeys.Contains(publicKey);
        }

        public async Task<Tickets> GetVotingInfo(string publicKey)
        {
            var ticketsBytes = await _contractInfoReader.GetBytesAsync<Tickets>(ConsensusContractAddress,
                Hash.FromMessage(publicKey.ToStringValue()), GlobalConfig.AElfDPoSCandidatesString);
            var tickets = Tickets.Parser.ParseFrom(ticketsBytes);
            return tickets;
        }

        public async Task<Tuple<ulong, ulong>> GetVotesGeneral()
        {
            // TODO NotImplemented
            return await Task.FromResult(default(Tuple<ulong, ulong>));
        }

        public async Task<Round> GetRoundInfo(ulong roundNumber)
        {
            var roundBytes = await _contractInfoReader.GetBytesAsync<Round>(ConsensusContractAddress,
                Hash.FromMessage(roundNumber.ToUInt64Value()), GlobalConfig.AElfDPoSRoundsMapString);
            var round = Round.Parser.ParseFrom(roundBytes);
            return round;
        }

        public async Task<List<string>> GetCurrentMines()
        {
            // TODO NotImplemented
            return await Task.FromResult(default(List<string>));
        }
    }
}