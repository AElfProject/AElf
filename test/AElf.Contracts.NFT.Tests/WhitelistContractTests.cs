using System.Threading.Tasks;
using AElf.Contracts.NFTMarket;
using AElf.Contracts.Whitelist;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;
using InitializeInput = AElf.Contracts.Whitelist.InitializeInput;

namespace AElf.Contracts.NFT
{
    public partial class WhitelistContractTests : NFTContractTestBase
    {
        private static readonly ByteString Info1 = new Price(){
            Symbol = "ELF",
            Amount = 200_000000
        }.ToByteString();
        private readonly Hash _info1Id = HashHelper.ComputeFrom(Info1.ToByteArray());
        
        private static readonly ByteString Info2 = new Price(){
            Symbol = "ETH",
            Amount = 100_0000000
        }.ToByteString();
        private readonly Hash _info2Id = HashHelper.ComputeFrom(Info2.ToByteArray());

        private static readonly ByteString Info3 = new Price(){
            Symbol = "ELF",
            Amount = 500_000000
        }.ToByteString();
        private readonly Hash _info3Id = HashHelper.ComputeFrom(Info3.ToByteArray());

        private static readonly ByteString Info4 = new Price(){
            Symbol = "ELF",
            Amount = 900_000000
        }.ToByteString();
        private readonly Hash _info4Id = HashHelper.ComputeFrom(Info4.ToByteArray());
        
        private static readonly ByteString Info5 = new Price(){
            Symbol = "BTC",
            Amount = 2200_000000
        }.ToByteString();
        private readonly Hash _info5Id = HashHelper.ComputeFrom(Info5.ToByteArray());

        [Fact]
        public async Task InitializeTest()
        {
            await WhitelistContractStub.Initialize.SendAsync(new InitializeInput());
        }

        [Fact]
        public async Task<Hash> CreateWhitelistTest()
        {
            await InitializeTest();
            var executionResult = await WhitelistContractStub.CreateWhitelist.SendAsync(new CreateWhitelistInput()
            {
                ExtraInfoList = new ExtraInfoList()
                {
                    Value =
                    {
                        new ExtraInfo
                        {
                            Address = User1Address,
                            Info = Info1
                        },
                        new ExtraInfo
                        {
                            Address = User1Address,
                            Info = Info3
                        },
                        new ExtraInfo()
                        {
                            Address = User2Address,
                            Info = Info2
                        }
                    }
                },
                IsCloneable = true,
                Remark = "new whitelist test"
            });
            var whitelistId = executionResult.Output;
            var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
            var whitelistDetail = await WhitelistContractStub.GetWhitelistDetail.CallAsync(whitelistId);
            whitelist.WhitelistId.ShouldBe(whitelistId);
            whitelist.ExtraInfoIdList.Value[1].Address.ShouldBe(User1Address);
            whitelistDetail.Value[1].Info.ShouldBe(Info3);
            whitelist.ExtraInfoIdList.Value[1].Id.ShouldBe(_info3Id);
            
            return whitelist.WhitelistId;
        }

        [Fact]
        public async Task AddAddressInfoToWhitelistTest_whitelistNotFound()
        {
            var whitelistId = await CreateWhitelistTest();
            var executionResult = await WhitelistContractStub.AddAddressInfoToWhitelist.SendWithExceptionAsync(
                new AddAddressInfoToWhitelistInput()
                {
                    WhitelistId = new Hash(),
                    ExtraInfo = new ExtraInfo()
                    {
                        Address = User3Address,
                        Info = Info1
                    }
                });
            executionResult.TransactionResult.Error.ShouldContain("Whitelist not found.");
        }

        [Fact]
        public async Task AddAddressInfoToWhitelistTest_ExtraInfoExist()
        {
            var whitelistId = await CreateWhitelistTest();
            var executionResult = await WhitelistContractStub.AddAddressInfoToWhitelist.SendWithExceptionAsync(
                new AddAddressInfoToWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfo = new ExtraInfo()
                    {
                        Address = User1Address,
                        Info = Info1
                    }
                });
            executionResult.TransactionResult.Error.ShouldContain("ExtraInfo already exists.");
        }

