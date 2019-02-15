using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Consensus.DPoS
{
    // ReSharper disable once InconsistentNaming
    public class DPoSInformationGenerationService : IConsensusInformationGenerationService
    {
        private readonly IMinersManager _minersManager;
        private DPoSCommand _command;
        private Hash _inValue;

        public DPoSInformationGenerationService(IMinersManager minersManager)
        {
            _minersManager = minersManager;
        }
        
        public async Task<byte[]> GenerateExtraInformationAsync()
        {
            switch (_command.Behaviour)
            {
                case DPoSBehaviour.InitialTerm:
                    return new DPoSExtraInformation
                    {
                        InitialMiners = {(await _minersManager.GetMiners(0)).PublicKeys},
                        MiningInterval = DPoSConsensusConsts.MiningInterval,
                    }.ToByteArray();
                
                case DPoSBehaviour.PackageOutValue:
                    _inValue = Hash.Generate();
                    return new DPoSExtraInformation
                    {
                        HashValue = Hash.FromMessage(_inValue)
                    }.ToByteArray();
                
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
                
                case DPoSBehaviour.PublishInValue:
                    return null;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task<byte[]> GenerateExtraInformationForTransactionAsync(byte[] consensusInformation)
        {
            var information = DPoSInformation.Parser.ParseFrom(consensusInformation);

            switch (_command.Behaviour)
            {
                case DPoSBehaviour.InitialTerm:
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
                        }
                    }.ToByteArray();
                
                case DPoSBehaviour.PublishInValue:
                    return new DPoSExtraInformation
                    {
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

        public void Tell(byte[] consensusCommand)
        {
            _command = DPoSCommand.Parser.ParseFrom(consensusCommand);
        }
    }
}