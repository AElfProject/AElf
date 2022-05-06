using AElf.Types;

namespace AElf.Contracts.WhiteList
{
    public partial class WhiteListContract
    {
        private Hash CalculateWhiteListHash(AddressList input)
        {
            return HashHelper.ComputeFrom(input);
        }

        private Hash CalculateSubscribeWhiteListHash(string input)
        {
            return HashHelper.ComputeFrom(input);
        }
        private WhiteListInfo AssertWhiteListInfo(Hash whiteListId)
        {
            var whiteListInfo = State.WhiteListInfoMap[whiteListId];
            Assert(whiteListInfo != null,$"WhiteList not found.{whiteListId.ToHex()}");
            return whiteListInfo;
        }

        private SubscribeWhiteListInfo AssertSubscribeWhiteListInfo(Hash subscribeId)
        {
            var subscribeInfo = State.SubscribeWhiteListInfoMap[subscribeId];
            Assert(subscribeInfo != null,$"Subscribe info not found.{subscribeId.ToHex()}");
            return subscribeInfo;
        }
        
    }
}