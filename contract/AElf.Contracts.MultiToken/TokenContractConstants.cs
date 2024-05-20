namespace AElf.Contracts.MultiToken;

public static class TokenContractConstants
{
    public const int TokenNameLength = 80;
    public const int MaxDecimals = 18;
    public const int SymbolMaxLength = 10;
    public const int MemoMaxLength = 64;

    public const string PayTxFeeSymbolListName = "SymbolListToPayTxFee";
    public const string PayRentalSymbolListName = "SymbolListToPayRental";

    public const string TransferCallbackExternalInfoKey = "aelf_transfer_callback";
    public const string LockCallbackExternalInfoKey = "aelf_lock_callback";
    public const string UnlockCallbackExternalInfoKey = "aelf_unlock_callback";
    public const string LogEventExternalInfoKey = "aelf_log_event";
    public const int DELEGATEE_MAX_COUNT = 24;
    public const char NFTSymbolSeparator = '-';
    public const int NFTSymbolMaxLength = 30;
    public const string UserContractMethodFeeKey = "UserContractMethodFee";
    public const string CollectionSymbolSuffix = "0";
    public const string SeedCollectionSymbol = "SEED-0";
    public const string SeedOwnedSymbolExternalInfoKey = "__seed_owned_symbol";
    public const string SeedExpireTimeExternalInfoKey = "__seed_exp_time";
    public const string NftCreateChainIdExternalInfoKey = "__nft_create_chain_id";
    public const int DefaultMaxBatchApproveCount = 100;
}