/*
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
//using AElf.Configuration;
//using AElf.Kernel.Managers;
using Shouldly;
using Xunit;
*/

namespace AElf.Kernel.Tests
{
    /*
    public sealed class MinersManagerTests:AElfKernelTestBase
    {
        private IMinersManager _minersManager;
        private int _chainId;

        public MinersManagerTests()
        {
            _minersManager = GetRequiredService<MinersManager>();
            MinersConfig.Instance = new MinersConfig()
            {
                Producers = InitMiners(3)
            };
            _chainId = "AELF".ConvertBase58ToChainId();
        }

        [Fact]
        public async Task GetMiners_Test()
        {
            var miners = await _minersManager.GetMiners(1);
            miners.PublicKeys.Count.ShouldBe(3);
        }

        [Fact]
        public async Task SetMiners_Test()
        {
            var address = Address.Generate();
            var pubKey = address.GetPublicKeyHash();
            var miner = new Miners()
            {
                TermNumber = 2,
                PublicKeys = { pubKey }
            };
            await _minersManager.SetMiners(miner, _chainId);
            var miner1 = await _minersManager.GetMiners(2);
            miner1.PublicKeys.Count.ShouldBe(1);
        }

        [Fact]
        public async Task IsMinerInDatabase_Test()
        {
            var result = await _minersManager.IsMinersInDatabase();
            result.ShouldBe(false);
        }

        private Dictionary<string, Dictionary<string, string>> InitMiners(int count)
        {
            var miners = new Dictionary<string, Dictionary<string, string>>();

            for (int i = 1; i <= count; i++)
            {
                var address = Address.Generate();
                var miner = new Dictionary<string, string>
                {
                    {"public_key", address.GetPublicKeyHash()},
                    {"address", address.GetFormatted()}
                };

                miners.Add(i.ToString(), miner);
            }

            return miners;
        }

    }
    */
}