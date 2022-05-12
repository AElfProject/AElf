using System.Collections.Generic;
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
            var whiteListInfo = AssertWhitelistInfo(input.WhitelistId);
            var subscribeId = CalculateSubscribeWhitelistHash(Context.Sender,input.ProjectId,input.WhitelistId);
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

        public override Empty UnsubscribeWhitelist(Hash input)
        {
            var subscribeInfo = AssertSubscribeWhitelistInfo(input);
            State.ConsumedListMap.Remove(input);
            Context.Fire(new WhitelistUnsubscribed()
            {
                SubscribeId = subscribeInfo.SubscribeId,
                ProjectId = subscribeInfo.ProjectId,
                WhitelistId = subscribeInfo.WhitelistId
            });
            return new Empty();
        }

        public override Empty ConsumeWhitelist(ConsumeWhitelistInput input)
        {
            var subscribeInfo = AssertSubscribeWhitelistInfo(input.SubscribeId);
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
            var whiteListInfo = AssertWhitelistInfo(input.WhitelistId).Clone();
            Assert(whiteListInfo.IsCloneable, "Whitelist is not allowed to be cloned.");
            var cloneWhiteListId = CalculateCloneWhitelistHash(Context.Sender,input.WhitelistId);
            Assert(State.WhitelistInfoMap[cloneWhiteListId] != null, "WhiteList has already been cloned.");
            var whitelistClone = new WhitelistInfo()
            {
                WhitelistId = cloneWhiteListId,
                ExtraInfoIdList = whiteListInfo.ExtraInfoIdList,
                IsAvailable = whiteListInfo.IsAvailable,
                IsCloneable = whiteListInfo.IsCloneable,
                Remark = whiteListInfo.Remark,
                CloneFrom = whiteListInfo.WhitelistId
            };
            State.WhitelistInfoMap[cloneWhiteListId] = whitelistClone;
            Context.Fire(new WhitelistCreated()
            {
                WhitelistId = whitelistClone.WhitelistId,
                ExtraInfoIdList = whitelistClone.ExtraInfoIdList,
                IsAvailable = whitelistClone.IsAvailable,
                Remark = whitelistClone.Remark,
                CloneFrom = whiteListInfo.WhitelistId,
                Manager = Context.Sender
            });
            return new Empty();
        }

        
    }
}