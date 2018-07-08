using System;
using System.Threading;
using AElf.Cryptography.ECDSA;

namespace AElf.Cryptography
{
    public class OpenAccount
    {
        // Close account when time out 
        public Timer CloseTimer { private get; set; }
        public ECKeyPair KeyPair { get; set; }

        public OpenAccount()
        {
        }
        
        public string Address
        {
            get { return KeyPair.GetAddressHex(); }
        }

        public void Close()
        {
            CloseTimer.Dispose();
        }
    }
}