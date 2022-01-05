using AElf.Contracts.NFT;
using AElf.Types;

namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContract
    {
        private void CheckSenderNFTBalanceAndAllowance(string symbol, long tokenId, long quantity)
        {
            var balance = State.NFTContract.GetBalance.Call(new GetBalanceInput
            {
                Symbol = symbol,
                TokenId = tokenId,
                Owner = Context.Sender
            }).Balance;
            Assert(balance >= quantity, "Check sender NFT balance failed.");
            var allowance = State.NFTContract.GetAllowance.Call(new GetAllowanceInput
            {
                Symbol = symbol,
                TokenId = tokenId,
                Owner = Context.Sender,
                Spender = Context.Self
            }).Allowance;
            Assert(allowance >= quantity, "Check sender NFT allowance failed.");
        }
    }
}