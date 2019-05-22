using System.Collections.Generic;

namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable once InconsistentNaming
    public static class ContractErrorCode
    {
        public const int NotFound = 1;

        public const int InvalidField = 2;

        public const int InvalidOperation = 3;

        public const int AttemptFailed = 4;

        public const int NoPermission = 5;
        
        public static readonly Dictionary<int, string> Message = new Dictionary<int, string>
        {
            {NotFound, "Not found"},
            {InvalidField, "Invalid field"},
            {InvalidOperation, "Invalid operation"},
            {AttemptFailed, "Attempt failed"},
            {NoPermission, "No Permission"}
        };

        public static string GetErrorMessage(int errorCode, string furtherInformation = "")
        {
            return furtherInformation == "" ? Message[errorCode] : $"{Message[errorCode]}: {furtherInformation}";
        }
    }
}