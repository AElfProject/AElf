using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Kernel.Managers;
using Google.Protobuf.WellKnownTypes;

namespace AElf.SmartContract.Consensus
{
    public class ElectionInfo : IElectionInfo
    {
        private readonly ContractInfoReader _contractInfoReader;

        private static Address ConsensusContractAddress =>
            ContractHelpers.GetConsensusContractAddress(ChainConfig.Instance.ChainId.ConvertBase58ToChainId());

        private static Address DividendsContractAddress =>
            ContractHelpers.GetDividendsContractAddress(ChainConfig.Instance.ChainId.ConvertBase58ToChainId());
        
        private static Address TokenContractAddress =>
            ContractHelpers.GetTokenContractAddress(ChainConfig.Instance.ChainId.ConvertBase58ToChainId());
        
        public ElectionInfo(IStateManager stateManager)
        {
            var chainId = ChainConfig.Instance.ChainId.ConvertBase58ToChainId();
            _contractInfoReader = new ContractInfoReader(chainId, stateManager);
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
            var termNumberBytes = await _contractInfoReader.GetBytesAsync<UInt64Value>(ConsensusContractAddress,
                Hash.FromString(GlobalConfig.AElfDPoSCurrentTermNumber));
            var termNumber = UInt64Value.Parser.ParseFrom(termNumberBytes);
            var minersBytes = await _contractInfoReader.GetBytesAsync<Miners>(ConsensusContractAddress,
                Hash.FromMessage(termNumber), GlobalConfig.AElfDPoSMinersMapString);
            var miners = Miners.Parser.ParseFrom(minersBytes);
            return miners.PublicKeys.ToList();
        }
    }
}