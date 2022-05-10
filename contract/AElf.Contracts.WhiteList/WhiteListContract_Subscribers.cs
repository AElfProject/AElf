using System.Linq;
using AElf.Contracts.Whitelist.Extensions;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Whitelist
{
    public partial class WhitelistContract
    {
        public override Hash SubscribeWhitelist(SubscribeWhitelistInput input)
        {
            var whiteListInfo = AssertWhiteListInfo(input.WhitelistId);
            var subscribeId = CalculateSubscribeWhiteListHash($"{input.ProjectId}{input.WhitelistId}");
            Assert(State.SubscribeWhitelistInfoMap[subscribeId] == null, "Subscribe info already exist.");
            var subscribeWhiteListInfo = new SubscribeWhitelistInfo
            {
                SubscribeId = subscribeId,
                ProjectId = input.ProjectId,
                WhitelistId = whiteListInfo.WhitelistId
            };
            State.SubscribeWhitelistInfoMap[subscribeId] = subscribeWhiteListInfo;
            Context.Fire(new WhitelistSubscribed
            {
                SubscribeId = subscribeId,
                ProjectId = subscribeWhiteListInfo.ProjectId,
                WhitelistId = subscribeWhiteListInfo.WhitelistId,
            });
            return subscribeId;
        }

        public override Empty CancelSubscribeWhitelist(Hash input)
        {
            AssertSubscribeWhiteListInfo(input);
            var consumedList = State.ConsumedListMap[input].ExtraInfoIdList.Value;
            consumedList.Clear();
            return new Empty();
        }

        public override Empty AddAddressInfoToConsumedList(AddAddressInfoToConsumedListInput input)
        {
            var subscribeInfo = AssertSubscribeWhiteListInfo(input.SubscribeId);
            if (State.ConsumedListMap[subscribeInfo.SubscribeId] != null)
            {
                var consumedList = GetConsumedList(subscribeInfo.SubscribeId);
                consumedList.ExtraInfoIdList.Value.Add(input.ExtraInfoId);
                State.ConsumedListMap[subscribeInfo.SubscribeId] = consumedList;
                Context.Fire(new ConsumedListAdded
                {
                    SubscribeId = consumedList.SubscribeId,
                    WhitelistId = subscribeInfo.WhitelistId,
                    ExtraInfoIdList = new ExtraInfoIdList { Value = { input.ExtraInfoId } }
                });
            }
            else
            {
                var addressInfoList = new ExtraInfoIdList();
                addressInfoList.Value.Add(input.ExtraInfoId);
                var consumedList = new ConsumedList
                {
                    SubscribeId = subscribeInfo.SubscribeId,
                    WhitelistId = subscribeInfo.WhitelistId,
                    ExtraInfoIdList = addressInfoList
                };
                State.ConsumedListMap[subscribeInfo.SubscribeId] = consumedList;
                Context.Fire(new ConsumedListAdded
                {
                    SubscribeId = consumedList.SubscribeId,
                    WhitelistId = subscribeInfo.WhitelistId,
                    ExtraInfoIdList = new ExtraInfoIdList { Value = { input.ExtraInfoId } }
                });
            }

            return new Empty();
        }

        public override Empty CloneWhitelist(CloneWhitelistInput input)
        {
            var whiteListInfo = AssertWhiteListInfo(input.WhitelistId).Clone();
            Assert(whiteListInfo.IsCloneable, "Whitelist is not allowed to be cloned.");
            var cloneWhiteListId = CalculateCloneWhiteListHash($"{Context.Sender}{input.WhitelistId}");
            Assert(State.CloneWhitelistInfoMap[cloneWhiteListId] != null, "WhiteList has already been cloned.");
            State.CloneWhitelistInfoMap[cloneWhiteListId] = whiteListInfo;
            Context.Fire(new WhitelistCloned()
            {
                CloneId = cloneWhiteListId,
                WhitelistId = whiteListInfo.WhitelistId
            });
            return new Empty();
        }

        public override Empty SetClonedWhitelistExtraInfo(SetClonedWhitelistExtraInfoInput input)
        {
            if (input.ExtraInfoList == null)
            {
                throw new AssertionException("Address and extra info is null.");
            }

            AssertClonedWhiteListInfo(input.CloneWhitelistId);
            var whiteListInfo = GetCloneWhitelist(input.CloneWhitelistId).Clone();
            var addressExtraList = GetWhitelist(whiteListInfo.WhitelistId).Clone().ExtraInfoIdList;
            var addressList = addressExtraList.Value.Select(info =>
            {
                var matchAddress = new ExtraInfoId();
                foreach (var inputValue in input.ExtraInfoList.Value)
                {
                    if (inputValue.Address.Equals(info.Address))
                    {
                        var extraInfoId = inputValue.Info.CalculateExtraInfoId();
                        var extra = GetExtraInfoByHash(extraInfoId);
                        State.ExtraInfoMap[extraInfoId] = extra;
                        info.Id = extraInfoId;
                        matchAddress.Address = inputValue.Address;
                        matchAddress.Id = extraInfoId;
                    }

                    break;
                }

                return matchAddress;
            }).ToList();
            State.CloneWhitelistInfoMap[input.CloneWhitelistId] = whiteListInfo;
            Context.Fire(new SetClonedWhitelist
            {
                CloneId = input.CloneWhitelistId,
                WhitelistId = whiteListInfo.WhitelistId,
                ExtraInfoIdList = new ExtraInfoIdList()
                {
                    Value = { addressList }
                }
            });
            return new Empty();
        }
    }
}