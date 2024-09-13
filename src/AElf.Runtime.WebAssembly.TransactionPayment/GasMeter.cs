using AElf.Runtime.WebAssembly.TransactionPayment.Extensions;

namespace AElf.Runtime.WebAssembly.TransactionPayment;

public class GasMeter : IGasMeter
{
    public Weight GasLimit { get; set; }
    public Weight GasLeft { get; private set; }
    public long EngineConsumed { get; set; }

    public GasMeter(Weight? gasLimit)
    {
        GasLimit = gasLimit ?? new Weight();
        GasLeft = GasLimit;
    }

    // TODO: Estimate in the future.
    public Weight ChargeGas(RuntimeCost runtimeCost)
    {
        var cost = runtimeCost switch
        {
            // Instantiate => new Weight(397_415_227, 6471),
            // Call => new Weight(214_770_000, 6733),
            Caller => new Weight(295_521_846, 6773),
            IsContract => new Weight(132_998_512, 6793),
            // SetCodeHash => new Weight(104_825_618, 6797),
            CallerIsOrigin => new Weight(284_371_703, 6771),
            CallerIsRoot => new Weight(273_052_488, 6660),
            GetAddress => new Weight(289_240_775, 6774),
            TransactionPayment.GasLeft => new Weight(288_660_978, 6773),
            Balance => new Weight(298_889_279, 6898),
            ValueTransferred => new Weight(284_293_851, 6790),
            MinimumBalance => new Weight(294_688_466, 6783),
            BlockNumber => new Weight(287_964_188, 6786),
            Now => new Weight(290_797_828, 6771),
            WeightToFee => new Weight(295_010_878, 6839),
            SealInput => new Weight(285_824_773, 6774),
            SealInputPerByte => new Weight(232_759_442, 6776),
            SealReturn c => new Weight(278_155_436, 6760).Add(FromBytesSize(c.BytesSize)),
            SealReturnPerByte => new Weight(285_765_697, 6777),
            Terminate => new Weight(310_023_381, 8879),
            Random => new Weight(295_931_189, 6852),
            DepositEvent c => new Weight(301_859_471, 6772).Add(FromBytesSize(c.TopicNumber * c.Length)),
            DebugMessage c => new Weight(179_567_029, 6774).Add(FromBytesSize(c.BytesSize)),
            SetStorage c => new Weight(171_270_899, 892).Add(FromBytesSize(c.OldBytesSize)
                .Add(FromBytesSize(c.NewBytesSize))),
            ClearStorage c => new Weight(177_898_012, 893).Add(FromBytesSize(c.BytesSize)),
            GetStorage c => new Weight(200_488_939, 889).Add(FromBytesSize(c.BytesSize)),
            ContainsStorage c => new Weight(202_164_395, 895).Add(FromBytesSize(c.BytesSize)),
            TakeStorage c => new Weight(190_468_808, 885).Add(FromBytesSize(c.BytesSize)),
            Transfer => new Weight(278_290_000, 7274),
            Call => new Weight(281_775_000, 9407),
            DelegateCall => new Weight(279_888_000, 6779),
            Instantiate => new Weight(671_726_000, 9587),
            HashSha256 c => new Weight(286_520_166, 6768).Add(FromBytesSize(c.BytesSize)),
            HashKeccak256 c => new Weight(283_273_283, 6773).Add(FromBytesSize(c.BytesSize)),
            HashBlake2256 c => new Weight(282_900_345, 6775).Add(FromBytesSize(c.BytesSize)),
            HashBlake2128 c => new Weight(289_853_116, 6772).Add(FromBytesSize(c.BytesSize)),
            Sr25519Verify c => new Weight(336_398_814, 6715).Add(FromBytesSize(c.BytesSize)),
            EcdsaRecovery => new Weight(340_672_829, 6768),
            EcdsaToEthAddress => new Weight(313_700_348, 6783),
            SetCodeHash => new Weight(275_643_000, 6774),
            AddDelegateDependency => new Weight(295_443_439, 6845),
            RemoveDelegateDependency => new Weight(297_493_194, 129453),
            ReentranceCount => new Weight(287_220_765, 6771),
            AccountReentranceCount => new Weight(331_187_665, 7866),
            InstantiationNonce => new Weight(286_686_654, 6768),
            CopyToContract c => new Weight(232_759_442, 6776).Mul(c.ContractBytesSize),
            _ => throw new ArgumentOutOfRangeException(nameof(runtimeCost), runtimeCost, null)
        };
        //GasLeft = GasLeft.Sub(cost);
        return GasLeft;
    }

    private static Weight FromBytesSize(int bytesSize)
    {
        return new Weight(0, bytesSize * WebAssemblyTransactionPaymentConstants.TransactionByteFee);
    }
}