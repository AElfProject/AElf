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
                    Address = User1Address,
                    Id = tagId
                }
            });
            var consumedList = await WhitelistContractStub.GetConsumedList.CallAsync(subscribeId);
            consumedList.ExtraInfoIdList.Value.Count.ShouldBe(1);
            consumedList.WhitelistId.ShouldBe(subscribe.WhitelistId);
            consumedList.ExtraInfoIdList.Value[0].Address.ShouldBe(User1Address);
            consumedList.ExtraInfoIdList.Value[0].Id.ShouldBe(CalculateId(DefaultAddress,_projectId,"INFO1"));
            var availableList = await WhitelistContractStub.GetAvailableWhitelist.CallAsync(subscribeId);
            availableList.Value.Count.ShouldBe(1);
            availableList.Value[0].Address.ShouldBe(User3Address);
            availableList.Value[0].Info.Info.ShouldBe(Info3);

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
                    Address = User1Address,
                    Id = CalculateId(DefaultAddress,_projectId,"INFO1")
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
                    Address = User3Address,
                    Id = CalculateId(DefaultAddress,_projectId,"INFO3")
                }
            });
            var consumedList = await WhitelistContractStub.GetConsumedList.CallAsync(subscribeId);
            consumedList.ExtraInfoIdList.Value.Count.ShouldBe(2);
            consumedList.WhitelistId.ShouldBe(subscribe.WhitelistId);
            consumedList.ExtraInfoIdList.Value[0].Address.ShouldBe(User1Address);
            consumedList.ExtraInfoIdList.Value[0].Id.ShouldBe(CalculateId(DefaultAddress,_projectId,"INFO1"));
            consumedList.ExtraInfoIdList.Value[1].Address.ShouldBe(User3Address);
            consumedList.ExtraInfoIdList.Value[1].Id.ShouldBe(CalculateId(DefaultAddress,_projectId,"INFO3"));
            var availableList = await WhitelistContractStub.GetAvailableWhitelist.CallAsync(subscribeId);
            availableList.Value.Count.ShouldBe(0);
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
    }
}