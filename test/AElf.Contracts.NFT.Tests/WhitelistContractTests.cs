using System.Threading.Tasks;
using AElf.Contracts.NFTMarket;
using AElf.Contracts.Whitelist;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using PriceTag = AElf.Contracts.Whitelist.PriceTag;

//using InitializeInput = AElf.Contracts.Whitelist.InitializeInput;

namespace AElf.Contracts.NFT
{
    public partial class WhitelistContractTests : NFTContractTestBase
    {
        private static readonly ByteString Info1 = new PriceTag(){
            Symbol = "ELF",
            Amount = 200_000000
        }.ToByteString();

        
        private static readonly ByteString Info2 = new PriceTag(){
            Symbol = "ETH",
            Amount = 100_0000000
        }.ToByteString();

        private static readonly ByteString Info3 = new PriceTag(){
            Symbol = "ELF",
            Amount = 500_000000
        }.ToByteString();

        private static readonly ByteString Info4 = new PriceTag(){
            Symbol = "ELF",
            Amount = 900_000000
        }.ToByteString();
        
        private static readonly ByteString Info5 = new PriceTag(){
            Symbol = "BTC",
            Amount = 2200_000000
        }.ToByteString();
        

        private Hash CalculateId(Address sender, Hash projectId, string tagName)
        {
            return HashHelper.ComputeFrom($"{sender}{projectId}{tagName}");
        }
        
        private readonly Hash _projectId = HashHelper.ComputeFrom("NFT Forest");
        private readonly Hash _projectId2 = HashHelper.ComputeFrom("IDO");
        
        [Fact]
        public async Task InitializeTest()
        {
            await WhitelistContractStub.Initialize.SendAsync(new Empty());
        }

        [Fact]
        public async Task<Hash> CreateWhitelistTest()
        {
            await InitializeTest();
            var whitelistId = (await WhitelistContractStub.CreateWhitelist.SendAsync(new CreateWhitelistInput()
            {
                ProjectId = _projectId,
                ExtraInfoList = new ExtraInfoList()
                {
                    Value =
                    {
                        new ExtraInfo
                        {
                            Address = User1Address,
                            Info = new TagInfo()
                            {
                                TagName = "INFO1",
                                Info = Info1
                            }
                        },
                        new ExtraInfo
                        {
                            Address = User3Address,
                            Info = new TagInfo()
                            {
                                TagName = "INFO3",
                                Info = Info3
                            }
                        }
                    }
                },
                IsCloneable = true,
                Remark = "new whitelist test",
                ManagerList = new Whitelist.AddressList()
                {
                    Value = { User4Address }
                },
                StrategyType = StrategyType.Price
            })).Output;
            var whitelistId2 = (await WhitelistContractStub.CreateWhitelist.SendAsync(new CreateWhitelistInput()
            {
                ProjectId = _projectId2,
                ExtraInfoList = new ExtraInfoList()
                {
                    Value =
                    {
                        new ExtraInfo
                        {
                            Address = User1Address,
                            Info = new TagInfo()
                            {
                                TagName = "INFO5",
                                Info = Info5
                            }
                        },
                        new ExtraInfo
                        {
                            Address = User2Address,
                            Info = new TagInfo()
                            {
                                TagName = "INFO1",
                                Info = Info1
                            }
                        }
                    }
                },
                IsCloneable = true,
                Remark = "second whitelist test",
                ManagerList = new Whitelist.AddressList()
                {
                    Value = { User5Address,User5Address }
                },
                StrategyType = StrategyType.Price
            })).Output;
            
            {
                var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
                whitelist.WhitelistId.ShouldBe(whitelistId);
                whitelist.ExtraInfoIdList.Value[1].Address.ShouldBe(User3Address);
                whitelist.ExtraInfoIdList.Value[1].Id.ShouldBe(CalculateId(DefaultAddress,_projectId,"INFO3"));
            }
            {
                var tagInfo = await WhitelistContractStub.GetTagInfoByHash.CallAsync(CalculateId(DefaultAddress,_projectId,"INFO1"));
                tagInfo.TagName.ShouldBe("INFO1");
                tagInfo.Info.ShouldBe(Info1);
            }
            {
                var extraInfo = await WhitelistContractStub.GetExtraInfoByTag.CallAsync(new GetExtraInfoByTagInput()
                {
                    WhitelistId = whitelistId,
                    TagInfoId = CalculateId(DefaultAddress,_projectId,"INFO1")
                });
                extraInfo.Value.Count.ShouldBe(1);
                extraInfo.Value[0].Address.ShouldBe(User1Address);
                extraInfo.Value[0].Info.Info.ShouldBe(Info1);
            }
            {
                var extraInfoIdList = await WhitelistContractStub.GetExtraInfoIdList.CallAsync(new GetExtraInfoIdListInput()
                {
                    Owner = DefaultAddress,
                    ProjectId = _projectId,
                    WhitelistId = whitelistId
                });
                extraInfoIdList.Value.Count.ShouldBe(2);
                extraInfoIdList.Value[0].ShouldBe(CalculateId(DefaultAddress,_projectId,"INFO1"));
                extraInfoIdList.Value[1].ShouldBe(CalculateId(DefaultAddress,_projectId,"INFO3"));
            }
            {
                var tagInfo = await WhitelistContractStub.GetExtraInfoByAddress.CallAsync(new GetExtraInfoByAddressInput()
                {
                    Address = User1Address,
                    WhitelistId = whitelistId
                });
                tagInfo.TagName.ShouldBe("INFO1");
                tagInfo.Info.ShouldBe(Info1);
            }
            {
                var whitelist2 = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId2);
                whitelist2.ExtraInfoIdList.Value[1].Address.ShouldBe(User2Address);
                whitelist2.ExtraInfoIdList.Value[1].Id.ShouldBe(CalculateId(DefaultAddress,_projectId2,"INFO1"));
            }
            {
                var whitelistDetail = await WhitelistContractStub.GetWhitelistDetail.CallAsync(whitelistId);
                whitelistDetail.Value[1].Address.ShouldBe(User3Address);
                whitelistDetail.Value[1].Info.TagName.ShouldBe("INFO3");
                whitelistDetail.Value[1].Info.Info.ShouldBe(Info3);
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
                var manager = await WhitelistContractStub.GetManagerList.CallAsync(whitelistId2);
                manager.Value.Count.ShouldBe(2);
                manager.Value[0].ShouldBe(User5Address);
            }

