using System;
using AElf.Common;
using AElf.Cryptography.ECDSA;
using Base58Check;
using Google.Protobuf;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class AddressTest
    {
        [Fact]
        public void UsageBuild()
        {
            Random rnd = new Random();
            
            // sha sha of pub key
            KeyPairGenerator kpg = new KeyPairGenerator();
            byte[] kp = kpg.Generate().PublicKey;
            
            byte[] hash = new byte[3];
            rnd.NextBytes(hash);
            
            Address adr = Address.FromPublicKey(hash, kp);
            string adr_formatted = adr.GetFormatted();
            ;
            
            Assert.True(true);
        }
        
        [Fact]
        public void Usage()
        {
            // Chain id prefix
            
            // ------ User side 
            
//            byte[] chainId = Hash.Generate().DumpByteArray();
//            string b58ChainId = Base58CheckEncoding.Encode(chainId);
//            string chainPrefix = b58ChainId.Substring(0, 4);
//            
//            // sha sha of pub key
//            KeyPairGenerator kpg = new KeyPairGenerator();
//            byte[] kp = kpg.Generate().GetEncodedPublicKey();
//            
//            // ------ chain side
//            
//            Address a = new Address();
//            var s = a.ToByteArray();
//            var ds = Address.Parser.ParseFrom(s);
//            
//            Assert.Equal(ds.ChainId, chainPrefix);
//            Assert.Equal(kp, ds.Value);
        }


    }
}