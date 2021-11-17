using System;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFT
{
    public partial class NFTContract
    {
        public override Hash Mint(MintInput input)
        {
            if (input.Metadata.Value.Any())
            {
                AssertMetadataKeysAreCorrect(input.Metadata.Value.Keys);
            }

            return PerformMint(input);
        }

        public override Empty Transfer(TransferInput input)
        {
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            DoTransfer(tokenHash, Context.Sender, input.To, input.Amount);
            return new Empty();
        }

        private void DoTransfer(Hash tokenHash, Address from, Address to, long amount)
        {
            if (amount <= 1)
            {
                amount = 1;
            }

            Assert(State.BalanceMap[tokenHash][from] > 0, "Insufficient balance.");
            State.BalanceMap[tokenHash][from] = State.BalanceMap[tokenHash][from].Sub(amount);
            State.BalanceMap[tokenHash][to] = State.BalanceMap[tokenHash][to].Add(amount);
        }

        public override Empty TransferFrom(TransferFromInput input)
        {
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            Assert(State.ApprovedAmountMap[tokenHash][input.From][Context.Sender] >= input.Amount, "Not approved.");
            DoTransfer(tokenHash, input.From, input.To, input.Amount);
            return new Empty();
        }

        public override Empty Burn(BurnInput input)
        {
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            var nftInfo = State.NftInfoMap[tokenHash];
            var nftProtocolInfo = State.NftProtocolMap[input.Symbol];
            Assert(
                State.BalanceMap[tokenHash][Context.Sender] > input.Amount &&
                nftProtocolInfo.MinterList.Value.Contains(Context.Sender),
                "No permission.");
            nftProtocolInfo.MintedCount = nftProtocolInfo.MintedCount.Sub(input.Amount);
            nftInfo.Quantity = nftInfo.Quantity.Sub(input.Amount);

            State.NftProtocolMap[input.Symbol] = nftProtocolInfo;
            if (nftInfo.Quantity <= 0)
            {
                nftInfo.IsBurned = true;
            }

            State.NftInfoMap[tokenHash] = nftInfo;
            return new Empty();
        }

        public override Hash Assemble(AssembleInput input)
        {
            if (input.Metadata.Value.Any())
            {
                AssertMetadataKeysAreCorrect(input.Metadata.Value.Keys);
            }

            var metadata = input.Metadata;

            if (input.AssembledNfts.Value.Any())
            {
                metadata.Value.Add(AssembledNftsKey, input.AssembledNfts.ToString());
                // Check owner.
                foreach (var pair in input.AssembledNfts.Value)
                {
                    var nftHash = Hash.LoadFromHex(pair.Key);
                    var nftInfo = State.NftInfoMap[nftHash];
                    Assert(State.BalanceMap[nftHash][Context.Sender] > pair.Value,
                        $"Insufficient balance of {nftInfo.Symbol}{nftInfo.TokenId}.");
                    DoTransfer(nftHash, Context.Sender, Context.Self, pair.Value <= 1 ? 1 : pair.Value);
                }
            }

            if (input.AssembledFts.Value.Any())
            {
                metadata.Value.Add(AssembledFtsKey, input.AssembledFts.ToString());
                // Check balance and allowance.
                foreach (var pair in input.AssembledFts.Value)
                {
                    var symbol = pair.Key;
                    var amount = pair.Value;
                    var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
                    {
                        Owner = Context.Sender,
                        Symbol = symbol
                    }).Balance;
                    Assert(balance >= amount, $"Insufficient balance of {symbol}");
                    var allowance = State.TokenContract.GetAllowance.Call((new GetAllowanceInput
                    {
                        Owner = Context.Sender,
                        Spender = Context.Self,
                        Symbol = symbol
                    })).Allowance;
                    Assert(amount >= allowance, $"Insufficient allowance of {symbol}");
                    State.TokenContract.TransferFrom.Send(new MultiToken.TransferFromInput
                    {
                        From = Context.Sender,
                        To = Context.Self,
                        Symbol = symbol,
                        Amount = amount
                    });
                }
            }

            var mingInput = new MintInput
            {
                Symbol = input.Symbol,
                Alias = input.Alias,
                Owner = input.Owner,
                Uri = input.Uri,
                Metadata = metadata
            };

            return PerformMint(mingInput);
        }

        public override Empty Disassemble(DisassembleInput input)
        {
            Burn(new BurnInput
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId
            });

            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            var nftInfo = State.NftInfoMap[tokenHash];
            if (nftInfo.Metadata.Value.ContainsKey(AssembledNftsKey))
            {
                var nfts = JsonParser.Default.Parse<AssembledNfts>(nftInfo.Metadata.Value[AssembledNftsKey]);
                foreach (var pair in nfts.Value)
                {
                    DoTransfer(Hash.LoadFromHex(pair.Key), Context.Self, Context.Sender, pair.Value);
                }
            }

            if (nftInfo.Metadata.Value.ContainsKey(AssembledFtsKey))
            {
                var fts = JsonParser.Default.Parse<AssembledFts>(nftInfo.Metadata.Value[AssembledFtsKey]);
                foreach (var pair in fts.Value)
                {
                    State.TokenContract.Transfer.Send(new MultiToken.TransferInput
                    {
                        Symbol = pair.Key,
                        Amount = pair.Value,
                        To = Context.Sender
                    });
                }
            }

            return new Empty();
        }

        public override Empty Recast(RecastInput input)
        {
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            var nftInfo = State.NftInfoMap[tokenHash];
            Assert(nftInfo.Quantity == 1, "Do not support recast.");
            var nftProtocolInfo = State.NftProtocolMap[input.Symbol];
            Assert(
                State.BalanceMap[tokenHash][Context.Sender] > 0 &&
                nftProtocolInfo.MinterList.Value.Contains(Context.Sender),
                "No permission.");
            if (input.Alias != null)
            {
                nftInfo.Alias = input.Alias;
            }

            if (input.Uri != null)
            {
                nftInfo.Uri = input.Uri;
            }

            var metadata = new Metadata();
            // Need to keep reserved metadata key.
            foreach (var reservedKey in GetNftMetadataReservedKeys())
            {
                if (nftInfo.Metadata.Value.ContainsKey(reservedKey))
                {
                    metadata.Value.Add(reservedKey, nftInfo.Metadata.Value[reservedKey]);
                }

                if (input.Metadata.Value.ContainsKey(reservedKey))
                {
                    input.Metadata.Value.Remove(reservedKey);
                }
            }

            metadata.Value.Add(input.Metadata.Value);

            if (input.Metadata.Value.Any())
            {
                nftInfo.Metadata = input.Metadata;
            }

            return new Empty();
        }

        public override Empty Approve(ApproveInput input)
        {
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            var nftInfo = State.NftInfoMap[tokenHash];
            Assert(State.BalanceMap[tokenHash][Context.Sender] >= input.Amount, "Insufficient amount.");
            State.ApprovedAmountMap[tokenHash][Context.Sender][input.Spender] = input.Amount;
            return new Empty();
        }

        public override Empty UnApprove(UnApproveInput input)
        {
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            var nftInfo = State.NftInfoMap[tokenHash];
            var allowance = State.ApprovedAmountMap[tokenHash][Context.Sender][input.Spender];
            var currentAllowance = Math.Max(allowance.Sub(input.Amount), 0);
            State.ApprovedAmountMap[tokenHash][Context.Sender][input.Spender] = currentAllowance;
            return new Empty();
        }

        private Hash CalculateTokenHash(string symbol, long tokenId)
        {
            return HashHelper.ComputeFrom($"{symbol}{tokenId}");
        }

        public override Empty AddMinters(AddMintersInput input)
        {
            var protocolInfo = State.NftProtocolMap[input.Symbol];
            Assert(Context.Sender == protocolInfo.Creator, "No permission.");
            foreach (var minter in input.MinterList.Value)
            {
                if (!protocolInfo.MinterList.Value.Contains(minter))
                {
                    protocolInfo.MinterList.Value.Add(minter);
                }
            }

            State.NftProtocolMap[input.Symbol] = protocolInfo;
            return new Empty();
        }

        public override Empty RemoveMiners(RemoveMinersInput input)
        {
            var protocolInfo = State.NftProtocolMap[input.Symbol];
            Assert(Context.Sender == protocolInfo.Creator, "No permission.");
            foreach (var minter in input.MinterList.Value)
            {
                if (protocolInfo.MinterList.Value.Contains(minter))
                {
                    protocolInfo.MinterList.Value.Remove(minter);
                }
            }

            State.NftProtocolMap[input.Symbol] = protocolInfo;
            return new Empty();
        }

        private MinterList GetMinterList(TokenInfo tokenInfo)
        {
            var minterList = State.MinterListMap[tokenInfo.Symbol] ?? new MinterList();
            if (!minterList.Value.Contains(tokenInfo.Issuer))
            {
                minterList.Value.Add(tokenInfo.Issuer);
            }

            return minterList;
        }

        private Hash PerformMint(MintInput input)
        {
            var tokenInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
            {
                Symbol = input.Symbol
            });
            var protocolInfo = State.NftProtocolMap[input.Symbol];
            if (protocolInfo == null)
            {
                throw new AssertionException($"Invalid NFT Token symbol: {input.Symbol}");
            }

            var tokenId = protocolInfo.MintedCount.Add(1);
            var minterList = GetMinterList(tokenInfo);
            Assert(minterList.Value.Contains(Context.Sender), "No permission to mint.");
            Assert(tokenInfo.IssueChainId == Context.ChainId, "Incorrect chain.");

            var tokenHash = CalculateTokenHash(input.Symbol, tokenId);

            var quantity = input.Quantity > 0 ? input.Quantity : 1;

            // Inherit from protocol info.
            var nftMetadata = protocolInfo.Metadata;
            foreach (var pair in input.Metadata.Value)
            {
                nftMetadata.Value.Add(pair.Key, pair.Value);
            }

            var nftInfo = new NFTInfo
            {
                Symbol = input.Symbol,
                BaseUri = protocolInfo.BaseUri,
                Uri = input.Uri,
                TokenName = tokenInfo.TokenName,
                TokenId = tokenId,
                Creator = protocolInfo.Creator,
                Metadata = nftMetadata,
                Minter = Context.Sender,
                Quantity = quantity
            };
            State.NftInfoMap[tokenHash] = nftInfo;
            State.BalanceMap[tokenHash][input.Owner ?? Context.Sender] = quantity;

            protocolInfo.MintedCount = protocolInfo.MintedCount.Add(quantity);
            State.NftProtocolMap[input.Symbol] = protocolInfo;
            return tokenHash;
        }
    }
}