        [Fact]
        public async Task AddAddressInfoToWhitelistTest()
        {
            var whitelistId = await CreateWhitelistTest();
            await WhitelistContractStub.AddAddressInfoToWhitelist.SendAsync(
                new AddAddressInfoToWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfo = new ExtraInfo()
                    {
                        Address = User3Address,
                        Info = Info1
                    }
                });
            var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
            var whitelistExtra = await WhitelistContractStub.GetWhitelistDetail.CallAsync(whitelistId);
            var extraInfoList = await WhitelistContractStub.GetExtraInfoByHash.CallAsync(_info1Id);
            whitelist.ExtraInfoIdList.Value[3].Address.ShouldBe(User3Address);
            whitelistExtra.Value[3].Info.ShouldBe(Info1);
            extraInfoList.Value.ShouldBe(Info1);
        }

        [Fact]
        public async Task RemoveAddressInfoFromWhitelistTest_NotMatchAddress()
        {
            var whitelistId = await CreateWhitelistTest();
            var executionResult1 = await WhitelistContractStub.RemoveAddressInfoFromWhitelist.SendWithExceptionAsync(
                new RemoveAddressInfoFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfo = new ExtraInfo()
                    {
                        Address = new Address()
                    }
                });
            executionResult1.TransactionResult.Error.ShouldContain("Address doesn't exist.");
            
        }

        [Fact]
        public async Task RemoveAddressInfoFromWhitelistTest_Address()
        {
            var whitelistId = await CreateWhitelistTest();
            await WhitelistContractStub.RemoveAddressInfoFromWhitelist.SendAsync(
                new RemoveAddressInfoFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfo = new ExtraInfo()
                    {
                        Address = User1Address
                    }
                });
            var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
            whitelist.ExtraInfoIdList.Value.Count.ShouldBe(1);
            whitelist.ExtraInfoIdList.Value[0].Address.ShouldBe(User2Address);
            whitelist.ExtraInfoIdList.Value[0].Id.ShouldBe(_info2Id);
        }
        
        [Fact]
        public async Task RemoveAddressInfoFromWhitelistTest_NotMatchAddressExtra()
        {
            var whitelistId = await CreateWhitelistTest();
            var executionResult = await WhitelistContractStub.RemoveAddressInfoFromWhitelist.SendWithExceptionAsync(
                new RemoveAddressInfoFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfo = new ExtraInfo()
                    {
                        Address = User1Address,
                        Info = Info2
                    }
                });
            executionResult.TransactionResult.Error.ShouldContain("Address and extra info doesn't exist.");
            await WhitelistContractStub.RemoveAddressInfoFromWhitelist.SendAsync(
                new RemoveAddressInfoFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfo = new ExtraInfo()
                    {
                        Address = User1Address,
                        Info = Info1
                    }
                });
            var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
            whitelist.ExtraInfoIdList.Value.Count.ShouldBe(2);
            whitelist.ExtraInfoIdList.Value[0].Address.ShouldBe(User1Address);
            whitelist.ExtraInfoIdList.Value[0].Id.ShouldBe(_info3Id);

        }
        
        [Fact]
        public async Task RemoveAddressInfoFromWhitelistTest_AddressExtra()
        {
            var whitelistId = await CreateWhitelistTest();
            await WhitelistContractStub.RemoveAddressInfoFromWhitelist.SendAsync(
                new RemoveAddressInfoFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfo = new ExtraInfo()
                    {
                        Address = User1Address,
                        Info = Info1
                    }
                });
            var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
            whitelist.ExtraInfoIdList.Value.Count.ShouldBe(2);
            whitelist.ExtraInfoIdList.Value[0].Address.ShouldBe(User1Address);
            whitelist.ExtraInfoIdList.Value[0].Id.ShouldBe(_info3Id);
        }
        
        [Fact]
        public async Task<Hash> AddAddressInfoListToWhitelistTest()
        {
            var whitelistId = await CreateWhitelistTest();
            var executionResult = await WhitelistContractStub.AddAddressInfoListToWhitelist.SendAsync(
                new AddAddressInfoListToWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoList = new ExtraInfoList()
                    {
                        Value = { 
                            // new ExtraInfo()
                            // {
                            //     Address  = User1Address,
                            //     Info = Info3
                            // },
                            new ExtraInfo() 
                            {
                                Address = User2Address,
                                Info = Info4
                            },
                            new ExtraInfo()
                            {
                                Address = User3Address,
                                Info = Info1
                            }
                        }
                    }
                });
            //executionResult.TransactionResult.Error.ShouldContain("These extraInfo already exists.");
            var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
            var extraInfo = await WhitelistContractStub.GetExtraInfoByHash.CallAsync(_info2Id);
            whitelist.ExtraInfoIdList.Value.Count.ShouldBe(5);
            whitelist.ExtraInfoIdList.Value[3].Address.ShouldBe(User2Address);
            whitelist.ExtraInfoIdList.Value[3].Id.ShouldBe(_info4Id);
            whitelist.ExtraInfoIdList.Value[4].Address.ShouldBe(User3Address);
            whitelist.ExtraInfoIdList.Value[4].Id.ShouldBe(_info1Id);
            extraInfo.Value.ShouldBe(Info2);
            return whitelistId;
        }
        
        [Fact]
        public async Task AddAddressInfoListToWhitelistTest_ExtraInfoExist()
        {
            var whitelistId = await CreateWhitelistTest();
            var executionResult = await WhitelistContractStub.AddAddressInfoListToWhitelist.SendWithExceptionAsync(
                new AddAddressInfoListToWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoList = new ExtraInfoList()
                    {
                        Value = { 
                            new ExtraInfo()
                            {
                                Address  = User1Address,
                                Info = Info3
                            },
                            new ExtraInfo() 
                            {
                                Address = User2Address,
                                Info = Info5
                            }
                        }
                    }
                });
            executionResult.TransactionResult.Error.ShouldContain("These extraInfo already exists.");
        }

        [Fact]
        public async Task RemoveAddressInfoListFromWhitelistTest_Address()
        {
            var whitelistId = await AddAddressInfoListToWhitelistTest();
            var executionResult = await WhitelistContractStub.RemoveAddressInfoListFromWhitelist.SendAsync(
                new RemoveAddressInfoListFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoList = new ExtraInfoList()
                    {
                        Value =
                        {
                            new ExtraInfo()
                            {
                                Address = User1Address
                            },
                            new ExtraInfo()
                            {
                                Address = User2Address
                            }
                        }
                    }
                });
            var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
            whitelist.ExtraInfoIdList.Value.Count.ShouldBe(1);
            whitelist.ExtraInfoIdList.Value[0].Address.ShouldBe(User3Address);
            whitelist.ExtraInfoIdList.Value[0].Id.ShouldBe(_info1Id);
        }
        
        [Fact]
        public async Task RemoveAddressInfoListFromWhitelistTest_NoMatchAddress()
        {
            var whitelistId = await AddAddressInfoListToWhitelistTest();
            var executionResult = await WhitelistContractStub.RemoveAddressInfoListFromWhitelist.SendWithExceptionAsync(
                new RemoveAddressInfoListFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoList = new ExtraInfoList()
                    {
                        Value =
                        {
                            new ExtraInfo()
                            {
                                Address = new Address()
                            },
                            new ExtraInfo()
                            {
                                Address = new Address()
                            }
                        }
                    }
                });
            executionResult.TransactionResult.Error.ShouldContain("Address doesn't exist.");
        }

        [Fact]
        public async Task RemoveAddressInfoListFromWhitelistTest_AddressExtra()
        {
            var whitelistId = await AddAddressInfoListToWhitelistTest();
            await WhitelistContractStub.RemoveAddressInfoListFromWhitelist.SendAsync(
                new RemoveAddressInfoListFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoList = new ExtraInfoList()
                    {
                        Value =
                        {
                            new ExtraInfo()
                            {
                                Address = User1Address,
                                Info = Info3
                            },
                            new ExtraInfo()
                            {
                                Address = User2Address,
                                Info = Info2
                            }
                        }
                    }
                });
            var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
            whitelist.ExtraInfoIdList.Value.Count.ShouldBe(3);
            whitelist.ExtraInfoIdList.Value[1].Address.ShouldBe(User2Address);
            whitelist.ExtraInfoIdList.Value[1].Id.ShouldBe(_info4Id);
        }
        
        [Fact]
        public async Task RemoveAddressInfoListFromWhitelistTest_NoMatchAddressExtra()
        {
            var whitelistId = await AddAddressInfoListToWhitelistTest();
            var executionResult = await WhitelistContractStub.RemoveAddressInfoListFromWhitelist.SendWithExceptionAsync(
                new RemoveAddressInfoListFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoList = new ExtraInfoList()
                    {
                        Value =
                        {
                            new ExtraInfo()
                            {
                                Address = User2Address,
                                Info = Info1
                            }
                        }
                    }
                });
            executionResult.TransactionResult.Error.ShouldContain("Address and extra info doesn't exist.");
        }

        [Fact]
        public async Task DisableWhitelistTest()
        {
            var whitelistId = await CreateWhitelistTest();
            var executionResult = await WhitelistContractStub.DisableWhitelist.SendWithExceptionAsync(
                new DisableWhitelistInput()
                {
                    WhitelistId = new Hash(),
                    Remark = "Disable this whitelist."
                });
            executionResult.TransactionResult.Error.ShouldContain("Whitelist not found.");
            await WhitelistContractStub.DisableWhitelist.SendAsync(
                new DisableWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    Remark = "Disable this whitelist."
                });
            var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
            whitelist.IsAvailable.ShouldBe(false);
        }

        [Fact]
        public async Task ChangeWhitelistCloneableTest()
        {
            var whitelistId = await CreateWhitelistTest();
            await WhitelistContractStub.ChangeWhitelistCloneable.SendAsync(new UpdateWhitelistCloneableInput()
            {
                WhitelistId = whitelistId,
                IsCloneable = false
            });
            var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
            whitelist.IsCloneable.ShouldBe(false);
        }

        [Fact]
        public async Task AddExtraInfoTest()
        {
            var whitelistId = await CreateWhitelistTest();
            var executionResult = await WhitelistContractStub.AddExtraInfo.SendWithExceptionAsync(
                new AddExtraInfoInput()
                {
                    ExtraInfoId = _info1Id,
                    ExtraInfo = Info1
                });
            executionResult.TransactionResult.Error.ShouldContain("Extra Info is exist.");
            await WhitelistContractStub.AddExtraInfo.SendAsync(
                new AddExtraInfoInput()
                {
                    ExtraInfoId = _info5Id,
                    ExtraInfo = Info5
                });
            var extraInfo = await WhitelistContractStub.GetExtraInfoByHash.CallAsync(_info5Id);
            extraInfo.Value.ShouldBe(Info5);
        }

        [Fact]
        public async Task UpdateExtraInfoTest()
        {
            var whitelistId = await AddAddressInfoListToWhitelistTest();
            await WhitelistContractStub.UpdateExtraInfo.SendAsync(
                new UpdateExtraInfoInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoList = new ExtraInfoUpdateList()
                    {
                        Value =
                        {
                            new ExtraInfoUpdate()
                            {
                                Address = User1Address,
                                InfoBefore = Info3,
                                InfoUpdate = Info5
                            },
                            new ExtraInfoUpdate()
                            {
                                Address = User1Address,
                                InfoBefore = Info1,
                                InfoUpdate = Info4
                            }
                        }
                    }
                });
            var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
            whitelist.ExtraInfoIdList.Value[0].Address.ShouldBe(User1Address);
            whitelist.ExtraInfoIdList.Value[0].Id.ShouldBe(_info4Id);
            whitelist.ExtraInfoIdList.Value[1].Address.ShouldBe(User1Address);
            whitelist.ExtraInfoIdList.Value[1].Id.ShouldBe(_info5Id);
        }
        
        [Fact]
        public async Task UpdateExtraInfoTest_NoMatch()
        {
            var whitelistId = await AddAddressInfoListToWhitelistTest();
            var executionResult = await WhitelistContractStub.UpdateExtraInfo.SendWithExceptionAsync(
                new UpdateExtraInfoInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoList = new ExtraInfoUpdateList()
                    {
                        Value =
                        {
                            new ExtraInfoUpdate()
                            {
                                Address = User1Address,
                                InfoBefore = Info4,
                                InfoUpdate = Info5
                            },
                            new ExtraInfoUpdate()
                            {
                                Address = User1Address,
                                InfoBefore = Info1,
                                InfoUpdate = Info4
                            }
                        }
                    }
                });
            executionResult.TransactionResult.Error.ShouldContain("No match address.");
        }


        [Fact]
        public async Task TransferManager()
        {
            var whitelistId = await CreateWhitelistTest();
            await WhitelistContractStub.TransferManager.SendAsync(
                new TransferManagerInput()
                {
                    WhitelistId = whitelistId,
                    Manager = User1Address
                });
            var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
            whitelist.Manager.ShouldBe(User1Address);
        }
    }
}