using System;
using System.Linq;
using AElf.Consensus.AElfConsensus;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AElfConsensus
{
    public partial class AElfConsensusContract
    {
        public override ConsensusCommand GetConsensusCommand(CommandInput input)
        {
            Assert(input.PublicKey.Any(), "Invalid public key.");

            var behaviour = GetBehaviour(input.PublicKey.ToHex(), Context.CurrentBlockTime, out var currentRound);

            if (behaviour == AElfConsensusBehaviour.Nothing)
            {
                return new ConsensusCommand
                {
                    ExpectedMiningTime = DateTime.MaxValue.ToUniversalTime().ToTimestamp(),
                    Hint = ByteString.CopyFrom(new AElfConsensusHint {Behaviour = behaviour}.ToByteArray()),
                    LimitMillisecondsOfMiningBlock = int.MaxValue, NextBlockMiningLeftMilliseconds = int.MaxValue
                };
            }

            Assert(currentRound != null && currentRound.RoundId != 0, "Consensus not initialized.");

            var command = GetConsensusCommand(behaviour, currentRound, input.PublicKey.ToHex(), Context.CurrentBlockTime);

            Context.LogDebug(() =>
                currentRound.GetLogs(input.PublicKey.ToHex(), AElfConsensusHint.Parser.ParseFrom(command.Hint).Behaviour));

            return command;
        }
    }
}