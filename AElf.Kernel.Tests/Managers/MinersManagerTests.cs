using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration;
using AElf.Consensus.DPoS;
using AElf.Kernel.Managers;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Tests.Managers
{
    public sealed class MinersManagerTests : AElfKernelTestBase
    {
        private IMinersManager _minersManager;

        private readonly DPoSOptions _options;

        public MinersManagerTests()
        {
            _options = new DPoSOptions
            {
                InitialMiners = CreateMiners(3)
            };

            _minersManager = GetRequiredService<MinersManager>();
        }

        [Fact]
        public async Task SetMiners_Test()
        {
            var address = Address.Generate();
            var pubKey = address.GetPublicKeyHash();
            var miner = new Miners()
            {
                TermNumber = 1,
                PublicKeys = {pubKey}
            };
            await _minersManager.SetMiners(miner);
            var miner1 = await _minersManager.GetMiners(1);
            miner1.PublicKeys.Count.ShouldBe(1);
        }

        [Fact]
        public async Task SetMiners_UpdateMinersVersion_Test()
        {
            await _minersManager.SetMiners(new Miners
            {
                TermNumber = 1,
                PublicKeys = {Address.Generate().GetPublicKeyHash()},
                MainchainLatestTermNumber = 1
            });

            var miner11 = await _minersManager.GetMiners(1);
            miner11.MainchainLatestTermNumber.ShouldBe<ulong>(1);

            await _minersManager.SetMiners(new Miners
            {
                TermNumber = 2,
                PublicKeys = {Address.Generate().GetPublicKeyHash()}
            });

            var miner12 = await _minersManager.GetMiners(1);
            miner12.MainchainLatestTermNumber.ShouldBe<ulong>(2);
        }

        [Fact]
        public async Task IsMinerInDatabase_Test()
        {
            var result = await _minersManager.IsMinersInDatabase();
            result.ShouldBe(false);
        }

        private List<string> CreateMiners(int count)
        {
            var publicKeys = new List<string>();

            for (var i = 0; i < count; i++)
            {
                publicKeys.Add(Address.Generate().GetPublicKeyHash());
            }

            return publicKeys;
        }
    }
}