using Google.Protobuf;

namespace AElf.Contracts.Consensus.AEDPoS;

public partial class NextRoundInput
{
    public static NextRoundInput Create(Round round, ByteString randomNumber)
    {
        return new NextRoundInput
        {
            RoundNumber = round.RoundNumber,
            RealTimeMinersInformation = { round.RealTimeMinersInformation },
            ExtraBlockProducerOfPreviousRound = round.ExtraBlockProducerOfPreviousRound,
            BlockchainAge = round.BlockchainAge,
            TermNumber = round.TermNumber,
            ConfirmedIrreversibleBlockHeight = round.ConfirmedIrreversibleBlockHeight,
            ConfirmedIrreversibleBlockRoundNumber = round.ConfirmedIrreversibleBlockRoundNumber,
            IsMinerListJustChanged = round.IsMinerListJustChanged,
            RoundIdForValidation = round.RoundIdForValidation,
            MainChainMinersRoundNumber = round.MainChainMinersRoundNumber,
            RandomNumber = randomNumber
        };
    }

    public Round ToRound()
    {
        return new Round
        {
            RoundNumber = RoundNumber,
            RealTimeMinersInformation = { RealTimeMinersInformation },
            ExtraBlockProducerOfPreviousRound = ExtraBlockProducerOfPreviousRound,
            BlockchainAge = BlockchainAge,
            TermNumber = TermNumber,
            ConfirmedIrreversibleBlockHeight = ConfirmedIrreversibleBlockHeight,
            ConfirmedIrreversibleBlockRoundNumber = ConfirmedIrreversibleBlockRoundNumber,
            IsMinerListJustChanged = IsMinerListJustChanged,
            RoundIdForValidation = RoundIdForValidation,
            MainChainMinersRoundNumber = MainChainMinersRoundNumber
        };
    }
}