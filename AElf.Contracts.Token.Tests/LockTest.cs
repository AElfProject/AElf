using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.Token
{
    public class LockTest
    {
        public ContractTester<TokenContractTestAElfModule> Starter { get; set; }

        public Address ConsensusContractAddress => Starter.GetConsensusContractAddress();

        public LockTest()
        {
            Starter = new ContractTester<TokenContractTestAElfModule>();
            AsyncHelper.RunSync(() => Starter.InitialChainAsync(typeof(TokenContract)));
        }

        [Fact]
        public async Task LockAndUnlockTest()
        {
            const long amount = 100;

            // Create token with consensus contract address in white list.
            await Starter.CreateTokenAsync(ConsensusContractAddress);

            var user = GenerateNewUser();

            var tester = Starter.CreateNewContractTester(user);

            await Starter.IssueTokenAsync(user, amount);

            // Check balance before locking.
            {
                var balance = await Starter.GetBalanceAsync(user);
                balance.ShouldBe(amount);
            }

            var lockId = Hash.Generate();

            // Lock.
            await tester.ExecuteContractWithMiningAsync(tester.GetTokenContractAddress(), nameof(TokenContract.Lock),
                new LockInput
                {
                    From = user,
                    To = ConsensusContractAddress,
                    Amount = amount,
                    Symbol = "ELF",
                    LockId = lockId,
                    Usage = "Testing."
                });

            // Check balance of user after locking.
            {
                var balance = await tester.GetBalanceAsync(user);
                balance.ShouldBe(0);
            }

            // Check balance of lock address after locking.
            {
                var balance =
                    await tester.GetBalanceAsync(GenerateLockAddress(user, ConsensusContractAddress, lockId));
                balance.ShouldBe(amount);
            }

            // Unlock.
            await tester.ExecuteContractWithMiningAsync(tester.GetTokenContractAddress(), nameof(TokenContract.Unlock),
                new UnlockInput
                {
                    From = user,
                    To = ConsensusContractAddress,
                    Amount = amount,
                    Symbol = "ELF",
                    LockId = lockId,
                    Usage = "Testing."
                });

            // Check balance of user after unlocking.
            {
                var balance = await tester.GetBalanceAsync(user);
                balance.ShouldBe(amount);
            }

            // Check balance of lock address after unlocking.
            {
                var balance =
                    await tester.GetBalanceAsync(GenerateLockAddress(user, ConsensusContractAddress, lockId));
                balance.ShouldBe(0);
            }
        }

        private Address GenerateLockAddress(Address from, Address to, Hash lockId)
        {
            var bytes = Address.TakeByAddressLength(ByteArrayHelpers.Combine(
                from.DumpByteArray().Take(TypeConsts.AddressHashLength / 3).ToArray(),
                to.DumpByteArray().Take(TypeConsts.AddressHashLength / 3).ToArray(),
                lockId.DumpByteArray()));
            return Address.FromBytes(bytes);
        }

        private static User GenerateNewUser()
        {
            var callKeyPair = CryptoHelpers.GenerateKeyPair();
            var callAddress = Address.FromPublicKey(callKeyPair.PublicKey);
            var callPublicKey = callKeyPair.PublicKey.ToHex();

            return new User
            {
                KeyPair = callKeyPair,
                Address = callAddress,
                PublicKey = callPublicKey
            };
        }

        private struct User
        {
            public ECKeyPair KeyPair { get; set; }
            public Address Address { get; set; }
            public string PublicKey { get; set; }

            public static implicit operator ECKeyPair(User user)
            {
                return user.KeyPair;
            }

            public static implicit operator Address(User user)
            {
                return user.Address;
            }

            public static implicit operator string(User user)
            {
                return user.PublicKey;
            }
        }
    }
}