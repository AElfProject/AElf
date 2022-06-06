using System.Collections.Generic;

namespace AElf.WebApp.Application.Chain;

public static class Error
{
    public const int NotFound = 20001;
    public const int InvalidAddress = 20002;
    public const int InvalidBlockHash = 20003;
    public const int InvalidTransactionId = 20004;
    public const int InvalidOffset = 20006;
    public const int InvalidLimit = 20007;
    public const int InvalidTransaction = 20008;
    public const int InvalidContractAddress = 20010;
    public const int NoMatchMethodInContractAddress = 20011;
    public const int InvalidParams = 20012;
    public const int InvalidSignature = 20013;
    public const string NeedBasicAuth = "User name and password for basic auth should be set";

    public static readonly Dictionary<int, string> Message = new()
    {
        { NotFound, "Not found" },
        { InvalidAddress, "Invalid address format" },
        { InvalidBlockHash, "Invalid block hash format" },
        { InvalidTransactionId, "Invalid Transaction id format" },
        { InvalidOffset, "Offset must greater than or equal to 0" },
        { InvalidLimit, "Limit must between 0 and 100" },
        { InvalidTransaction, "Invalid transaction information" },
        { InvalidContractAddress, "Invalid contract address" },
        { NoMatchMethodInContractAddress, "No match method in contract address" },
        { InvalidParams, "Invalid params" },
        { InvalidSignature, "Invalid signature" }
    };
}