using System.Linq;
using AElf.Contracts.Parliament;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System.Text;
using Acs0;
using Acs1;
using Acs7;
using AElf.CSharp.Core;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract
    {
        private static bool IsValidSymbolChar(char character)
        {
            return character >= 'A' && character <= 'Z';
        }

        private TokenInfo AssertValidToken(string symbol, long amount)
        {
            AssertValidSymbolAndAmount(symbol, amount);
            var tokenInfo = State.TokenInfos[symbol];
            Assert(tokenInfo != null && !string.IsNullOrEmpty(tokenInfo.Symbol), $"Token is not found. {symbol}");
            return tokenInfo;
        }
        
        private void AssertValidSymbolAndAmount(string symbol, long amount)
        {
            Assert(!string.IsNullOrEmpty(symbol) && symbol.All(IsValidSymbolChar),
                "Invalid symbol.");
            Assert(amount > 0, "Invalid amount.");
        }

        private void AssertValidMemo(string memo)
        {
            Assert(memo == null || Encoding.UTF8.GetByteCount(memo) <= TokenContractConstants.MemoMaxLength,
                "Invalid memo size.");
        }

        private void DoTransfer(Address from, Address to, string symbol, long amount, string memo = null)
        {
            Assert(from != to, "Can't do transfer to sender itself.");
            AssertValidMemo(memo);
            ModifyBalance(from, symbol, -amount);
            ModifyBalance(to, symbol, amount);
            Context.Fire(new Transferred
            {
                From = from,
                To = to,
                Symbol = symbol,
                Amount = amount,
                Memo = memo ?? string.Empty
            });
        }

        private void ModifyBalance(Address address, string symbol, long addAmount)
        {
            var before = GetBalance(address, symbol);
            if (addAmount < 0 && before < -addAmount)
            {
                Assert(false, $"Insufficient balance. {symbol}: {before} / {-addAmount}");
            }
            var target = before.Add(addAmount);
            State.Balances[address][symbol] = target;
        }

        private long GetBalance(Address address, string symbol)
        {
            return State.Balances[address][symbol];
        }

        private void AssertSystemContractOrLockWhiteListAddress(string symbol)
        {
            var symbolState = State.LockWhiteLists[symbol];
            var isInWhiteList = symbolState != null && symbolState[Context.Sender];
            var systemContractAddresses = Context.GetSystemContractNameToAddressMapping().Values;
            var isSystemContractAddress = systemContractAddresses.Contains(Context.Sender);
            Assert(isInWhiteList || isSystemContractAddress, "No Permission.");
        }

        private Address ExtractTokenContractAddress(ByteString bytes)
        {
            var validateSystemContractAddressInput = ValidateSystemContractAddressInput.Parser.ParseFrom(bytes);
            var validatedAddress = validateSystemContractAddressInput.Address;
            var validatedContractHashName = validateSystemContractAddressInput.SystemContractHashName;

            Assert(validatedContractHashName == SmartContractConstants.TokenContractSystemHashName,
                "Address validation failed.");
            return validatedAddress;
        }

        private void AssertCrossChainTransaction(Transaction originalTransaction, Address validAddress, params string[] validMethodNames)
        {
            var validateResult = validMethodNames.Contains(originalTransaction.MethodName) 
                                 && originalTransaction.To == validAddress;
            Assert(validateResult, "Invalid transaction.");
        }

        private void RegisterTokenInfo(TokenInfo tokenInfo)
        {
            var existing = State.TokenInfos[tokenInfo.Symbol];
            Assert(existing == null || existing.Equals(new TokenInfo()), "Token already exists.");
            Assert(!string.IsNullOrEmpty(tokenInfo.Symbol) && tokenInfo.Symbol.All(IsValidSymbolChar),
                "Invalid symbol.");
            Assert(!string.IsNullOrEmpty(tokenInfo.TokenName), $"Invalid token name. {tokenInfo.Symbol}");
            Assert(tokenInfo.TotalSupply > 0, "Invalid total supply.");
            Assert(tokenInfo.Issuer != null, "Invalid issuer address.");
            State.TokenInfos[tokenInfo.Symbol] = tokenInfo;
        }

        private void CrossChainVerify(Hash transactionId, long parentChainHeight, int chainId, MerklePath merklePath)
        {
            var verificationInput = new VerifyTransactionInput
            {
                TransactionId = transactionId,
                ParentChainHeight = parentChainHeight,
                VerifiedChainId = chainId,
                Path = merklePath
            };
            var address = Context.GetContractAddressByName(SmartContractConstants.CrossChainContractSystemName);

            var verificationResult = Context.Call<BoolValue>(address,
                nameof(ACS7Container.ACS7ReferenceState.VerifyTransaction), verificationInput);
            Assert(verificationResult.Value, "Cross chain verification failed.");
        }

        private AuthorityInfo GetCrossChainTokenContractRegistrationController()
        {
            var parliamentContractAddress =
                Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            var controller = new AuthorityInfo
            {
                ContractAddress = State.ParliamentContract.Value,
                OwnerAddress = Context.Call<Address>(parliamentContractAddress,
                    nameof(ParliamentContractContainer.ParliamentContractReferenceState.GetDefaultOrganizationAddress),
                    new Empty())
            };
            return controller;
        }

        private int GetIssueChainId(string symbol)
        {
            var tokenInfo = State.TokenInfos[symbol];
            return tokenInfo.IssueChainId;
        }

        private void AssertValidCreateInput(CreateInput input)
        {
            var isValid = input.TokenName.Length <= TokenContractConstants.TokenNameLength
                          && input.Symbol.Length > 0
                          && input.Symbol.Length <= TokenContractConstants.SymbolMaxLength
                          && input.Decimals >= 0
                          && input.Decimals <= TokenContractConstants.MaxDecimals;
            Assert(isValid, "Invalid input.");
        }

        private void CheckCrossChainTokenContractRegistrationControllerAuthority()
        {
            if (State.CrossChainTokenContractRegistrationController.Value == null)
                State.CrossChainTokenContractRegistrationController.Value = GetCrossChainTokenContractRegistrationController();
            Assert(State.CrossChainTokenContractRegistrationController.Value.OwnerAddress == Context.Sender, "No permission.");
        }
    }
}