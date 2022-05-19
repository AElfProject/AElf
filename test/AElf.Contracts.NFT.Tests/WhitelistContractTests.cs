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

        private static readonly ByteString Info6 = new Price(){
            Symbol = "ELF",
            Amount = 10_0000
        }.ToByteString();
        private readonly Hash _info6Id = HashHelper.ComputeFrom(Info6.ToByteArray());

        [Fact]
        public async Task InitializeTest()
        {
            await WhitelistContractStub.Initialize.SendAsync(new InitializeInput());
        }

        [Fact]
        public async Task<Hash> CreateWhitelistTest()
        {
            await InitializeTest();
            var whitelistId = (await WhitelistContractStub.CreateWhitelist.SendAsync(new CreateWhitelistInput()
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
                Remark = "new whitelist test",
                ManagerList = new Whitelist.AddressList()
                {
                    Value = { User4Address }
                }
            })).Output;
            var whitelistId2 = (await WhitelistContractStub.CreateWhitelist.SendAsync(new CreateWhitelistInput()
            {
                ExtraInfoList = new ExtraInfoList()
                {
                    Value =
                    {
                        new ExtraInfo
                        {
                            Address = User1Address,
                            Info = Info5
                        },
                        new ExtraInfo
                        {
                            Address = User2Address,
                            Info = Info1
                        }
                    }
                },
                IsCloneable = true,
                Remark = "second whitelist test",
                ManagerList = new Whitelist.AddressList()
                {
                    Value = { User5Address }
                }
            })).Output;
            
            {
                var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
                whitelist.WhitelistId.ShouldBe(whitelistId);
                whitelist.ExtraInfoIdList.Value[1].Address.ShouldBe(User1Address);
                whitelist.ExtraInfoIdList.Value[1].Id.ShouldBe(_info3Id);
            }
            {
                var whitelist2 = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId2);
                whitelist2.ExtraInfoIdList.Value[1].Address.ShouldBe(User2Address);
                whitelist2.ExtraInfoIdList.Value[1].Id.ShouldBe(_info1Id);
            }
            {
                var whitelistDetail = await WhitelistContractStub.GetWhitelistDetail.CallAsync(whitelistId);
                whitelistDetail.Value[1].Address.ShouldBe(User1Address);
                whitelistDetail.Value[1].Info.ShouldBe(Info3);
            }
            {
                var whitelistIdList1 = await WhitelistContractStub.GetWhitelistByManager.CallAsync(DefaultAddress);
                whitelistIdList1.WhitelistId.Count.ShouldBe(2);
                whitelistIdList1.WhitelistId[0].ShouldBe(whitelistId);
                whitelistIdList1.WhitelistId[1].ShouldBe(whitelistId2);
            }
            {
                var whitelistIdList2 = await WhitelistContractStub.GetWhitelistByManager.CallAsync(User5Address);
                whitelistIdList2.WhitelistId.Count.ShouldBe(1);
                whitelistIdList2.WhitelistId[0].ShouldBe(whitelistId2);
            }
            {
                var manager = await WhitelistContractStub.GetManagerList.CallAsync(whitelistId);
                manager.Value[0].ShouldBe(User4Address);
            }

            return whitelistId;
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
            {
                var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
                whitelist.ExtraInfoIdList.Value.Count.ShouldBe(5);
                whitelist.ExtraInfoIdList.Value[3].Address.ShouldBe(User2Address);
                whitelist.ExtraInfoIdList.Value[3].Id.ShouldBe(_info4Id);
                whitelist.ExtraInfoIdList.Value[4].Address.ShouldBe(User3Address);
                whitelist.ExtraInfoIdList.Value[4].Id.ShouldBe(_info1Id);
            }
            {
                var extraInfo = await WhitelistContractStub.GetExtraInfoByHash.CallAsync(_info2Id);
                var deserializedExtraInfo = new Price();
                deserializedExtraInfo.MergeFrom(extraInfo.Value);
                deserializedExtraInfo.Symbol.ShouldBe("ETH");
                deserializedExtraInfo.Amount.ShouldBe(100_0000000);
                extraInfo.Value.ShouldBe(Info2);
            }

            {
                var extra = await WhitelistContractStub.GetExtraInfoByAddress.CallAsync(new GetExtraInfoByAddressInput()
                {
                    WhitelistId = whitelistId,
                    Address = User2Address
                });
                extra.Value.Count.ShouldBe(2);
                extra.Value[0].Info.ShouldBe(Info2);
                extra.Value[1].Info.ShouldBe(Info4);
            }
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
            executionResult.TransactionResult.Error.ShouldContain("These extraInfo already exists in the whitelist.");
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
            await WhitelistContractStub.ChangeWhitelistCloneable.SendAsync(new ChangeWhitelistCloneableInput()
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
                    ExtraInfoId = _info6Id,
                    ExtraInfo = Info6
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
            whitelist.Manager.Value.ShouldContain(User1Address);
            whitelist.Manager.Value.ShouldNotContain(DefaultAddress);
        }

        [Fact]
        public async Task<Hash> AddManagersTest()
        {
            var whitelistId = await CreateWhitelistTest();
            await WhitelistContractStub.AddManagers.SendAsync(new AddManagersInput()
            {
                WhitelistId = whitelistId,
                ManagerList = new Whitelist.AddressList()
                {
                    Value = { User5Address }
                }
            });
            {
                var manager = await WhitelistContractStub.GetManagerList.CallAsync(whitelistId);
                manager.Value.Count.ShouldBe(3);
                manager.Value[0].ShouldBe(User4Address);
                manager.Value[2].ShouldBe(User5Address);
            }
            {
                var whitelistIdList = await WhitelistContractStub.GetWhitelistByManager.CallAsync(User5Address);
                whitelistIdList.WhitelistId.Count.ShouldBe(2);
                whitelistIdList.WhitelistId[1].ShouldBe(whitelistId);
            }
            return whitelistId;
        }
        
        [Fact]
        public async Task AddManagersTest_AlreadyExists()
        {
            var whitelistId = await CreateWhitelistTest();
            var executionResult = await WhitelistContractStub.AddManagers.SendWithExceptionAsync(new AddManagersInput()
            {
                WhitelistId = whitelistId,
                ManagerList = new Whitelist.AddressList()
                {
                    Value = { User4Address }
                }
            });
            executionResult.TransactionResult.Error.ShouldContain("Managers already exists.");
        }

        [Fact]
        public async Task RemoveManagersTest()
        {
            var whitelistId = await AddManagersTest();
            await WhitelistContractStub.RemoveManagers.SendAsync(new RemoveManagersInput()
            {
                WhitelistId = whitelistId,
                ManagerList = new Whitelist.AddressList()
                {
                    Value = { User4Address }
                }
            });
            {
                var manager = await WhitelistContractStub.GetManagerList.CallAsync(whitelistId);
                manager.Value.Count.ShouldBe(2);
                manager.Value[0].ShouldBe(DefaultAddress);
            }
            {
                var whitelistIdList = await WhitelistContractStub.GetWhitelistByManager.CallAsync(User4Address);
                whitelistIdList.WhitelistId.Count.ShouldBe(0);
            }
        }
    }
}