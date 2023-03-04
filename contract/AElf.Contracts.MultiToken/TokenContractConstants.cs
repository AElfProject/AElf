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
    public const int DELEGATEE_MAX_COUNT = 128;
    public const char NFTSymbolSeparator = '-';
    public const int NFTSymbolMaxLength = 30;
}