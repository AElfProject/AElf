using System.Linq;
using AElf.Contracts.Whitelist.Extensions;
using AElf.Sdk.CSharp;
using AElf.Types;

namespace AElf.Contracts.Whitelist
{
    public partial class WhitelistContract
    {
        private Hash CalculateWhitelistHash(Address address, ExtraInfoList input)
        {
            return HashHelper.ComputeFrom($"{address}{input}");
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
            if (whitelistInfo == null)
            {
                throw new AssertionException($"Whitelist not found.{whitelistId.ToHex()}");
            }
            Assert(whitelistInfo.IsAvailable, $"Whitelist is not available.{whitelistId.ToHex()}");
            return whitelistInfo;
        }

        private WhitelistInfo AssertWhitelistManager(Hash whitelistId)
        {
            var whitelistInfo = GetWhitelist(whitelistId);
            Assert(whitelistInfo.Manager == Context.Sender,$"{Context.Sender} is not the manager of the whitelist.");
            return whitelistInfo;
        }

        private SubscribeWhitelistInfo AssertSubscribeWhitelistInfo(Hash subscribeId)
        {
            var subscribeInfo = State.SubscribeWhitelistInfoMap[subscribeId];
            Assert(subscribeInfo != null, $"Subscribe info not found.{subscribeId.ToHex()}");
            return subscribeInfo;
        }

        /// <summary>
        ///Convert extra_info to extra_info_id
        /// </summary>
        /// <returns>AddressExtraIdInfo</returns>
        private ExtraInfoId ConvertExtraInfo(ExtraInfo input)
        {
            var infoId = input.Info.CalculateExtraInfoId();
            var extraInfo = GetExtraInfoByHash(infoId);
            State.ExtraInfoMap[infoId].Value = extraInfo?.Value == null ? input.Info : extraInfo.Value;
            var extraInfoId = new ExtraInfoId
            {
                Address = input.Address,
                Id = infoId
            };
            return extraInfoId;
        }

        /// <summary>
        ///remove address or address+extra_info
        /// </summary>
        /// <returns>AddressExtraIdInfo</returns>
        private ExtraInfoId RemoveAddressOrExtra(WhitelistInfo whiteListInfo, ExtraInfo extraInfo)
        {
            
            if (extraInfo.Info == null)
            {
                var address = extraInfo.Address;
                var resultList = whiteListInfo.ExtraInfoIdList.Value
                    .Where(u => u.Address.Equals(address)).ToList();
                Assert(resultList.Count != 0, "Address doesn't exist.");
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
                Assert(toRemove.Count != 0, "Address and extra info doesn't exist.");
                foreach (var result in toRemove)
                {
                    whiteListInfo.ExtraInfoIdList.Value.Remove(result);
                }
                State.WhitelistInfoMap[whiteListInfo.WhitelistId] = whiteListInfo;
                return ConvertExtraInfo(extraInfo);
            }
        }
    }
}