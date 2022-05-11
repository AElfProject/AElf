using System.Linq;
using AElf.Contracts.Whitelist.Extensions;
using AElf.Types;

namespace AElf.Contracts.Whitelist
{
    public partial class WhitelistContract
    {
        private Hash CalculateWhitelistHash(ExtraInfoList input)
        {
            return HashHelper.ComputeFrom(input);
        }

        private Hash CalculateSubscribeWhiteListHash(string input)
        {
            return HashHelper.ComputeFrom(input);
        }

        private Hash CalculateCloneWhiteListHash(string input)
        {
            return HashHelper.ComputeFrom(input);
        }

        private WhitelistInfo AssertWhiteListInfo(Hash whiteListId)
        {
            var whiteListInfo = State.WhitelistInfoMap[whiteListId];
            Assert(whiteListInfo != null, $"WhiteList not found.{whiteListId.ToHex()}");
            Assert(whiteListInfo != null && whiteListInfo.IsAvailable,
                $"WhiteList is not available.{whiteListId.ToHex()}");
            return whiteListInfo;
        }

        private SubscribeWhitelistInfo AssertSubscribeWhiteListInfo(Hash subscribeId)
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
            var extraInfoId = ConvertExtraInfo(extraInfo);
            if (extraInfo.Info == null)
            {
                var address = extraInfoId.Address;
                var resultList = whiteListInfo.ExtraInfoIdList.Value
                    .Where(u => u.Address.Equals(address)).ToList();
                Assert(resultList.Count != 0, "Address doesn't exist.");
                foreach (var result in resultList)
                {
                    whiteListInfo.ExtraInfoIdList.Value.Remove(result);
                }

                State.WhitelistInfoMap[whiteListInfo.WhitelistId] = whiteListInfo;
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
            }

            return extraInfoId;
        }
    }
}