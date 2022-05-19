using System.Linq;
using AElf.Contracts.Whitelist.Extensions;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Whitelist
{
    public partial class WhitelistContract
    {
        private Hash CalculateWhitelistHash(Address address,ExtraInfoList input)
        {
            return Context.GenerateId(Context.Self, ByteArrayHelper.ConcatArrays(address.ToByteArray(),input.ToByteArray()));
        }

        private Hash CalculateSubscribeWhitelistHash(Address address,Hash projectId,Hash whitelistId)
        {
            return HashHelper.ComputeFrom($"{address}{projectId}{whitelistId}");
        }

        private Hash CalculateCloneWhitelistHash(Address address,Hash whitelistId)
        {
            return HashHelper.ComputeFrom($"{address}{whitelistId}");
        }
        
        private WhitelistInfo AssertWhitelistInfo(Hash whitelistId)
        {
            var whitelistInfo = State.WhitelistInfoMap[whitelistId];
            Assert(whitelistInfo != null,$"Whitelist not found.{whitelistId.ToHex()}");
            return whitelistInfo;
        }
        
        private WhitelistInfo AssertWhitelistIsAvailable(Hash whitelistId)
        {
            var whitelistInfo = State.WhitelistInfoMap[whitelistId];
            Assert(whitelistInfo.IsAvailable, $"Whitelist is not available.{whitelistId.ToHex()}");
            return whitelistInfo;
        }

        private WhitelistInfo AssertWhitelistManager(Hash whitelistId)
        {
            var whitelistInfo = GetWhitelist(whitelistId);
            Assert(whitelistInfo.Manager.Value.Contains(Context.Sender),$"{Context.Sender} is not the manager of the whitelist.");
            return whitelistInfo;
        }

        private WhitelistInfo AssertWhitelistCreator(Hash whitelistId)
        {
            var whitelistInfo = GetWhitelist(whitelistId);
            Assert(whitelistInfo.Creator == Context.Sender,$"{Context.Sender}No permission.");
            return whitelistInfo;
        }

        private SubscribeWhitelistInfo AssertSubscribeWhitelistInfo(Hash subscribeId)
        {
            var subscribeInfo = State.SubscribeWhitelistInfoMap[subscribeId];
            Assert(subscribeInfo != null, $"Subscribe info not found.{subscribeId.ToHex()}");
            return subscribeInfo;
        }

        private ExtraInfoId AssertExtraInfoIsExist(Hash whitelistId, ExtraInfo info)
        {
            var whitelist = State.WhitelistInfoMap[whitelistId];
            var extraInfoId = ConvertExtraInfo(info);
            var ifExist = whitelist.ExtraInfoIdList.Value.Contains(extraInfoId);
            Assert(!ifExist, $"ExtraInfo already exists.{whitelistId}{info}");
            return extraInfoId;
        }
        
        private ExtraInfoId AssertExtraInfoIsNotExist(Hash subscribeId, ExtraInfoId info)
        {
            var whitelist = GetAvailableWhitelist(subscribeId);
            var ifExist = whitelist.Value.Contains(ConvertExtraInfoId(new ExtraInfoIdList(){Value = { info }}).Value[0]);
            Assert(ifExist, $"ExtraInfo doesn't exist in the available whitelist.{info}");
            return info;
        }

        /// <summary>
        ///Convert extra_info to extra_info_id
        /// </summary>
        /// <returns>AddressExtraIdInfo</returns>
        private ExtraInfoId ConvertExtraInfo(ExtraInfo input)
        {
            var infoId = input.Info.CalculateExtraInfoId();
            var extraInfo = GetExtraInfoByHash(infoId);
            if (extraInfo == null)
            {
                State.ExtraInfoMap[infoId] = new BytesValue()
                {
                    Value = input.Info
                };
                Context.Fire(new ExtraInfoAdded
                {
                    ExtraInfoId = infoId,
                    ExtraInfo = State.ExtraInfoMap[infoId].Value
                });
            }
            var extraInfoId = new ExtraInfoId
            {
                Address = input.Address,
                Id = infoId
            };
            
            return extraInfoId;
        }

        private ExtraInfoList ConvertExtraInfoId(ExtraInfoIdList input)
        {
            var extraInfoList = input.Value.Select(info =>
            {
                var extra = State.ExtraInfoMap[info.Id];
                return new ExtraInfo()
                {
                    Address = info.Address,
                    Info = extra.Value
                };
            }).ToList();
            return new ExtraInfoList()
            {
                Value = {extraInfoList}
            };
        }

        /// <summary>
        ///remove address or address+extra_info
        /// </summary>
        /// <returns>AddressExtraIdInfo</returns>
        private ExtraInfoId RemoveAddressOrExtra(WhitelistInfo whiteListInfo, ExtraInfo extraInfo)
        {
            if (extraInfo.Info.IsEmpty)
            {
                var address = extraInfo.Address;
                var resultList = whiteListInfo.ExtraInfoIdList.Value
                    .Where(u => u.Address.Equals(address)).ToList();
                Assert(resultList.Count != 0, $"Address doesn't exist.{resultList}");
                foreach (var result in resultList)
                {
                    whiteListInfo.ExtraInfoIdList.Value.Remove(result);
                }
                State.WhitelistInfoMap[whiteListInfo.WhitelistId] = whiteListInfo;
                return new ExtraInfoId()
                {
                    Address = address
                };
            }
            else
            {
                var toRemove = whiteListInfo.ExtraInfoIdList.Value
                    .Where(u => u.Address == extraInfo.Address && u.Id == extraInfo.Info.CalculateExtraInfoId())
                    .ToList();
                Assert(toRemove.Count != 0, $"Address and extra info doesn't exist.{toRemove}");
                foreach (var result in toRemove)
                {
                    whiteListInfo.ExtraInfoIdList.Value.Remove(result);
                }
                State.WhitelistInfoMap[whiteListInfo.WhitelistId] = whiteListInfo;
                return ConvertExtraInfo(extraInfo);
            }
        }
        
        private AddressList SetManagerList(Hash whitelistId,AddressList input)
        {
            var managerList = input ?? new AddressList();
            if (!managerList.Value.Contains(Context.Sender))
            {
                managerList.Value.Add(Context.Sender);
            }
            State.ManagerListMap[whitelistId] = managerList;
            return State.ManagerListMap[whitelistId];
        }

        private Empty SetWhitelistIdManager(Hash whitelistId,AddressList managerList)
        {
            foreach (var manager in managerList.Value)
            {
                var whitelistIdList = State.WhitelistIdMap[manager] ?? new WhitelistIdList();
                whitelistIdList.WhitelistId.Add(whitelistId);
                State.WhitelistIdMap[manager] = whitelistIdList;
            }

            return new Empty();
        }
        
        private Empty RemoveWhitelistIdManager(Hash whitelistId,AddressList managerList)
        {
            foreach (var manager in managerList.Value)
            {
                var whitelistIdList = State.WhitelistIdMap[manager] ?? new WhitelistIdList();
                whitelistIdList.WhitelistId.Remove(whitelistId);
                State.WhitelistIdMap[manager] = whitelistIdList;
            }

            return new Empty();
        }
    }
}