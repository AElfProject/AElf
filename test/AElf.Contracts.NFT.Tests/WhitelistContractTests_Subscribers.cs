using System.Threading.Tasks;
using AElf.Contracts.Whitelist;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Contracts.NFT
{
    public partial class WhitelistContractTests
    {
        [Fact]
        public async Task<Hash> SubscribeWhitelistTest()
        {
            var whitelistId = await CreateWhitelistTest();
            var projectId = HashHelper.ComputeFrom("Project");
            var executionResult = await WhitelistContractStub.SubscribeWhitelist.SendAsync(
                new SubscribeWhitelistInput() 
                {
                    WhitelistId = whitelistId,
                    ProjectId = projectId
                });
            var subscribeId = executionResult.Output;
            var subscribe = await WhitelistContractStub.GetSubscribeWhitelist.CallAsync(subscribeId);
            subscribe.ProjectId.ShouldBe(projectId);
            subscribe.WhitelistId.ShouldBe(whitelistId);
            return subscribeId;
        }
        
        [Fact]
        public async Task SubscribeWhitelistTest_WhitelistNotAvailable()
        {
            var whitelistId = await CreateWhitelistTest();
            var projectId = new Hash();
            var executionResult = await WhitelistContractStub.SubscribeWhitelist.SendWithExceptionAsync(
                new SubscribeWhitelistInput() 
                {
                    WhitelistId = new Hash(),
                    ProjectId = projectId
                });
            executionResult.TransactionResult.Error.ShouldContain("Whitelist not found.");
        }
        
        [Fact]
        public async Task UnsubscribeWhitelistTest()
        {
            var subscribeId = await SubscribeWhitelistTest();
            await WhitelistContractStub.UnsubscribeWhitelist.SendAsync(subscribeId);
            var consumedList = await WhitelistContractStub.GetConsumedList.CallAsync(subscribeId);
            consumedList.ExtraInfoIdList.ShouldBeNull();
        }

        [Fact]
        public async Task<Hash> ConsumeWhitelistTest()
        {
            var subscribeId = await SubscribeWhitelistTest();
            var subscribe = await WhitelistContractStub.GetSubscribeWhitelist.CallAsync(subscribeId);
            var tagId = await WhitelistContractStub.GetTagIdByAddress.CallAsync(new GetTagIdByAddressInput()
            {
                WhitelistId = subscribe.WhitelistId,
                Address = User1Address
            });
            await WhitelistContractStub.ConsumeWhitelist.SendAsync(new ConsumeWhitelistInput()
            {
                SubscribeId = subscribeId,
                WhitelistId = subscribe.WhitelistId,
                ExtraInfoId = new ExtraInfoId()
                {
                    AddressList = new Whitelist.AddressList(){Value = { User1Address }},
                    Id = tagId
                }
            });
            var consumedList = await WhitelistContractStub.GetConsumedList.CallAsync(subscribeId);
            consumedList.ExtraInfoIdList.Value.Count.ShouldBe(1);
            consumedList.WhitelistId.ShouldBe(subscribe.WhitelistId);
            consumedList.ExtraInfoIdList.Value[0].AddressList.Value[0].ShouldBe(User1Address);
            consumedList.ExtraInfoIdList.Value[0].Id.ShouldBe(CalculateId(subscribe.WhitelistId,_projectId,"INFO1"));
            var availableList = await WhitelistContractStub.GetAvailableWhitelist.CallAsync(subscribeId);
            availableList.Value.Count.ShouldBe(2);
            availableList.Value[0].AddressList.Value[0].ShouldBe(User2Address);

            return subscribeId;
        }
        
        [Fact]
        public async Task ConsumeWhitelistTest_RepeatConsumption()
        {
            var subscribeId = await ConsumeWhitelistTest();
            var subscribe = await WhitelistContractStub.GetSubscribeWhitelist.CallAsync(subscribeId);
            var executionResult = await WhitelistContractStub.ConsumeWhitelist.SendWithExceptionAsync(new ConsumeWhitelistInput()
            {
                SubscribeId = subscribeId,
                WhitelistId = subscribe.WhitelistId,
                ExtraInfoId = new ExtraInfoId()
                {
                    AddressList = new Whitelist.AddressList(){Value = { User1Address }},
                    Id = CalculateId(subscribe.WhitelistId,_projectId,"INFO3")
                }
            });
            executionResult.TransactionResult.Error.ShouldContain("ExtraInfo doesn't exist in the available whitelist.");
        }
        
        [Fact]
        public async Task ConsumeWhitelistTest_ConsumptionExist()
        {
            var subscribeId = await ConsumeWhitelistTest();
            var subscribe = await WhitelistContractStub.GetSubscribeWhitelist.CallAsync(subscribeId);
            await WhitelistContractStub.ConsumeWhitelist.SendAsync(new ConsumeWhitelistInput()
            {
                SubscribeId = subscribeId,
                WhitelistId = subscribe.WhitelistId,
                ExtraInfoId = new ExtraInfoId()
                {
                    AddressList = new Whitelist.AddressList(){Value = { User3Address }},
                    Id = CalculateId(subscribe.WhitelistId,_projectId,"INFO3")
                }
            });
            var consumedList = await WhitelistContractStub.GetConsumedList.CallAsync(subscribeId);
            consumedList.ExtraInfoIdList.Value.Count.ShouldBe(2);
            consumedList.WhitelistId.ShouldBe(subscribe.WhitelistId);
            consumedList.ExtraInfoIdList.Value[0].AddressList.Value.Count.ShouldBe(1);
            consumedList.ExtraInfoIdList.Value[0].AddressList.Value[0].ShouldBe(User1Address);
            consumedList.ExtraInfoIdList.Value[0].Id.ShouldBe(CalculateId(subscribe.WhitelistId,_projectId,"INFO1"));
            consumedList.ExtraInfoIdList.Value[1].AddressList.Value.Count.ShouldBe(1);
            consumedList.ExtraInfoIdList.Value[1].AddressList.Value[0].ShouldBe(User3Address);
            consumedList.ExtraInfoIdList.Value[1].Id.ShouldBe(CalculateId(subscribe.WhitelistId,_projectId,"INFO3"));
            var availableList = await WhitelistContractStub.GetAvailableWhitelist.CallAsync(subscribeId);
            availableList.Value.Count.ShouldBe(2);
            availableList.Value[1].AddressList.Value.Count.ShouldBe(1);
        }
        
        [Fact]
        public async Task ConsumeWhitelistTest_ConsumptionExist_SameTagId()
        {
            var subscribeId = await ConsumeWhitelistTest();
            var subscribe = await WhitelistContractStub.GetSubscribeWhitelist.CallAsync(subscribeId);
            await WhitelistContractStub.ConsumeWhitelist.SendAsync(new ConsumeWhitelistInput()
            {
                SubscribeId = subscribeId,
                WhitelistId = subscribe.WhitelistId,
                ExtraInfoId = new ExtraInfoId()
                {
                    AddressList = new Whitelist.AddressList(){Value = { User2Address }},
                    Id = CalculateId(subscribe.WhitelistId,_projectId,"INFO1")
                }
            });
            var consumedList = await WhitelistContractStub.GetConsumedList.CallAsync(subscribeId);
            consumedList.ExtraInfoIdList.Value.Count.ShouldBe(1);
            consumedList.WhitelistId.ShouldBe(subscribe.WhitelistId);
            consumedList.ExtraInfoIdList.Value[0].AddressList.Value.Count.ShouldBe(2);
            consumedList.ExtraInfoIdList.Value[0].AddressList.Value[1].ShouldBe(User2Address);
            consumedList.ExtraInfoIdList.Value[0].Id.ShouldBe(CalculateId(subscribe.WhitelistId,_projectId,"INFO1"));
            
            var availableList = await WhitelistContractStub.GetAvailableWhitelist.CallAsync(subscribeId);
            availableList.Value.Count.ShouldBe(2);
            availableList.Value[0].AddressList.Value.Count.ShouldBe(0);
            availableList.Value[1].AddressList.Value.Count.ShouldBe(2);
        }

        [Fact]
        public async Task<Hash> CloneWhitelistTest()
        {
            var whitelistId =await CreateWhitelistTest();
            var executionResult = await WhitelistContractStub.CloneWhitelist.SendAsync(new CloneWhitelistInput()
            {
                WhitelistId = whitelistId
            });
            var cloneWhitelistId = executionResult.Output;
            var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(cloneWhitelistId);
            whitelist.CloneFrom.ShouldBe(whitelistId);
            whitelist.ExtraInfoIdList.Value.Count.ShouldBe(2);
            return whitelist.WhitelistId;
        }
        
        [Fact]
        public async Task CloneWhitelistTest_NotAllowed()
        {
            var whitelistId =await CreateWhitelistTest();
            await WhitelistContractStub.ChangeWhitelistCloneable.SendAsync(new ChangeWhitelistCloneableInput()
            {
                WhitelistId = whitelistId,
                IsCloneable = false
            });
            {
                var executionResult = await WhitelistContractStub.CloneWhitelist.SendWithExceptionAsync(
                    new CloneWhitelistInput()
                    {
                        WhitelistId = whitelistId
                    });
                executionResult.TransactionResult.Error.ShouldContain("Whitelist is not allowed to be cloned.");
            }
        }
        
        [Fact]
        public async Task<Hash> AddSubscribeManagersTest()
        {
            var subscribeId = await SubscribeWhitelistTest();
            await WhitelistContractStub.AddSubscribeManagers.SendAsync(new AddSubscribeManagersInput()
            {
                SubscribeId = subscribeId,
                ManagerList = new Whitelist.AddressList()
                {
                    Value = { User5Address,User6Address }
                }
            });
            {
                var manager = await WhitelistContractStub.GetSubscribeManagerList.CallAsync(subscribeId);
                manager.Value.Count.ShouldBe(3);
                manager.Value[0].ShouldBe(DefaultAddress);
                manager.Value[1].ShouldBe(User5Address);
                manager.Value[2].ShouldBe(User6Address);
            }
            {
                var subscribeIdList = await WhitelistContractStub.GetSubscribeIdByManager.CallAsync(User5Address);
                subscribeIdList.Value.Count.ShouldBe(1);
                subscribeIdList.Value[0].ShouldBe(subscribeId);
            }
            return subscribeId;
        }
        
        [Fact]
        public async Task AddSubscribeManagersTest_AlreadyExists()
        {
            var subscribeId = await SubscribeWhitelistTest();
            var executionResult = await WhitelistContractStub.AddSubscribeManagers.SendWithExceptionAsync(new AddSubscribeManagersInput()
            {
                SubscribeId = subscribeId,
                ManagerList = new Whitelist.AddressList()
                {
                    Value = { DefaultAddress }
                }
            });
            executionResult.TransactionResult.Error.ShouldContain("Managers already exists.");
        }
        
        [Fact]
        public async Task RemoveSubscribeManagersTest()
        {
            var subscribeId = await AddSubscribeManagersTest();
            await WhitelistContractStub.RemoveSubscribeManagers.SendAsync(new RemoveSubscribeManagersInput()
            {
                SubscribeId = subscribeId,
                ManagerList = new Whitelist.AddressList()
                {
                    Value = { User5Address }
                }
            });
            {
                var manager = await WhitelistContractStub.GetSubscribeManagerList.CallAsync(subscribeId);
                manager.Value.Count.ShouldBe(2);
                manager.Value[1].ShouldBe(User6Address);
            }
            {
                var exception = await WhitelistContractStub.GetSubscribeIdByManager.CallWithExceptionAsync(User5Address);
                exception.Value.ShouldContain("No subscribe id according to the manager.");
            }
            {
                var exception = await WhitelistContractStub.GetSubscribeIdByManager.CallWithExceptionAsync(User3Address);
                exception.Value.ShouldContain("No subscribe id according to the manager.");
            }
        }
        
        [Fact]
        public async Task RemoveSubscribeManagersTest_NotExist()
        {
            var subscribeId = await AddSubscribeManagersTest();
            var exceptionAsync = await WhitelistContractStub.RemoveSubscribeManagers.SendWithExceptionAsync(new RemoveSubscribeManagersInput()
            {
                SubscribeId = subscribeId,
                ManagerList = new Whitelist.AddressList()
                {
                    Value = { User3Address }
                }
            });
            exceptionAsync.TransactionResult.Error.ShouldContain("Managers doesn't exists.");
            {
                var manager = await WhitelistContractStub.GetSubscribeManagerList.CallAsync(subscribeId);
                manager.Value.Count.ShouldBe(3);
                manager.Value[2].ShouldBe(User6Address);
            }
            {
                var exception = await WhitelistContractStub.GetSubscribeIdByManager.CallWithExceptionAsync(User3Address);
                exception.Value.ShouldContain("No subscribe id according to the manager.");
            }
        }
    }
}