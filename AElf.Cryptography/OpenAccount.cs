﻿using System;
using System.Threading;
using AElf.Cryptography.ECDSA;

namespace AElf.Cryptography
{
    public class OpenAccount
    {
        public Timer Timer { get; set; }
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
            Timer.Dispose();
        }
    }
}