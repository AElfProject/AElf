using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
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

            var nftMinted = PerformMint(input);
            return nftMinted.TokenHash;
        }

        public override Empty Transfer(TransferInput input)
        {
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            DoTransfer(tokenHash, Context.Sender, input.To, input.Amount);
            Context.Fire(new Transferred
            {
                From = Context.Sender,
                To = input.To,
                Amount = input.Amount,
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                Memo = input.Memo
            });
            return new Empty();
        }

        /// <summary>
        /// Throw Assertion Exception if amount is negative or balance insufficient.
        /// Do nothing if amount is 0 or balance of from address is 0.
        /// </summary>
        /// <param name="tokenHash"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="amount"></param>
        /// <exception cref="AssertionException"></exception>
        private void DoTransfer(Hash tokenHash, Address from, Address to, long amount)
        {
            if (amount < 0)
            {
                throw new AssertionException("Invalid transfer amount.");
            }

            if (amount == 0 || State.BalanceMap[tokenHash][from] == 0)
            {
                return;
            }

            Assert(State.BalanceMap[tokenHash][from] >= amount, "Insufficient balance.");
            State.BalanceMap[tokenHash][from] = State.BalanceMap[tokenHash][from].Sub(amount);
            State.BalanceMap[tokenHash][to] = State.BalanceMap[tokenHash][to].Add(amount);
        }

        public override Empty TransferFrom(TransferFromInput input)
        {
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            var operatorList = State.OperatorMap[input.Symbol][input.From];
            var isOperator = operatorList?.Value.Contains(Context.Sender) ?? false;
            if (!isOperator)
            {
                var allowance = State.AllowanceMap[tokenHash][input.From][Context.Sender];
                Assert(allowance >= input.Amount, "Not approved.");
                State.AllowanceMap[tokenHash][input.From][Context.Sender] = allowance.Sub(input.Amount);
            }

            DoTransfer(tokenHash, input.From, input.To, input.Amount);
            Context.Fire(new Transferred
            {
                From = input.From,
                To = input.To,
                Amount = input.Amount,
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                Memo = input.Memo
            });
            return new Empty();
        }

        public override Empty Burn(BurnInput input)
        {
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            var nftInfo = GetNFTInfoByTokenHash(tokenHash);
            var nftProtocolInfo = State.NftProtocolMap[input.Symbol];
            Assert(nftProtocolInfo.IsBurnable,
                $"NFT Protocol {nftProtocolInfo.ProtocolName} of symbol {nftProtocolInfo.Symbol} is not burnable.");
            var minterList = State.MinterListMap[input.Symbol] ?? new MinterList();
            Assert(
                State.BalanceMap[tokenHash][Context.Sender] >= input.Amount &&
                minterList.Value.Contains(Context.Sender),
                "No permission.");
            State.BalanceMap[tokenHash][Context.Sender] = State.BalanceMap[tokenHash][Context.Sender].Sub(input.Amount);
            nftProtocolInfo.Supply = nftProtocolInfo.Supply.Sub(input.Amount);
            nftInfo.Quantity = nftInfo.Quantity.Sub(input.Amount);

            State.NftProtocolMap[input.Symbol] = nftProtocolInfo;
            if (nftInfo.Quantity == 0 && !nftProtocolInfo.IsTokenIdReuse)
            {
                nftInfo.IsBurned = true;
            }

            State.NftInfoMap[tokenHash] = nftInfo;

            Context.Fire(new Burned
            {
                Burner = Context.Sender,
                Symbol = input.Symbol,
                Amount = input.Amount,
                TokenId = input.TokenId
            });
            return new Empty();
        }

        public override Hash Assemble(AssembleInput input)
        {
            if (input.Metadata != null && input.Metadata.Value.Any())
            {
                AssertMetadataKeysAreCorrect(input.Metadata.Value.Keys);
            }

            var metadata = input.Metadata ?? new Metadata();

            if (input.AssembledNfts.Value.Any())
            {
                metadata.Value.Add(AssembledNftsKey, input.AssembledNfts.ToString());
                // Check owner.
                foreach (var pair in input.AssembledNfts.Value)
                {
                    var nftHash = Hash.LoadFromHex(pair.Key);
                    var nftInfo = GetNFTInfoByTokenHash(nftHash);
                    Assert(State.BalanceMap[nftHash][Context.Sender] >= pair.Value,
                        $"Insufficient balance of {nftInfo.Symbol}{nftInfo.TokenId}.");
                    DoTransfer(nftHash, Context.Sender, Context.Self, pair.Value);
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
                    var balance = State.TokenContract.GetBalance.Call(new MultiToken.GetBalanceInput()
                    {
                        Owner = Context.Sender,
                        Symbol = symbol
                    }).Balance;
                    Assert(balance >= amount, $"Insufficient balance of {symbol}");
                    var allowance = State.TokenContract.GetAllowance.Call((new MultiToken.GetAllowanceInput()
                    {
                        Owner = Context.Sender,
                        Spender = Context.Self,
                        Symbol = symbol
                    })).Allowance;
                    Assert(allowance >= amount, $"Insufficient allowance of {symbol}");
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
                Metadata = metadata,
                TokenId = input.TokenId
            };

            var nftMinted = PerformMint(mingInput, true);
            if (input.AssembledNfts.Value.Any())
            {
                State.AssembledNftsMap[nftMinted.TokenHash] = input.AssembledNfts;
            }

            if (input.AssembledFts.Value.Any())
            {
                State.AssembledFtsMap[nftMinted.TokenHash] = input.AssembledFts;
            }

            Context.Fire(new Assembled
            {
                Symbol = input.Symbol,
                TokenId = nftMinted.TokenId,
                AssembledNfts = input.AssembledNfts,
                AssembledFts = input.AssembledFts
            });

            return nftMinted.TokenHash;
        }

        public override Empty Disassemble(DisassembleInput input)
        {
            Burn(new BurnInput
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                Amount = 1
            });

            var receiver = input.Owner ?? Context.Sender;

            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            var assembledNfts = State.AssembledNftsMap[tokenHash].Clone();
            if (assembledNfts != null)
            {
                var nfts = assembledNfts;
                foreach (var pair in nfts.Value)
                {
                    DoTransfer(Hash.LoadFromHex(pair.Key), Context.Self, receiver, pair.Value);
                }

                State.AssembledNftsMap.Remove(tokenHash);
            }

            var assembledFts = State.AssembledFtsMap[tokenHash].Clone();
            if (assembledFts != null)
            {
                var fts = assembledFts;
                foreach (var pair in fts.Value)
                {
                    State.TokenContract.Transfer.Send(new MultiToken.TransferInput
                    {
                        Symbol = pair.Key,
                        Amount = pair.Value,
                        To = receiver
                    });
                }

                State.AssembledFtsMap.Remove(tokenHash);
            }

            Context.Fire(new Disassembled
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                DisassembledNfts = assembledNfts ?? new AssembledNfts(),
                DisassembledFts = assembledFts ?? new AssembledFts()
            });

            return new Empty();
        }

        public override Empty ApproveProtocol(ApproveProtocolInput input)
        {
            Assert(State.NftProtocolMap[input.Symbol] != null, $"Protocol {input.Symbol} not exists.");
            var operatorList = State.OperatorMap[input.Symbol][Context.Sender] ?? new AddressList();
            switch (input.Approved)
            {
                case true when !operatorList.Value.Contains(input.Operator):
                    operatorList.Value.Add(input.Operator);
                    break;
                case false when operatorList.Value.Contains(input.Operator):
                    operatorList.Value.Remove(input.Operator);
                    break;
            }

            State.OperatorMap[input.Symbol][Context.Sender] = operatorList;
            return new Empty();
        }

        public override Empty Recast(RecastInput input)
        {
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            var minterList = State.MinterListMap[input.Symbol] ?? new MinterList();
            Assert(minterList.Value.Contains(Context.Sender), "No permission.");
            var nftInfo = GetNFTInfoByTokenHash(tokenHash);
            Assert(nftInfo.Quantity != 0 && nftInfo.Quantity == State.BalanceMap[tokenHash][Context.Sender],
                "Do not support recast.");
            if (input.Alias != null)
            {
                nftInfo.Alias = input.Alias;
            }

            if (input.Uri != null)
            {
                nftInfo.Uri = input.Uri;
            }

            var oldMetadata = nftInfo.Metadata.Clone();
            var metadata = new Metadata();
            // Need to keep reserved metadata key.
            foreach (var reservedKey in GetNftMetadataReservedKeys())
            {
                if (oldMetadata.Value.ContainsKey(reservedKey))
                {
                    metadata.Value.Add(reservedKey, oldMetadata.Value[reservedKey]);
                }

                if (input.Metadata.Value.ContainsKey(reservedKey))
                {
                    input.Metadata.Value.Remove(reservedKey);
                }
            }

            metadata.Value.Add(input.Metadata.Value);
            nftInfo.Metadata = metadata;

            State.NftInfoMap[tokenHash] = nftInfo;
            Context.Fire(new Recasted
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                OldMetadata = oldMetadata,
                NewMetadata = nftInfo.Metadata,
                Alias = nftInfo.Alias,
                Uri = nftInfo.Uri
            });
            return new Empty();
        }

        public override Empty Approve(ApproveInput input)
        {
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            State.AllowanceMap[tokenHash][Context.Sender][input.Spender] = input.Amount;
            Context.Fire(new Approved
            {
                Owner = Context.Sender,
                Spender = input.Spender,
                Symbol = input.Symbol,
                Amount = input.Amount,
                TokenId = input.TokenId
            });
            return new Empty();
        }

        public override Empty UnApprove(UnApproveInput input)
        {
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            var oldAllowance = State.AllowanceMap[tokenHash][Context.Sender][input.Spender];
            var currentAllowance = oldAllowance.Sub(input.Amount);
            if (currentAllowance <= 0)
            {
                currentAllowance = 0;
            }

            State.AllowanceMap[tokenHash][Context.Sender][input.Spender] = currentAllowance;

            Context.Fire(new UnApproved
            {
                Owner = Context.Sender,
                Spender = input.Spender,
                Symbol = input.Symbol,
                CurrentAllowance = currentAllowance,
                TokenId = input.TokenId
            });
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
            var minterList = State.MinterListMap[protocolInfo.Symbol] ?? new MinterList();

            foreach (var minter in input.MinterList.Value)
            {
                if (!minterList.Value.Contains(minter))
                {
                    minterList.Value.Add(minter);
                }
            }

            State.MinterListMap[input.Symbol] = minterList;
            return new Empty();
        }

        public override Empty RemoveMinters(RemoveMintersInput input)
        {
            var protocolInfo = State.NftProtocolMap[input.Symbol];
            Assert(Context.Sender == protocolInfo.Creator, "No permission.");
            var minterList = State.MinterListMap[protocolInfo.Symbol];

            foreach (var minter in input.MinterList.Value)
            {
                if (minterList.Value.Contains(minter))
                {
                    minterList.Value.Remove(minter);
                }
            }

            State.MinterListMap[input.Symbol] = minterList;
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

        private NFTMinted PerformMint(MintInput input, bool isTokenIdMustBeUnique = false)
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

            var tokenId = input.TokenId == 0 ? protocolInfo.Issued.Add(1) : input.TokenId;
            var tokenHash = CalculateTokenHash(input.Symbol, tokenId);
            var nftInfo = State.NftInfoMap[tokenHash];
            if (!protocolInfo.IsTokenIdReuse || isTokenIdMustBeUnique)
            {
                Assert(nftInfo == null, $"Token id {tokenId} already exists. Please assign a different token id.");
            }

            var minterList = GetMinterList(tokenInfo);
            Assert(minterList.Value.Contains(Context.Sender), "No permission to mint.");
            Assert(tokenInfo.IssueChainId == Context.ChainId, "Incorrect chain.");

            var quantity = input.Quantity > 0 ? input.Quantity : 1;
            protocolInfo.Supply = protocolInfo.Supply.Add(quantity);
            protocolInfo.Issued = protocolInfo.Issued.Add(quantity);
            Assert(protocolInfo.Issued <= protocolInfo.TotalSupply, "Total supply exceeded.");
            State.NftProtocolMap[input.Symbol] = protocolInfo;

            // Inherit from protocol info.
            var nftMetadata = protocolInfo.Metadata.Clone();
            foreach (var pair in input.Metadata.Value)
            {
                if (!nftMetadata.Value.ContainsKey(pair.Key))
                {
                    nftMetadata.Value.Add(pair.Key, pair.Value);
                }
            }

            if (nftInfo == null)
            {
                nftInfo = new NFTInfo
                {
                    Symbol = input.Symbol,
                    Uri = input.Uri ?? string.Empty,
                    TokenId = tokenId,
                    Metadata = nftMetadata,
                    Minters = {Context.Sender},
                    Quantity = quantity,
                    Alias = input.Alias,

                    // No need.
                    //BaseUri = protocolInfo.BaseUri,
                    //Creator = protocolInfo.Creator,
                    //ProtocolName = protocolInfo.ProtocolName
                };
            }
            else
            {
                nftInfo.Quantity = nftInfo.Quantity.Add(quantity);
                if (!nftInfo.Minters.Contains(Context.Sender))
                {
                    nftInfo.Minters.Add(Context.Sender);
                }
            }

            State.NftInfoMap[tokenHash] = nftInfo;
            var owner = input.Owner ?? Context.Sender;
            State.BalanceMap[tokenHash][owner] = State.BalanceMap[tokenHash][owner].Add(quantity);

            var nftMinted = new NFTMinted
            {
                Symbol = input.Symbol,
                ProtocolName = protocolInfo.ProtocolName,
                TokenId = tokenId,
                Metadata = nftMetadata,
                Owner = owner,
                Minter = Context.Sender,
                Quantity = quantity,
                Alias = input.Alias,
                BaseUri = protocolInfo.BaseUri,
                Uri = input.Uri ?? string.Empty,
                Creator = protocolInfo.Creator,
                NftType = protocolInfo.NftType,
                TotalQuantity = nftInfo.Quantity,
                TokenHash = tokenHash
            };
            Context.Fire(nftMinted);

            return nftMinted;
        }
    }
}