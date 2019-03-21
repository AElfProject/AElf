using System;
using System.Linq;
using AElf.Common;
using AElf.Consensus.DPoS;
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

        public DPoSTriggerInformation GetTriggerInformation()
        {
            if (_controlInformation.ConsensusCommand == null)
            {
                return new DPoSTriggerInformation
                {
                    IsBootMiner = _dpoSOptions.IsBootMiner,
                    PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex(),
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                    InitialTermNumber = _dpoSOptions.InitialTermNumber
                };
            }

            switch (Hint.Behaviour)
            {
                case DPoSBehaviour.InitialConsensus:
                    return new DPoSTriggerInformation
                    {
                        PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex(),
                        Timestamp = DateTime.UtcNow.ToTimestamp(),
                        Miners = {_dpoSOptions.InitialMiners},
                        InitialTermNumber = _dpoSOptions.InitialTermNumber,
                        IsBootMiner = _dpoSOptions.IsBootMiner,
                        MiningInterval = _dpoSOptions.MiningInterval
                    };
                case DPoSBehaviour.UpdateValue:
                    if (_inValue == null)
                    {
                        // First Round.
                        _inValue = Hash.Generate();
                        return new DPoSTriggerInformation
                        {
                            PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex(),
                            Timestamp = DateTime.UtcNow.ToTimestamp(),
                            PreviousInValue = Hash.Empty,
                            CurrentInValue = _inValue
                        };
                    }
                    
                    var previousInValue = _inValue;
                    _inValue = Hash.Generate();
                    return new DPoSTriggerInformation
                    {
                        PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex(),
                        Timestamp = DateTime.UtcNow.ToTimestamp(),
                        PreviousInValue = previousInValue,
                        CurrentInValue = _inValue
                    };
                case DPoSBehaviour.NextRound:
                    return new DPoSTriggerInformation
                    {
                        PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex(),
                        Timestamp = DateTime.UtcNow.ToTimestamp()
                    };
                case DPoSBehaviour.NextTerm:
                    return new DPoSTriggerInformation
                    {
                        PublicKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex(),
                        Timestamp = DateTime.UtcNow.ToTimestamp()
                    };
                case DPoSBehaviour.Invalid:
                    throw new InvalidOperationException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}