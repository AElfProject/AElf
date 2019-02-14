using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.SmartContractExecution.Domain;
using AElf.Kernel.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.SmartContract.Consensus
{
    public class ElectionInfo : IElectionInfo
    {
        private readonly ContractInfoReader _contractInfoReader;

        public ElectionInfo(IStateManager stateManager)
        {
            _contractInfoReader = new ContractInfoReader(stateManager);
        }

        public async Task<bool> IsCandidate(int chainId, string publicKey)
        {
            var candidatesBytes = await _contractInfoReader.GetBytesAsync<Candidates>(chainId,
                ContractHelpers.GetConsensusContractAddress(chainId),
                Hash.FromString(GlobalConfig.AElfDPoSCandidatesString));
            var candidates = Candidates.Parser.ParseFrom(candidatesBytes);
            return candidates.PublicKeys.Contains(publicKey);
        }

        public async Task<Tickets> GetVotingInfo(int chainId, string publicKey)
        {
            var ticketsBytes = await _contractInfoReader.GetBytesAsync<Tickets>(chainId,
                ContractHelpers.GetConsensusContractAddress(chainId), Hash.FromMessage(publicKey.ToStringValue()),
                GlobalConfig.AElfDPoSCandidatesString);
            var tickets = Tickets.Parser.ParseFrom(ticketsBytes);
            return tickets;
        }

        public async Task<Tuple<ulong, ulong>> GetVotesGeneral(int chainId)
        {
            // TODO NotImplemented
            return await Task.FromResult(default(Tuple<ulong, ulong>));
        }

        public async Task<Round> GetRoundInfo(int chainId, ulong roundNumber)
        {
            var roundBytes = await _contractInfoReader.GetBytesAsync<Round>(chainId,
                ContractHelpers.GetConsensusContractAddress(chainId), Hash.FromMessage(roundNumber.ToUInt64Value()),
                GlobalConfig.AElfDPoSRoundsMapString);
            var round = Round.Parser.ParseFrom(roundBytes);
            return round;
        }

        public async Task<List<string>> GetCurrentMines(int chainId)
        {
            var termNumberBytes = await _contractInfoReader.GetBytesAsync<UInt64Value>(chainId,
                ContractHelpers.GetConsensusContractAddress(chainId),
                Hash.FromString(GlobalConfig.AElfDPoSCurrentTermNumber));
            var termNumber = UInt64Value.Parser.ParseFrom(termNumberBytes);
            var minersBytes = await _contractInfoReader.GetBytesAsync<Miners>(chainId,
                ContractHelpers.GetConsensusContractAddress(chainId), Hash.FromMessage(termNumber),
                GlobalConfig.AElfDPoSMinersMapString);
            var miners = Miners.Parser.ParseFrom(minersBytes);
            return miners.PublicKeys.ToList();
        }
    }
}