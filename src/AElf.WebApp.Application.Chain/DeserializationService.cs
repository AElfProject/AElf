using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.WebApp.Application.Chain.Dto;
using Google.Protobuf;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain
{
    public interface IDeserializationService : IApplicationService
    {
        RoundDto GetRoundFromBase64(string str);
    }

    public class DeserializationService : IDeserializationService
    {
        public RoundDto GetRoundFromBase64(string str)
        {
            var bytes = ByteString.FromBase64(str);
            var round = Round.Parser.ParseFrom(bytes);
            return new RoundDto
            {
                RoundNumber = round.RoundNumber,
                RealTimeMinerInformation = round.RealTimeMinersInformation.ToDictionary(i => i.Key, i =>
                    new MinerInRoundDto
                    {
                        ExpectedMiningTime = i.Value.ExpectedMiningTime.ToDateTime(),
                        ActualMiningTimes = i.Value.ActualMiningTimes?.Select(d => d.ToDateTime()).ToList(),
                        InValue = i.Value?.InValue.ToHex(),
                        PreviousInValue = i.Value?.PreviousInValue.ToHex(),
                        MissedBlocks = i.Value.MissedTimeSlots,
                        Order = i.Value.Order,
                        OutValue = i.Value?.OutValue.ToHex(),
                        ProducedBlocks = i.Value.ProducedBlocks,
                        ProducedTinyBlocks = i.Value.ProducedTinyBlocks,
                        ImpliedIrreversibleBlockHeight = i.Value.ImpliedIrreversibleBlockHeight
                    }),
                ExtraBlockProducerOfPreviousRound = round.ExtraBlockProducerOfPreviousRound,
                TermNumber = round.TermNumber,
                ConfirmedIrreversibleBlockHeight = round.ConfirmedIrreversibleBlockHeight,
                ConfirmedIrreversibleBlockRoundNumber = round.ConfirmedIrreversibleBlockRoundNumber,
                IsMinerListJustChanged = round.IsMinerListJustChanged,
                RoundId = round.RealTimeMinersInformation.Values.Select(bpInfo => bpInfo.ExpectedMiningTime.Seconds)
                    .Sum()
            };
        }
    }
}