            return whitelistId;
        }
        
        [Fact]
        public async Task<Hash> CreateWhitelist_Address()
        {
            await InitializeTest();
            var projectId = new Hash();
            var whitelistId = (await WhitelistContractStub.CreateWhitelist.SendAsync(new CreateWhitelistInput()
            {
                ProjectId = projectId,
                ExtraInfoList = new ExtraInfoList()
                {
                    Value =
                    {
                        new ExtraInfo
                        {
                            Address = User1Address
                        },
                        new ExtraInfo
                        {
                            Address = User3Address
                        }
                    }
                },
                IsCloneable = true,
                Remark = "new whitelist test",
                ManagerList = new Whitelist.AddressList()
                {
                    Value = { User4Address }
                },
                StrategyType = StrategyType.Basic
            })).Output;
            {
                var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
                whitelist.WhitelistId.ShouldBe(whitelistId);
                whitelist.ExtraInfoIdList.Value[1].Address.ShouldBe(User3Address);
                whitelist.ExtraInfoIdList.Value[1].Id.ShouldBeNull();
            }
            return whitelistId;
        }
        
        [Fact]
        public async Task CreateWhitelist_Duplicate()
        {
            await InitializeTest();
            var projectId = new Hash();
            var executionResult = await WhitelistContractStub.CreateWhitelist.SendWithExceptionAsync(new CreateWhitelistInput()
            {
                ProjectId = projectId,
                ExtraInfoList = new ExtraInfoList()
                {
                    Value =
                    {
                        new ExtraInfo
                        {
                            Address = User1Address,
                            Info = new TagInfo()
                            {
                                TagName = "INFO1",
                                Info = Info1
                            }
                        },
                        new ExtraInfo
                        {
                            Address = User1Address,
                            Info = new TagInfo()
                            {
                                TagName = "INFO3",
                                Info = Info3
                            }
                        },
                        new ExtraInfo()
                        {
                            Address = User2Address,
                            Info = new TagInfo()
                            {
                                TagName = "INFO2",
                                Info = Info2
                            }
                        }
                    }
                },
                IsCloneable = true,
                Remark = "new whitelist test",
                ManagerList = new Whitelist.AddressList()
                {
                    Value = {User4Address}
                },
                StrategyType = StrategyType.Price
            });
            executionResult.TransactionResult.Error.ShouldContain("Duplicate address list.");
        }

        [Fact]
        public async Task AddExtraInfoTest()
        {
            var whitelistId = await CreateWhitelistTest();
            var tagInfoId = (await WhitelistContractStub.AddExtraInfo.SendAsync(new AddExtraInfoInput()
            {
                ProjectId = _projectId,
                WhitelistId = whitelistId,
                TagInfo = new TagInfo()
                {
                    TagName = "INFO2",
                    Info = Info2
                }
            })).Output;
            {
                var tag = await WhitelistContractStub.GetTagInfoByHash.CallAsync(tagInfoId);
                tag.TagName.ShouldBe("INFO2");
                var deserializedExtraInfo = new PriceTag();
                deserializedExtraInfo.MergeFrom(tag.Info);
                deserializedExtraInfo.Symbol.ShouldBe("ETH");
                deserializedExtraInfo.Amount.ShouldBe(100_0000000);
                tag.Info.ShouldBe(Info2);
            }
            {
                var tagIdList = await WhitelistContractStub.GetExtraInfoIdList.CallAsync(new GetExtraInfoIdListInput()
                {
                    WhitelistId = whitelistId,
                    Owner = DefaultAddress,
                    ProjectId = _projectId
                });
                tagIdList.Value.Count.ShouldBe(3);
                tagIdList.Value[0].ShouldBe(CalculateId(DefaultAddress,_projectId,"INFO1"));
                tagIdList.Value[2].ShouldBe(CalculateId(DefaultAddress,_projectId,"INFO2"));
            }
            {
                var exception = await WhitelistContractStub.GetExtraInfoByTag.CallWithExceptionAsync(new GetExtraInfoByTagInput()
                {
                    WhitelistId = whitelistId,
                    TagInfoId = CalculateId(DefaultAddress, _projectId, "INFO2")
                });
                exception.Value.ShouldContain("No address list under the current tag.");
            }
        }

