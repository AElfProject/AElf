using System;
using System.Linq;
using AElf.Common;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.Infrastructure;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;

namespace AElf.Kernel.Consensus.DPoS.Application
{
    // ReSharper disable once InconsistentNaming
    public class DPoSInformationGenerationService : IConsensusInformationGenerationService
    {
        private readonly DPoSOptions _dpoSOptions;
        private readonly IAccountService _accountService;
        private readonly ConsensusControlInformation _controlInformation;
        private Hash _inValue;

        public DPoSHint Hint => DPoSHint.Parser.ParseFrom(_controlInformation.ConsensusCommand.Hint);

        public ILogger<DPoSInformationGenerationService> Logger { get; set; }

        public DPoSInformationGenerationService(IOptions<DPoSOptions> consensusOptions, IAccountService accountService,
            ConsensusControlInformation controlInformation)
        {
            _dpoSOptions = consensusOptions.Value;
            _accountService = accountService;
            _controlInformation = controlInformation;

            Logger = NullLogger<DPoSInformationGenerationService>.Instance;
        }

        public byte[] GetFirstExtraInformation()
        {
            return new DPoSExtraInformation
            {
                IsBootMiner = _dpoSOptions.IsBootMiner,
                PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex(),
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
            }.ToByteArray();
        }

        public byte[] GenerateExtraInformation()
        {
            try
            {
                switch (Hint.Behaviour)
                {
                    case DPoSBehaviour.InitialTerm:
                        return new DPoSExtraInformation
                        {
                            InitialMiners = {_dpoSOptions.InitialMiners},
                            MiningInterval = DPoSConsensusConsts.MiningInterval,
                            PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex(),
                            IsBootMiner = _dpoSOptions.IsBootMiner
                        }.ToByteArray();

                    case DPoSBehaviour.PackageOutValue:
                        if (_inValue == null)
                        {
                            // For Round 1.
                            _inValue = Hash.Generate();
                            return new DPoSExtraInformation
                            {
                                HashValue = Hash.FromMessage(_inValue),
                                InValue = Hash.Zero,
                                PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex()
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
                                InValue = previousInValue,
                                PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex()
                            }.ToByteArray();
                        }

                    case DPoSBehaviour.NextRound:
                        return new DPoSExtraInformation
                        {
                            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                            PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex()
                        }.ToByteArray();

                    case DPoSBehaviour.NextTerm:
                        return new DPoSExtraInformation
                        {
                            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                            ChangeTerm = true,
                            PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex()
                        }.ToByteArray();

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (NullReferenceException e)
            {
                throw new NullReferenceException(
                    $"Invalid data of generating consensus extra information.\n{e.Message}");
            }
            catch (Exception e)
            {
                throw new Exception($"Unknown exception.\n{e.Message}");
            }
        }

        public byte[] GenerateExtraInformationForTransaction(byte[] consensusInformation, int chainId)
        {
            DPoSInformation information;
            try
            {
                information = DPoSInformation.Parser.ParseFrom(consensusInformation);
            }
            catch (Exception e)
            {
                throw new InvalidCastException($"Failed to parse byte array to DPoSInformation.\n{e.Message}");
            }

            Logger.LogInformation($"Current behaviour: {Hint.Behaviour.ToString()}.");

            try
            {
                switch (Hint.Behaviour)
                {
                    case DPoSBehaviour.InitialTerm:
                        Logger.LogInformation(GetLogStringForOneRound(information.NewTerm.FirstRound));
                        information.NewTerm.ChainId = chainId;
                        information.NewTerm.FirstRound.MiningInterval = _dpoSOptions.MiningInterval;
                        return new DPoSExtraInformation
                        {
                            NewTerm = information.NewTerm
                        }.ToByteArray();

                    case DPoSBehaviour.PackageOutValue:
                        var minersInformation = information.CurrentRound.RealTimeMinersInfo;
                        if (!minersInformation.Any())
                        {
                            Logger.LogError($"Incorrect consensus information.\n{information}");
                        }
                        Logger.LogInformation(GetLogStringForOneRound(information.CurrentRound));
                        var currentMinerInformation = minersInformation.OrderByDescending(m => m.Value.Order).First(m => m.Value.OutValue != null).Value;
                        Logger.LogInformation(
                            $"\nRound number: {information.CurrentRound.RoundNumber}\nOrder: {currentMinerInformation.Order}");
                        return new DPoSExtraInformation
                        {
                            ToPackage = new ToPackage
                            {
                                OutValue = currentMinerInformation.OutValue,
                                RoundId = information.CurrentRound.RoundId,
                                Signature = currentMinerInformation.Signature,
                                PromiseTinyBlocks = currentMinerInformation.PromisedTinyBlocks
                            },
                            ToBroadcast = new ToBroadcast
                            {
                                InValue = _inValue,
                                RoundId = information.CurrentRound.RoundId
                            }
                        }.ToByteArray();

                    case DPoSBehaviour.NextRound:
                        Logger.LogInformation(GetLogStringForOneRound(information.Forwarding.NextRound));
                        return new DPoSExtraInformation
                        {
                            Forwarding = information.Forwarding,
                            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                        }.ToByteArray();

                    case DPoSBehaviour.NextTerm:
                        Logger.LogInformation(GetLogStringForOneRound(information.NewTerm.FirstRound));
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
            catch (NullReferenceException e)
            {
                throw new NullReferenceException(
                    $"Invalid data of generating consensus extra information for transaction.\n{e.Message}");
            }
            catch (Exception e)
            {
                throw new Exception($"Unknown exception.\n{e.Message}");
            }
        }

        private string GetLogStringForOneRound(Round round)
        {
            var logs = $"\nRound {round.RoundNumber}. Round Id: {round.RoundId}";
            foreach (var minerInRound in round.RealTimeMinersInfo.Values.OrderBy(m => m.Order))
            {
                var minerInformation = "\n";
                minerInformation += $"[{minerInRound.PublicKey.Substring(0, 10)}]";
                minerInformation += minerInRound.IsExtraBlockProducer ? "(Current EBP)" : "";
                minerInformation += $"\nAddress::\t {minerInRound.Address}";
                minerInformation += $"\nOrder:\t {minerInRound.Order}";
                minerInformation +=
                    $"\nTime::\t {minerInRound.ExpectedMiningTime.ToDateTime().ToUniversalTime():yyyy-MM-dd HH.mm.ss,fff}";
                minerInformation += $"\nOut:\t {minerInRound.OutValue?.ToHex()}";
                minerInformation += $"\nIn:\t {minerInRound.InValue?.ToHex()}";
                minerInformation += $"\nSig:\t {minerInRound.Signature?.ToHex()}";
                minerInformation += $"\nMine:\t {minerInRound.ProducedBlocks}";
                minerInformation += $"\nMiss:\t {minerInRound.MissedTimeSlots}";
                minerInformation += $"\nLMiss: {minerInRound.LatestMissedTimeSlots}";

                logs += minerInformation;
            }

            return logs;
        }
    }
}