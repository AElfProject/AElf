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

        public byte[] GetTriggerInformation()
        {
            if (_controlInformation.ConsensusCommand == null)
            {
                return new DPoSTriggerInformation
                {
                    IsBootMiner = _dpoSOptions.IsBootMiner,
                    PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex(),
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                }.ToByteArray();
            }
            
            switch (Hint.Behaviour)
            {
                case DPoSBehaviour.InitialConsensus:
                    return new DPoSTriggerInformation
                    {
                        PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex(),
                        Timestamp = DateTime.UtcNow.ToTimestamp(),
                        Miners = {_dpoSOptions.InitialMiners},
                        MiningInterval = DPoSConsensusConsts.MiningInterval,
                    }.ToByteArray();
                case DPoSBehaviour.UpdateValue:
                    if (_inValue == null)
                    {
                        // First Round.
                        _inValue = Hash.Generate();
                        return new DPoSTriggerInformation
                        {
                            PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex(),
                            Timestamp = DateTime.UtcNow.ToTimestamp(),
                            PreviousInValue = Hash.Default,
                            CurrentInValue = _inValue
                        }.ToByteArray();
                    }
                    
                    var previousInValue = _inValue;
                    _inValue = Hash.Generate();
                    return new DPoSTriggerInformation
                    {
                        PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex(),
                        Timestamp = DateTime.UtcNow.ToTimestamp(),
                        PreviousInValue = previousInValue,
                        CurrentInValue = _inValue
                    }.ToByteArray();
                case DPoSBehaviour.NextRound:
                    return new DPoSTriggerInformation
                    {
                        PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex(),
                        Timestamp = DateTime.UtcNow.ToTimestamp()
                    }.ToByteArray();
                case DPoSBehaviour.NextTerm:
                    return new DPoSTriggerInformation
                    {
                        PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex(),
                        Timestamp = DateTime.UtcNow.ToTimestamp()
                    }.ToByteArray();
                case DPoSBehaviour.Invalid:
                    throw new InvalidOperationException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string GetLogStringForOneRound(Round round)
        {
            var logs = $"\n[Round {round.RoundNumber}](Round Id: {round.RoundId})";
            foreach (var minerInRound in round.RealTimeMinersInformation.Values.OrderBy(m => m.Order))
            {
                var minerInformation = "\n";
                minerInformation += $"[{minerInRound.PublicKey.Substring(0, 10)}]";
                minerInformation += minerInRound.IsExtraBlockProducer ? "(Current EBP)" : "";
                minerInformation +=
                    minerInRound.PublicKey == AsyncHelper.RunSync(() => _accountService.GetPublicKeyAsync()).ToHex()
                        ? "(This Node)"
                        : "";
                minerInformation += $"\nOrder:\t {minerInRound.Order}";
                minerInformation +=
                    $"\nTime:\t {minerInRound.ExpectedMiningTime.ToDateTime().ToUniversalTime():yyyy-MM-dd HH.mm.ss,fff}";
                minerInformation += $"\nOut:\t {minerInRound.OutValue?.ToHex()}";
                minerInformation += $"\nPreIn:\t {minerInRound.PreviousInValue?.ToHex()}";
                minerInformation += $"\nSig:\t {minerInRound.Signature?.ToHex()}";
                minerInformation += $"\nMine:\t {minerInRound.ProducedBlocks}";
                minerInformation += $"\nMiss:\t {minerInRound.MissedTimeSlots}";
                minerInformation += $"\nLMiss:\t {minerInRound.LatestMissedTimeSlots}";

                logs += minerInformation;
            }

            return logs;
        }
    }
}