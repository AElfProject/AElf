namespace AElf.Contracts.EconomicSystem.Tests
{
    public class EconomicSystemTestConstants
    {
        public const string NativeTokenSymbol = "ELF";
        public const string ConverterTokenSymbol = "AETC";
        public const string TransactionFeeChargingContractTokenSymbol = "TFCC";
        public const int Decimals = 8;
        public const bool IsBurnable = true;
        public const long TotalSupply = 1_000_000_000_00000000;

        public const int InitialCoreDataCenterCount = 5;
        public const int CoreDataCenterCount = 7;
        public const int ValidateDataCenterCount = 35;
        public const int ValidateDataCenterCandidateCount = 13;
        public const int VoterCount = 10;

        public const int MiningInterval = 4000;
    }
}