        [Fact]
        public async Task<Hash> AddExtraInfoTest_WithAddress()
        {
            var whitelistId = await CreateWhitelistTest();
            var tagInfoId = (await WhitelistContractStub.AddExtraInfo.SendAsync(new AddExtraInfoInput()
            {
                WhitelistId = whitelistId,
                ProjectId = _projectId,
                TagInfo = new TagInfo()
                {
                    TagName = "INFO2",
                    Info = Info2
                },
                AddressList = new Whitelist.AddressList()
                {
                    Value = {User2Address, User4Address}
                }
            })).Output;
            {
                var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
                whitelist.ExtraInfoIdList.Value.Count.ShouldBe(4);
                whitelist.ExtraInfoIdList.Value[2].Address.ShouldBe(User2Address);
                whitelist.ExtraInfoIdList.Value[2].Id.ShouldBe(tagInfoId);
                whitelist.ExtraInfoIdList.Value[3].Address.ShouldBe(User4Address);
            }
            {
                var tag = await WhitelistContractStub.GetTagInfoByHash.CallAsync(tagInfoId);
                tag.TagName.ShouldBe("INFO2");
                tag.Info.ShouldBe(Info2);
            }
            {
                var extraInfo = await WhitelistContractStub.GetExtraInfoByTag.CallAsync(new GetExtraInfoByTagInput()
                {
                    WhitelistId = whitelistId,
                    TagInfoId = tagInfoId
                });
                extraInfo.Value.Count.ShouldBe(2);
                extraInfo.Value[1].Address.ShouldBe(User4Address);
                extraInfo.Value[1].Info.Info.ShouldBe(Info2);
            }
            {
                var tagInfo = await WhitelistContractStub.GetExtraInfoByAddress.CallAsync(
                    new GetExtraInfoByAddressInput()
                    {
                        WhitelistId = whitelistId,
                        Address = User4Address
                    });
                tagInfo.TagName.ShouldBe("INFO2");
            }
            return whitelistId;
        }

        [Fact]
        public async Task AddExtraInfoTest_TagExist()
        {
            var whitelistId = await CreateWhitelistTest();
            var executionResult = await WhitelistContractStub.AddExtraInfo.SendWithExceptionAsync(new AddExtraInfoInput()
            {
                TagInfo = new TagInfo()
                {
                    TagName = "INFO1",
                    Info = Info1
                },
                WhitelistId = whitelistId,
                ProjectId = _projectId,
                AddressList = new Whitelist.AddressList()
                {
                    Value = {User1Address, User4Address}
                }
            });
            executionResult.TransactionResult.Error.ShouldContain("The tag Info INFO1 already exists.");
        }

        [Fact]
        public async Task RemoveTagInfoTest()
        {
            var whitelistId = await AddExtraInfoTest_WithAddress();
            await RemoveExtraInfo(whitelistId);
            await WhitelistContractStub.RemoveTagInfo.SendAsync(new RemoveTagInfoInput()
            {
                WhitelistId = whitelistId,
                ProjectId = _projectId,
                TagId = CalculateId(DefaultAddress, _projectId, "INFO1")
            });
            {
                var tagIdList = await WhitelistContractStub.GetExtraInfoIdList.CallAsync(new GetExtraInfoIdListInput()
                {
                    Owner = DefaultAddress,
                    ProjectId = _projectId,
                    WhitelistId = whitelistId
                });
                tagIdList.Value.Count.ShouldBe(2);
                tagIdList.Value[0].ShouldBe(CalculateId(DefaultAddress,_projectId,"INFO3"));
            }
            {
                var exist = await WhitelistContractStub.GetExtraInfoFromWhitelist.CallAsync(
                    new GetExtraInfoFromWhitelistInput()
                    {
                        WhitelistId = whitelistId,
                        ExtraInfoId = new ExtraInfoId()
                        {
                            Address = User1Address,
                            Id = CalculateId(DefaultAddress, _projectId, "INFO3")
                        }
                    });
                exist.Value.ShouldBe(false);
            }
        }

        [Fact]
        public async Task RemoveTagInfoTest_WithAddress()
        {
            var whitelistId = await AddExtraInfoTest_WithAddress();
            var executionResult = await WhitelistContractStub.RemoveTagInfo.SendWithExceptionAsync(new RemoveTagInfoInput()
            {
                WhitelistId = whitelistId,
                ProjectId = _projectId,
                TagId = CalculateId(DefaultAddress, _projectId, "INFO2")
            });
            executionResult.TransactionResult.Error.ShouldContain("Exist address list.");
        }
        
