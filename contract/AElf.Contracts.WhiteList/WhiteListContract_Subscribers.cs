using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.WhiteList
{
    public partial class WhiteListContract
    {
        public override Hash SubscribeWhiteList(SubscribeWhiteListInput input)
        {
            var whiteListInfo = AssertWhiteListInfo(input.WhitelistId);
            var subscribeId = CalculateSubscribeWhiteListHash($"{input.ProjectId}{input.WhitelistId}");
            Assert(State.SubscribeWhiteListInfoMap[subscribeId] == null,"Subscribe info already exist.");
            var subscribeWhiteListInfo = new SubscribeWhiteListInfo
            {
                SubscribeId = subscribeId,
                ProjectId = input.ProjectId,
                WhitelistId = whiteListInfo.WhitelistId
            };
            State.SubscribeWhiteListInfoMap[subscribeId] = subscribeWhiteListInfo;
            Context.Fire(new WhiteListSubscribed()
            {
                SubscribeId = subscribeId,
                ProjectId = subscribeWhiteListInfo.ProjectId,
                WhitelistId = subscribeWhiteListInfo.WhitelistId,
            });
            return subscribeId;
        }

        public override Empty CancelSubscribeWhiteList(Hash input)
        {
            AssertSubscribeWhiteListInfo(input);
            var consumedList = State.ConsumedListMap[input].AddressExtraInfoList.Value;
            consumedList.Clear();
            return new Empty();
        }

        public override Empty AddAddressInfoToConsumedList(AddAddressInfoToConsumedListInput input)
        {
            var subscribeInfo = AssertSubscribeWhiteListInfo(input.SubscribeId);
            if (State.ConsumedListMap[subscribeInfo.SubscribeId] != null)
            {
                var consumedList = GetConsumedList(subscribeInfo.SubscribeId);
                consumedList.AddressExtraInfoList.Value.Add(input.AddressExtraInfo);
                State.ConsumedListMap[subscribeInfo.SubscribeId] = consumedList;
                Context.Fire(new ConsumedListAdded
                {
                    SubscribeId = consumedList.SubscribeId,
                    WhitelistId = subscribeInfo.WhitelistId,
                    AddressExtraInfo = input.AddressExtraInfo
                });
            }
            else
            {
                var addressInfoList = new AddressExtraIdInfoList();
                addressInfoList.Value.Add(input.AddressExtraInfo);
                var consumedList = new ConsumedList
                {
                    SubscribeId = subscribeInfo.SubscribeId,
                    WhitelistId = subscribeInfo.WhitelistId,
                    AddressExtraInfoList = addressInfoList
                };
                State.ConsumedListMap[subscribeInfo.SubscribeId] = consumedList;
                Context.Fire(new ConsumedListAdded
                {
                    SubscribeId = consumedList.SubscribeId,
                    WhitelistId = subscribeInfo.WhitelistId,
                    AddressExtraInfo = input.AddressExtraInfo
                });
            }
            return new Empty();
        }

        public override Empty CloneWhiteList(CloneWhiteListInput input)
        {
            var whiteListInfo = AssertWhiteListInfo(input.WhitelistId).Clone();
            Assert(whiteListInfo.IsCloned,"WhiteList is not allowed to be cloned.");
            var cloneWhiteListId = CalculateCloneWhiteListHash($"{Context.Sender}{input.WhitelistId}");
            Assert(State.CloneWhiteListInfoMap[cloneWhiteListId] != null,"WhiteList has already been cloned.");
            State.CloneWhiteListInfoMap[cloneWhiteListId] = whiteListInfo;
            Context.Fire(new WhiteListCloned
            {
                CloneId = cloneWhiteListId,
                WhitelistId = whiteListInfo.WhitelistId
            });
            return new Empty();
        }

        public override Empty SetClonedWhiteListExtraInfo(SetClonedWhiteListExtraInfoInput input)
        {
            Assert(input.AddressExtraInfo != null, "Address and extra info is null.");
            AssertClonedWhiteListInfo(input.CloneWhitelistId);
            var whiteListInfo = GetCloneWhiteList(input.CloneWhitelistId).Clone();
            var addressExtraList = GetWhiteList(whiteListInfo.WhitelistId).Clone().AddressExtraInfoList;
            var addressList = addressExtraList.Value.Select(info =>
            {
                var matchAddress = new AddressExtraIdInfo();
                foreach (var inputValue in input.AddressExtraInfo)
                {
                    if (inputValue.Address.Equals(info.Address))
                    {
                        var extraInfo = CalculateExtraInfoHash(inputValue.ExtraInfo.ToByteArray());
                        var extra = GetExtraInfoByHash(extraInfo) ?? new ExtraInfo
                        {
                            ExtraInfoId = extraInfo,
                            ExtraInfo_ = inputValue.ExtraInfo
                        };
                        State.ExtraInfoMap[extraInfo] = extra;
                        info.ExtraInfoId = extraInfo;
                        matchAddress.Address = inputValue.Address;
                        matchAddress.ExtraInfoId = extraInfo;
                    }
                    break;
                }
                return matchAddress;
            }).ToList();
            State.CloneWhiteListInfoMap[input.CloneWhitelistId] = whiteListInfo;
            Context.Fire(new SetClonedWhiteList
            {
                CloneId = input.CloneWhitelistId,
                WhitelistId = whiteListInfo.WhitelistId,
                AddressExtraInfo = new AddressExtraIdInfoList
                {
                    Value = { addressList }
                }
            });
            return new Empty();
        }
        
    }
}