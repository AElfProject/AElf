using System;
using System.Threading;
using AElf.Common;
using AElf.Cryptography.ECDSA;

namespace AElf.Cryptography
{
    public class OpenAccount
    {
        // Close account when time out 
        public Timer CloseTimer { private get; set; }
        
        public ECKeyPair KeyPair { get; set; }
        public string AccountName { get; }

        public OpenAccount(string address)
        {
            AccountName = address;
        }

        public void Close()
        {
            CloseTimer.Dispose();
        }
    }
}