        [Fact]
        public async Task RemoveTagInfoTest_IncorrectId()
        {
            var whitelistId = await AddExtraInfoTest_WithAddress();
            var executionResult = await WhitelistContractStub.RemoveTagInfo.SendWithExceptionAsync(new RemoveTagInfoInput()
            {
                WhitelistId = whitelistId,
                ProjectId = _projectId,
                TagId = CalculateId(DefaultAddress, _projectId, "INFO9")
            });
            executionResult.TransactionResult.Error.ShouldContain("Incorrect tagInfoId.");
        }
        
        private async Task RemoveExtraInfo(Hash whitelistId)
        {
            await WhitelistContractStub.RemoveAddressInfoFromWhitelist.SendAsync(
                new RemoveAddressInfoFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoId = new ExtraInfoId()
                    {
                        Address = User1Address,
                        Id = CalculateId(DefaultAddress, _projectId, "INFO1")
                    }
                });
        }
        
        [Fact]
        public async Task<Hash> AddAddressInfoToWhitelistTest()
        {
            var whitelistId = await CreateWhitelistTest();
            await WhitelistContractStub.AddAddressInfoToWhitelist.SendAsync(
                new AddAddressInfoToWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoId = new ExtraInfoId()
                    {
                        Address = User4Address,
                        Id = CalculateId(DefaultAddress, _projectId, "INFO1")
                    }
                });
            {
                var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
                whitelist.ExtraInfoIdList.Value[2].Address.ShouldBe(User4Address);
                whitelist.ExtraInfoIdList.Value[2].Id.ShouldBe(CalculateId(DefaultAddress, _projectId, "INFO1"));
            }
            {
                var whitelistExtra = await WhitelistContractStub.GetWhitelistDetail.CallAsync(whitelistId);
                whitelistExtra.Value[2].Info.TagName.ShouldBe("INFO1");
                whitelistExtra.Value[2].Info.Info.ShouldBe(Info1);
            }
            {
                var extraInfo = await WhitelistContractStub.GetExtraInfoByTag.CallAsync(new GetExtraInfoByTagInput()
                {
                    WhitelistId = whitelistId,
                    TagInfoId = CalculateId(DefaultAddress, _projectId, "INFO1")
                });
                extraInfo.Value.Count.ShouldBe(2);
                extraInfo.Value[0].Address.ShouldBe(User1Address);
                extraInfo.Value[1].Address.ShouldBe(User4Address);
            }
            {
                var extraInfoIdList = await WhitelistContractStub.GetExtraInfoIdList.CallAsync(new GetExtraInfoIdListInput()
                {
                    Owner = DefaultAddress,
                    ProjectId = _projectId,
                    WhitelistId = whitelistId
                });
                extraInfoIdList.Value.Count.ShouldBe(2);
                extraInfoIdList.Value[0].ShouldBe(CalculateId(DefaultAddress, _projectId, "INFO1"));
                extraInfoIdList.Value[1].ShouldBe(CalculateId(DefaultAddress, _projectId, "INFO3"));
            }
            {
                var tagInfo = await WhitelistContractStub.GetExtraInfoByAddress.CallAsync(
                    new GetExtraInfoByAddressInput()
                    {
                        WhitelistId = whitelistId,
                        Address = User1Address
                    });
                tagInfo.TagName.ShouldBe("INFO1");
            }
            return whitelistId;
        }
        
        [Fact]
        public async Task AddAddressInfoToWhitelistTest_WhitelistNotFound()
        {
            var whitelistId = await CreateWhitelistTest();
            var executionResult = await WhitelistContractStub.AddAddressInfoToWhitelist.SendWithExceptionAsync(
                new AddAddressInfoToWhitelistInput()
                {
                    WhitelistId = new Hash(),
                    ExtraInfoId = new ExtraInfoId()
                    {
                        Address = User4Address,
                        Id = CalculateId(DefaultAddress, _projectId, "INFO1")
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
                    ExtraInfoId = new ExtraInfoId()
                    {
                        Address = User3Address,
                        Id = CalculateId(DefaultAddress, _projectId, "INFO1")
                    }
                });
            executionResult.TransactionResult.Error.ShouldContain("Duplicate address.");
        }
        
        [Fact]
        public async Task<Hash> AddAddressInfoToWhitelistTest_OnlyAddress()
        {
            var whitelistId = await CreateWhitelist_Address();
            await WhitelistContractStub.AddAddressInfoToWhitelist.SendAsync(
                new AddAddressInfoToWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoId = new ExtraInfoId()
                    {
                        Address = User4Address
                    }
                });
            {
                var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
                whitelist.ExtraInfoIdList.Value[2].Address.ShouldBe(User4Address);
                whitelist.ExtraInfoIdList.Value[2].Id.ShouldBeNull();
            }
            {
                var exception = await WhitelistContractStub.GetExtraInfoByAddress.CallWithExceptionAsync(
                    new GetExtraInfoByAddressInput()
                    {
                        WhitelistId = whitelistId,
                        Address = User4Address
                    });
                exception.Value.ShouldContain("No Match tagInfo according to the address.");
            }
            return whitelistId;
        }

         [Fact]
        public async Task RemoveAddressInfoFromWhitelistTest_AddressExtra()
        {
            var whitelistId = await AddAddressInfoToWhitelistTest();
            await WhitelistContractStub.RemoveAddressInfoFromWhitelist.SendAsync(
                new RemoveAddressInfoFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoId = new ExtraInfoId()
                    {
                        Address = User1Address,
                        Id = CalculateId(DefaultAddress,_projectId,"INFO1")
                    }
                });
            {
                var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
                whitelist.ExtraInfoIdList.Value.Count.ShouldBe(2);
                whitelist.ExtraInfoIdList.Value[0].Address.ShouldBe(User3Address);
                whitelist.ExtraInfoIdList.Value[0].Id.ShouldBe(CalculateId(DefaultAddress, _projectId, "INFO3"));
            }
            {
                var tagInfo = await WhitelistContractStub.GetExtraInfoByTag.CallAsync(
                    new GetExtraInfoByTagInput()
                    {
                        WhitelistId = whitelistId,
                        TagInfoId = CalculateId(DefaultAddress, _projectId, "INFO1")
                    });
                tagInfo.Value.Count.ShouldBe(1);
                tagInfo.Value[0].Address.ShouldBe(User4Address);
            }
        }
        
        [Fact]
        public async Task RemoveAddressInfoFromWhitelistTest_Address()
        {
            var whitelistId = await AddAddressInfoToWhitelistTest_OnlyAddress();
            await WhitelistContractStub.RemoveAddressInfoFromWhitelist.SendAsync(
                new RemoveAddressInfoFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoId = new ExtraInfoId()
                    {
                        Address = User4Address
                    }
                });
            {
                var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
                whitelist.ExtraInfoIdList.Value.Count.ShouldBe(2);
                whitelist.ExtraInfoIdList.Value[1].Address.ShouldBe(User3Address);
            }
        }
        
        [Fact]
        public async Task RemoveAddressInfoFromWhitelistTest_NoMatchAddress()
        {
            var whitelistId = await AddAddressInfoToWhitelistTest_OnlyAddress();
            var executionResult1 = await WhitelistContractStub.RemoveAddressInfoFromWhitelist.SendWithExceptionAsync(
                new RemoveAddressInfoFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoId = new ExtraInfoId()
                    {
                        Address = User2Address
                    }
                });
            executionResult1.TransactionResult.Error.ShouldContain("Address doesn't exist.");
        }
        
        [Fact]
        public async Task RemoveAddressInfoFromWhitelistTest_NoMatchAddressExtra()
        {
            var whitelistId = await AddAddressInfoToWhitelistTest();
            var executionResult = await WhitelistContractStub.RemoveAddressInfoFromWhitelist.SendWithExceptionAsync(
                new RemoveAddressInfoFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoId = new ExtraInfoId()
                    {
                        Address = User1Address,
                        Id = CalculateId(DefaultAddress,_projectId,"INFO3")
                    }
                });
            executionResult.TransactionResult.Error.ShouldContain("ExtraInfo doesn't exist");
        }

        [Fact]
        public async Task<Hash> AddAddressInfoListToWhitelistTest()
        {
            var whitelistId = await CreateWhitelistTest();
            var executionResult = await WhitelistContractStub.AddAddressInfoListToWhitelist.SendAsync(
                new AddAddressInfoListToWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoIdList = new ExtraInfoIdList()
                    {
                        Value = {
                            new ExtraInfoId()
                            {
                                Address = User2Address,
                                Id = CalculateId(DefaultAddress,_projectId,"INFO1")
                            },
                            new ExtraInfoId()
                            {
                                Address = User4Address,
                                Id = CalculateId(DefaultAddress,_projectId,"INFO3")
                            }
                        }
                    }
                });
            {
                var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
                whitelist.ExtraInfoIdList.Value.Count.ShouldBe(4);
                whitelist.ExtraInfoIdList.Value[2].Address.ShouldBe(User2Address);
                whitelist.ExtraInfoIdList.Value[2].Id.ShouldBe(CalculateId(DefaultAddress,_projectId,"INFO1"));
                whitelist.ExtraInfoIdList.Value[3].Address.ShouldBe(User4Address);
                whitelist.ExtraInfoIdList.Value[3].Id.ShouldBe(CalculateId(DefaultAddress,_projectId,"INFO3"));
            }
            {
                var extraInfo = await WhitelistContractStub.GetTagInfoByHash.CallAsync(CalculateId(DefaultAddress,_projectId,"INFO1"));
                var deserializedExtraInfo = new PriceTag();
                deserializedExtraInfo.MergeFrom(extraInfo.Info);
                deserializedExtraInfo.Symbol.ShouldBe("ELF");
                deserializedExtraInfo.Amount.ShouldBe(200_000000);
                extraInfo.Info.ShouldBe(Info1);
            }
        
            {
                var extra = await WhitelistContractStub.GetExtraInfoByTag.CallAsync(new GetExtraInfoByTagInput()
                {
                    WhitelistId = whitelistId,
                    TagInfoId = CalculateId(DefaultAddress,_projectId,"INFO3")
                });
                extra.Value.Count.ShouldBe(2);
                extra.Value[0].Address.ShouldBe(User3Address);
                extra.Value[1].Address.ShouldBe(User4Address);
            }
            {
                var tagId = await WhitelistContractStub.GetExtraInfoByAddress.CallAsync(new GetExtraInfoByAddressInput()
                {
                    WhitelistId = whitelistId,
                    Address = User4Address
                });
                tagId.TagName.ShouldBe("INFO3");
                tagId.Info.ShouldBe(Info3);
            }
            {
                var ifExist = await WhitelistContractStub.GetExtraInfoFromWhitelist.CallAsync(
                    new GetExtraInfoFromWhitelistInput()
                    {
                        WhitelistId = whitelistId,
                        ExtraInfoId = new ExtraInfoId()
                        {
                            Address = User1Address,
                            Id = CalculateId(DefaultAddress, _projectId, "INFO1")
                        }
                    });
                ifExist.Value.ShouldBe(true);
            }
            return whitelistId;
        }

        [Fact]
        public async Task<Hash> AddAddressInfoListToWhitelistTest_Address()
        {
            var whitelistId = await CreateWhitelist_Address();
            await WhitelistContractStub.AddAddressInfoListToWhitelist.SendAsync(
                new AddAddressInfoListToWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoIdList = new ExtraInfoIdList()
                    {
                        Value =
                        {
                            new ExtraInfoId()
                            {
                                Address = User2Address
                            },
                            new ExtraInfoId()
                            {
                                Address = User4Address
                            }
                        }
                    }
                });
            {
                var whitelistInfo = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
                whitelistInfo.ExtraInfoIdList.Value.Count.ShouldBe(4);
                whitelistInfo.ExtraInfoIdList.Value[3].Address.ShouldBe(User4Address);
                whitelistInfo.ExtraInfoIdList.Value[3].Id.ShouldBeNull();
            }
            {
                var whitelistDetail = await WhitelistContractStub.GetWhitelistDetail.CallAsync(whitelistId);
                whitelistDetail.Value[0].Address.ShouldBe(User1Address);
                whitelistDetail.Value[0].Info.ShouldBeNull();
            }
            {
                var exist = await WhitelistContractStub.GetAddressFromWhitelist.CallAsync(
                    new GetAddressFromWhitelistInput()
                    {
                        WhitelistId = whitelistId,
                        Address = User3Address
                    });
                exist.Value.ShouldBe(true);
            }
            return whitelistId;
        }
        
        [Fact]
        public async Task AddAddressInfoListToWhitelistTest_Address_Duplicate()
        {
            var whitelistId = await CreateWhitelist_Address();
            var executionResult = await WhitelistContractStub.AddAddressInfoListToWhitelist.SendWithExceptionAsync(
                new AddAddressInfoListToWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoIdList = new ExtraInfoIdList()
                    {
                        Value = { 
                            new ExtraInfoId()
                            {
                                Address  = User1Address
                            },
                            new ExtraInfoId() 
                            {
                                Address = User2Address
                            }
                        }
                    }
                });
            executionResult.TransactionResult.Error.ShouldContain("Address already exists in whitelist.");
        }
        
        [Fact]
        public async Task AddAddressInfoListToWhitelistTest_ExtraInfo_Duplicate()
        {
            var whitelistId = await CreateWhitelistTest();
            var executionResult = await WhitelistContractStub.AddAddressInfoListToWhitelist.SendWithExceptionAsync(
                new AddAddressInfoListToWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoIdList = new ExtraInfoIdList()
                    {
                        Value = { 
                            new ExtraInfoId()
                            {
                                Address  = User1Address,
                                Id = CalculateId(DefaultAddress,_projectId,"INFO3")
                            },
                            new ExtraInfoId() 
                            {
                                Address = User2Address,
                                Id = CalculateId(DefaultAddress,_projectId,"INFO1")
                            }
                        }
                    }
                });
            executionResult.TransactionResult.Error.ShouldContain("Address already exists in whitelist.");
        }
        
        [Fact]
        public async Task RemoveAddressInfoListFromWhitelistTest()
        {
            var whitelistId = await AddAddressInfoListToWhitelistTest();
            await WhitelistContractStub.RemoveAddressInfoListFromWhitelist.SendAsync(
                new RemoveAddressInfoListFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoIdList = new ExtraInfoIdList()
                    {
                        Value =
                        {
                            new ExtraInfoId()
                            {
                                Address = User1Address,
                                Id = CalculateId(DefaultAddress,_projectId,"INFO1")
                            },
                            new ExtraInfoId()
                            {
                                Address = User2Address,
                                Id = CalculateId(DefaultAddress,_projectId,"INFO1")
                            }
                        }
                    }
                });
            {
                var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
                whitelist.ExtraInfoIdList.Value.Count.ShouldBe(2);
                whitelist.ExtraInfoIdList.Value[0].Address.ShouldBe(User3Address);
                whitelist.ExtraInfoIdList.Value[0].Id.ShouldBe(CalculateId(DefaultAddress, _projectId, "INFO3"));
            }
            {
                var exception = await WhitelistContractStub.GetExtraInfoByTag.CallWithExceptionAsync(new GetExtraInfoByTagInput()
                {
                    WhitelistId = whitelistId,
                    TagInfoId = CalculateId(DefaultAddress, _projectId, "INFO1")
                });
                exception.Value.ShouldContain("No address list under the current tag.");
            }
        }

        [Fact]
        public async Task RemoveAddressInfoListFromWhitelistTest_Address()
        {
            var whitelistId = await AddAddressInfoListToWhitelistTest_Address();
            await WhitelistContractStub.RemoveAddressInfoListFromWhitelist.SendAsync(
                new RemoveAddressInfoListFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoIdList = new ExtraInfoIdList()
                    {
                        Value =
                        {
                            new ExtraInfoId()
                            {
                                Address = User1Address
                            },
                            new ExtraInfoId()
                            {
                                Address = User2Address
                            }
                        }
                    }
                });
            {
                var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
                whitelist.ExtraInfoIdList.Value.Count.ShouldBe(2);
                whitelist.ExtraInfoIdList.Value[0].Address.ShouldBe(User3Address);
                whitelist.ExtraInfoIdList.Value[0].Id.ShouldBeNull();
            }
            {
                var exist = await WhitelistContractStub.GetAddressFromWhitelist.CallAsync(
                    new GetAddressFromWhitelistInput()
                    {
                        WhitelistId = whitelistId,
                        Address = User1Address
                    });
                exist.Value.ShouldBe(false);
            }
        }
        
        [Fact]
        public async Task RemoveAddressInfoListFromWhitelistTest_NoMatchAddress()
        {
            var whitelistId = await AddAddressInfoListToWhitelistTest();
            var executionResult = await WhitelistContractStub.RemoveAddressInfoListFromWhitelist.SendWithExceptionAsync(
                new RemoveAddressInfoListFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoIdList = new ExtraInfoIdList()
                    {
                        Value =
                        {
                            new ExtraInfoId()
                            {
                                Address = new Address(),
                                Id = CalculateId(DefaultAddress,_projectId,"INFO1")
                            },
                            new ExtraInfoId()
                            {
                                Address = new Address(),
                                Id = CalculateId(DefaultAddress,_projectId,"INFO1")
                            },
                        }
                    }
                });
            executionResult.TransactionResult.Error.ShouldContain("These extraInfos do not exist.");
        }
        
        [Fact]
        public async Task RemoveAddressInfoListFromWhitelistTest_DuplicateAddress()
        {
            var whitelistId = await AddAddressInfoListToWhitelistTest();
            var executionResult = await WhitelistContractStub.RemoveAddressInfoListFromWhitelist.SendWithExceptionAsync(
                new RemoveAddressInfoListFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoIdList = new ExtraInfoIdList()
                    {
                        Value =
                        {
                            new ExtraInfoId()
                            {
                                Address = User1Address,
                                Id = CalculateId(DefaultAddress,_projectId,"INFO1")
                            },
                            new ExtraInfoId()
                            {
                                Address = User1Address,
                                Id = CalculateId(DefaultAddress,_projectId,"INFO3")
                            },
                        }
                    }
                });
            executionResult.TransactionResult.Error.ShouldContain("Duplicate addresses.");
        }
        [Fact]
        public async Task DisableWhitelistTest()
        {
            var whitelistId = await CreateWhitelistTest();
            var executionResult = await WhitelistContractStub.DisableWhitelist.SendWithExceptionAsync(new Hash());
            executionResult.TransactionResult.Error.ShouldContain("Whitelist not found.");
            await WhitelistContractStub.DisableWhitelist.SendAsync(whitelistId);
            var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
            whitelist.IsAvailable.ShouldBe(false);
        }
        
        [Fact]
        public async Task ReenableWhitelistTest()
        {
            var whitelistId = await CreateWhitelistTest();
            var executionResult = await WhitelistContractStub.EnableWhitelist.SendWithExceptionAsync(new Hash());
            executionResult.TransactionResult.Error.ShouldContain("Whitelist not found.");
            var executionResult2 = await WhitelistContractStub.EnableWhitelist.SendWithExceptionAsync(whitelistId);
            executionResult2.TransactionResult.Error.ShouldContain("The whitelist is already available.");
            await WhitelistContractStub.DisableWhitelist.SendAsync(whitelistId);
            {
                var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
                whitelist.IsAvailable.ShouldBe(false);
            }
            await WhitelistContractStub.EnableWhitelist.SendAsync(whitelistId);
            {
                var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
                whitelist.IsAvailable.ShouldBe(true);
            }
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
        public async Task UpdateExtraInfoTest()
        {
            var whitelistId = await AddAddressInfoListToWhitelistTest();
            await WhitelistContractStub.UpdateExtraInfo.SendAsync(
                new UpdateExtraInfoInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoList = new ExtraInfoId()
                    {
                        Address = User3Address,
                        Id = CalculateId(DefaultAddress,_projectId,"INFO1")
                    }
                });
            {
                var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
                whitelist.ExtraInfoIdList.Value.Count.ShouldBe(4);
                whitelist.ExtraInfoIdList.Value[1].Address.ShouldBe(User3Address);
                whitelist.ExtraInfoIdList.Value[1].Id.ShouldBe(CalculateId(DefaultAddress, _projectId, "INFO1"));
            }
            {
                var extraInfo = await WhitelistContractStub.GetExtraInfoByTag.CallAsync(new GetExtraInfoByTagInput()
                {
                    WhitelistId = whitelistId,
                    TagInfoId = CalculateId(DefaultAddress, _projectId, "INFO1")
                });
                extraInfo.Value.Count.ShouldBe(3);
                extraInfo.Value[0].Address.ShouldBe(User1Address);
                extraInfo.Value[2].Address.ShouldBe(User3Address);
            }
            {
                var extraInfo = await WhitelistContractStub.GetExtraInfoByTag.CallAsync(new GetExtraInfoByTagInput()
                {
                    WhitelistId = whitelistId,
                    TagInfoId = CalculateId(DefaultAddress, _projectId, "INFO3")
                });
                extraInfo.Value.Count.ShouldBe(1);
                extraInfo.Value[0].Address.ShouldBe(User4Address);
            }
            {
                var tagInfo = await WhitelistContractStub.GetExtraInfoByAddress.CallAsync(
                    new GetExtraInfoByAddressInput()
                    {
                        WhitelistId = whitelistId,
                        Address = User3Address
                    });
                tagInfo.TagName.ShouldBe("INFO1");
                tagInfo.Info.ShouldBe(Info1);
            }
            {
                var exist = await WhitelistContractStub.GetExtraInfoFromWhitelist.CallAsync(
                    new GetExtraInfoFromWhitelistInput()
                    {
                        WhitelistId = whitelistId,
                        ExtraInfoId = new ExtraInfoId()
                        {
                            Address = User3Address,
                            Id = CalculateId(DefaultAddress, _projectId, "INFO3")
                        }
                    });
                exist.Value.ShouldBe(false);
            }
        }
        
        [Fact]
        public async Task UpdateExtraInfoTest_NoMatchAddress()
        {
            var whitelistId = await AddAddressInfoListToWhitelistTest();
            var executionResult = await WhitelistContractStub.UpdateExtraInfo.SendWithExceptionAsync(
                new UpdateExtraInfoInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoList = new ExtraInfoId()
                    {
                        Address = User5Address,
                        Id = CalculateId(DefaultAddress,_projectId,"INFO1")
                    }
                });
            executionResult.TransactionResult.Error.ShouldContain("ExtraInfo not match.");
        }
        
        [Fact]
        public async Task UpdateExtraInfoTest_IncorrectTag()
        {
            var whitelistId = await AddAddressInfoListToWhitelistTest();
            var executionResult = await WhitelistContractStub.UpdateExtraInfo.SendWithExceptionAsync(
                new UpdateExtraInfoInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoList = new ExtraInfoId()
                    {
                        Address = User2Address,
                        Id = CalculateId(DefaultAddress,_projectId,"INFO5")
                    }
                });
            executionResult.TransactionResult.Error.ShouldContain("Incorrect extraInfoId.");
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
            {
                var managerList = await WhitelistContractStub.GetManagerList.CallAsync(whitelistId);
                managerList.Value.ShouldContain(User1Address);
                managerList.Value.ShouldNotContain(DefaultAddress);
            }
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
                var exception = await WhitelistContractStub.GetWhitelistByManager.CallWithExceptionAsync(User4Address);
                exception.Value.ShouldContain("No whitelist according to the manager.");
            }
        }

        [Fact]
        public async Task ResetWhitelist()
        {
            var whitelistId = await CreateWhitelistTest();
            await WhitelistContractStub.ResetWhitelist.SendAsync(new ResetWhitelistInput()
            {
                WhitelistId = whitelistId,
                ProjectId = _projectId
            });
            {
                var whitelist = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
                whitelist.ExtraInfoIdList.Value.Count.ShouldBe(0);
                whitelist.ProjectId.ShouldBe(_projectId);
            }
            {
                var exception = await WhitelistContractStub.GetExtraInfoIdList.CallWithExceptionAsync(new GetExtraInfoIdListInput()
                {
                    Owner = DefaultAddress,
                    ProjectId = _projectId,
                    WhitelistId = whitelistId
                });
                exception.Value.ShouldContain("No extraInfo id list.");
            }
        }
    }
}