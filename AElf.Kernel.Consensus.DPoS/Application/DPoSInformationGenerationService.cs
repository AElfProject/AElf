using System;
using System.Linq;
using AElf.Common;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.Infrastructure;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AElf.Kernel.Consensus.DPoS.Application
{
    // ReSharper disable once InconsistentNaming
    public class DPoSInformationGenerationService : IConsensusInformationGenerationService
    {
        private readonly DPoSOptions _dpoSOptions;
        private readonly IConsensusCommand _command;
        private Hash _inValue;

        public DPoSCommand Command => DPoSCommand.Parser.ParseFrom(_command.Command);

        public ILogger<DPoSInformationGenerationService> Logger { get; set; }

        public DPoSInformationGenerationService(IOptions<DPoSOptions> consensusOptions, IConsensusCommand command)
        {
            _dpoSOptions = consensusOptions.Value;
            _command = command;

            Logger = NullLogger<DPoSInformationGenerationService>.Instance;
        }

        public byte[] GenerateExtraInformation()
        {
            switch (Command.Behaviour)
            {
                case DPoSBehaviour.InitialTerm:
                    return new DPoSExtraInformation
                    {
                        InitialMiners = {_dpoSOptions.InitialMiners},
                        MiningInterval = DPoSConsensusConsts.MiningInterval,
                    }.ToByteArray();

                case DPoSBehaviour.PackageOutValue:
                    if (_inValue == null)
                    {
                        // For Round 1.
                        _inValue = Hash.Generate();
                        return new DPoSExtraInformation
                        {
                            HashValue = Hash.FromMessage(_inValue),
                            InValue = Hash.Zero
                        }.ToByteArray();
                    }
                    else
                    {
                        var previousInValue = _inValue;
                        var outValue = Hash.FromMessage(_inValue);
                        _inValue = Hash.Generate();
                        return new DPoSExtraInformation
                        {
                            HashValue = outValue,
                            InValue = previousInValue
                        }.ToByteArray();
                    }

                case DPoSBehaviour.NextRound:
                    return new DPoSExtraInformation
                    {
                        Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                    }.ToByteArray();

                case DPoSBehaviour.NextTerm:
                    return new DPoSExtraInformation
                    {
                        Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                        ChangeTerm = true
                    }.ToByteArray();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public byte[] GenerateExtraInformationForTransaction(byte[] consensusInformation, int chainId)
        {
            var information = DPoSInformation.Parser.ParseFrom(consensusInformation);

            switch (Command.Behaviour)
            {
                case DPoSBehaviour.InitialTerm:
                    information.NewTerm.ChainId = chainId;
                    information.NewTerm.FirstRound.MiningInterval = _dpoSOptions.MiningInterval;
                    return new DPoSExtraInformation
                    {
                        NewTerm = information.NewTerm
                    }.ToByteArray();

                case DPoSBehaviour.PackageOutValue:
                    var currentMinerInformation = information.CurrentRound.RealTimeMinersInfo
                        .OrderByDescending(m => m.Value.Order).First(m => m.Value.OutValue != null).Value;
                    return new DPoSExtraInformation
                    {
                        ToPackage = new ToPackage
                        {
                            OutValue = currentMinerInformation.OutValue,
                            RoundId = information.CurrentRound.RoundId,
                            Signature = currentMinerInformation.Signature
                        },
                        ToBroadcast = new ToBroadcast
                        {
                            InValue = _inValue,
                            RoundId = information.CurrentRound.RoundId
                        }
                    }.ToByteArray();

                case DPoSBehaviour.NextRound:
                    return new DPoSExtraInformation
                    {
                        Forwarding = information.Forwarding,
                        Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                    }.ToByteArray();

                case DPoSBehaviour.NextTerm:
                    return new DPoSExtraInformation
                    {
                        Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                        ChangeTerm = true,
                        NewTerm = information.NewTerm
                    }.ToByteArray();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}