using System.Collections.Generic;

namespace AElf.OS.Rpc.Wallet
{
    public static class Error
    {
        public const long CreateAccountFailed = 10001;
        public const long WrongPassword = 10002;
        public const long AccountNotExist = 10003;

        public static readonly Dictionary<long, string> Message = new Dictionary<long, string>
        {
            {CreateAccountFailed, "Create account failed"},
            {WrongPassword, "Wrong password"},
            {AccountNotExist, "Account not exist"}
        };
    }
}