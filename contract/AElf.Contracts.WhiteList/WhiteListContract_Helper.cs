using System.Linq;
using AElf.Types;

namespace AElf.Contracts.WhiteList
{
    public partial class WhiteListContract
    {
        private Hash CalculateWhiteListHash(AddressExtraInfoList input)
        {
            return HashHelper.ComputeFrom(input);
        }

        private Hash CalculateSubscribeWhiteListHash(string input)
        {
            return HashHelper.ComputeFrom(input);
        }

        private Hash CalculateExtraInfoHash(byte[] input)
        {
            return HashHelper.ComputeFrom(input);
        }

        private Hash CalculateCloneWhiteListHash(string input)
        {
            return HashHelper.ComputeFrom(input);
        }
        private WhiteListInfo AssertWhiteListInfo(Hash whiteListId)
        {
            var whiteListInfo = State.WhiteListInfoMap[whiteListId];
            Assert(whiteListInfo != null,$"WhiteList not found.{whiteListId.ToHex()}");
            Assert(whiteListInfo != null && whiteListInfo.IsAvailable, $"WhiteList is not available.{whiteListId.ToHex()}");
            return whiteListInfo;
        }

        private SubscribeWhiteListInfo AssertSubscribeWhiteListInfo(Hash subscribeId)
        {
            var subscribeInfo = State.SubscribeWhiteListInfoMap[subscribeId];
            Assert(subscribeInfo != null,$"Subscribe info not found.{subscribeId.ToHex()}");
            return subscribeInfo;
        }

        private WhiteListInfo AssertClonedWhiteListInfo(Hash cloneId)
        {
            var whiteListInfo = State.CloneWhiteListInfoMap[cloneId];
            Assert(whiteListInfo != null ,$"WhiteList cloned info not exist.{cloneId.ToHex()}");
            return whiteListInfo;
        }
        
        /// <summary>
        ///Convert extra_info to extra_id_info.
        /// </summary>
        /// <returns>AddressExtraIdInfo</returns>
        private AddressExtraIdInfo ConvertExtraInfo(AddressExtraInfo input)
        {
            var extraInfoId = CalculateExtraInfoHash(input.ExtraInfo.ToByteArray());
            var extraInfo = GetExtraInfoByHash(extraInfoId) ?? new ExtraInfo
                {
                    ExtraInfoId = extraInfoId,
                    ExtraInfo_ = input.ExtraInfo
                };
            State.ExtraInfoMap[extraInfoId] = extraInfo;
            var addressExtraInfo = new AddressExtraIdInfo 
            {
                Address = input.Address, 
                ExtraInfoId = extraInfoId
            };
            return addressExtraInfo;
        }
        
        /// <summary>
        ///remove address or address+extra_info
        /// </summary>
        /// <returns>AddressExtraIdInfo</returns>
        private AddressExtraIdInfo RemoveAddressOrExtra(WhiteListInfo whiteListInfo,AddressExtraInfo input)
        {
            var addressExtraIdInfo = ConvertExtraInfo(input);
            if (input.ExtraInfo == null)
            {
                var address = addressExtraIdInfo.Address;
                var resultList = whiteListInfo.AddressExtraInfoList.Value
                    .Where(u => u.Address.Equals(address)).ToList();
                Assert(resultList.Count != 0,"Address doesn't exist.");
                foreach (var result in resultList)
                {
                    whiteListInfo.AddressExtraInfoList.Value.Remove(result);
                }
                State.WhiteListInfoMap[whiteListInfo.WhitelistId] = whiteListInfo;
            }
            else
            {
                var resultList = whiteListInfo.AddressExtraInfoList.Value
                    .Where(u => u.Equals(addressExtraIdInfo)).ToList();
                Assert(resultList.Count != 0,"Address and extra info doesn't exist.");
                foreach (var result in resultList)
                {
                    whiteListInfo.AddressExtraInfoList.Value.Remove(result);
                } 
                State.WhiteListInfoMap[whiteListInfo.WhitelistId] = whiteListInfo;
            }
            return addressExtraIdInfo;
        }
    }
}