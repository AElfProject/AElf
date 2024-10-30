using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ContractTestKit;
using AElf.Types;
using Scale;
using Shouldly;
using Xunit.Abstractions;

namespace AElf.Contracts.SolidityContract;

public sealed class ArraysTests : SolidityContractTestBase
{
    public ArraysTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        ContractPath = "contracts/arrays.contract";
    }

    private record User(ulong Id, string Address, string Name, List<Permission> Permissions);

    [Fact(DisplayName = "Test arrays contract.")]
    public async Task TestArray()
    {
        var contractAddress = await DeployContractAsync();
        var users = new List<User>();
        for (var i = 0; i < 3; i++)
        {
            var address = SampleAddress.AddressList[i].ToBase58();
            var name = $"name{i}";
            var id = (ulong)(1000 + i);

            var randomPerm = 1; //new Random().Next(0, 6);
            var perms = new List<Permission>
            {
                (Permission)randomPerm,
                (Permission)(randomPerm + 1)
            };

            var permissions = perms.Select(EnumType<Permission>.From).ToArray();
            await ExecuteTransactionAsync(contractAddress, "addUser",
                TupleType<UInt64Type, BytesType, StringType, VecType<EnumType<Permission>>>
                    .GetByteStringFrom(
                        UInt64Type.From(id),
                        BytesType.From(Address.FromBase58(address).ToByteArray()),
                        //new AddressType(Address.FromBase58(address)),
                        StringType.From(name),
                        VecType<EnumType<Permission>>.From(permissions)
                    ));
            users.Add(new User(id, address, name, perms));
        }

        var firstUser = users.First();

        // Test first user's data is saved correctly.
        {
            var queriedUserByteString = await QueryAsync(contractAddress, "getUserById",
                UInt64Type.GetByteStringFrom(firstUser.Id));
            var queriedUser =
                TupleType<StringType, AddressType, UInt64Type, VecType<EnumType<Permission>>>
                    .From(queriedUserByteString.ToByteArray());
            StringType.From(queriedUser.Value[0].Encode()).ToString().ShouldBe(firstUser.Name);
            AddressType.From(queriedUser.Value[1].Encode()).Value.ToBase58().ShouldBe(firstUser.Address);
            UInt64Type.From(queriedUser.Value[2].Encode()).Value.ShouldBe(firstUser.Id);
            VecType<EnumType<Permission>>.From(queriedUser.Value[3].Encode()).Value.Select(p => p.Value)
                .ShouldBe(firstUser.Permissions);
        }

        // Test hasPermission method.
        {
            var hasPermission = await QueryAsync(contractAddress, "hasPermission",
                TupleType<UInt64Type, EnumType<Permission>>.GetByteStringFrom(
                    UInt64Type.From(firstUser.Id),
                    EnumType<Permission>.From(firstUser.Permissions.First())
                ));
            BoolType.From(hasPermission.ToByteArray()).Value.ShouldBeTrue();
        }

        {
            var hasPermission = await QueryAsync(contractAddress, "hasPermission",
                TupleType<UInt64Type, EnumType<Permission>>.GetByteStringFrom(
                    UInt64Type.From(firstUser.Id),
                    EnumType<Permission>.From(firstUser.Permissions.Last() + 1)
                ));
            BoolType.From(hasPermission.ToByteArray()).Value.ShouldBeFalse();
        }

        // Test getUserByAddress method.
        {
            var queriedUserByteString = await QueryAsync(contractAddress, "getUserByAddress",
                BytesType.GetByteStringFrom(Address.FromBase58(firstUser.Address).ToByteArray()));
            var queriedUser =
                TupleType<StringType, AddressType, UInt64Type, VecType<EnumType<Permission>>>
                    .From(queriedUserByteString.ToByteArray());
            StringType.From(queriedUser.Value[0].Encode()).ToString().ShouldBe(firstUser.Name);
            AddressType.From(queriedUser.Value[1].Encode()).Value.ToBase58().ShouldBe(firstUser.Address);
            UInt64Type.From(queriedUser.Value[2].Encode()).Value.ShouldBe(firstUser.Id);
            VecType<EnumType<Permission>>.From(queriedUser.Value[3].Encode()).Value.Select(p => p.Value)
                .ShouldBe(firstUser.Permissions);
        }

        // Test removeUser method.
        {
            await ExecuteTransactionAsync(contractAddress, "removeUser", UInt64Type.GetByteStringFrom(firstUser.Id));
            var isUserExists =
                await QueryAsync(contractAddress, "userExists", UInt64Type.GetByteStringFrom(firstUser.Id));
            BoolType.From(isUserExists.ToByteArray()).Value.ShouldBeFalse();
        }
    }

    private enum Permission
    {
        Perm1,
        Perm2,
        Perm3,
        Perm4,
        Perm5,
        Perm6,
        Perm7,
        Perm8
    }
}