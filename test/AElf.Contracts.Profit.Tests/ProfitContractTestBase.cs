using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Contracts.Profit
{
    public class ProfitContractTestBase : ContractTestBase<ProfitContractTestAElfModule>
    {
        protected ECKeyPair StarterKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address Starter => Address.FromPublicKey(StarterKeyPair.PublicKey);
        protected Address TokenContractAddress { get; set; }
        protected Address ProfitContractAddress { get; set; }
        
        internal List<ProfitContractContainer.ProfitContractStub> Creators => CreatorMinerKeyPair
            .Select(p => GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, p)).ToList();

        internal List<ProfitContractContainer.ProfitContractStub> Normal => NormalMinerKeyPair
            .Select(p => GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, p)).ToList();
        
        protected List<ECKeyPair> CreatorMinerKeyPair => SampleECKeyPairs.KeyPairs.Skip(1).Take(4).ToList();
        
        protected List<ECKeyPair> NormalMinerKeyPair => SampleECKeyPairs.KeyPairs.Skip(5).Take(5).ToList();

        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }

        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }

        internal ProfitContractContainer.ProfitContractStub ProfitContractStub { get; set; }

        protected void InitializeContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(StarterKeyPair);
            
            ProfitContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ProfitContract).Assembly.Location)),
                        Name = ProfitSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateProfitInitializationCallList()
                    })).Output;
            ProfitContractStub = GetProfitContractTester(StarterKeyPair);

            //deploy token contract
            TokenContractAddress = AsyncHelper.RunSync(() => GetContractZeroTester(StarterKeyPair)
                .DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location)),
                        Name = TokenSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateTokenInitializationCallList()
                    })).Output;
            TokenContractStub = GetTokenContractTester(StarterKeyPair);
        }

        protected async Task CreateTreasury()
        {

        }

        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }

        internal ProfitContractContainer.ProfitContractStub GetProfitContractTester(ECKeyPair keyPair)
        {
            return GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, keyPair);
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateProfitInitializationCallList()
        {
            return new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateTokenInitializationCallList()
        {
            const string symbol = "ELF";
            var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            tokenContractCallList.Add(nameof(TokenContract.Create), new CreateInput
            {
                Symbol = symbol,
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = ProfitContractTestConsts.NativeTokenTotalSupply,
                Issuer = Starter,
                LockWhiteList =
                {
                    ProfitContractAddress
                }
            });

            // For creating `Treasury` profit item.
            tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
            {
                Symbol = symbol,
                Amount = (long) (ProfitContractTestConsts.NativeTokenTotalSupply * 0.2),
                To = Address.FromPublicKey(StarterKeyPair.PublicKey),
                Memo = "Issue token to default user for vote.",
            });

            CreatorMinerKeyPair.ForEach(creatorKeyPair => tokenContractCallList.Add(nameof(TokenContract.Issue),
                new IssueInput
                {
                    Symbol = symbol,
                    Amount = (long) (ProfitContractTestConsts.NativeTokenTotalSupply * 0.2),
                    To = Address.FromPublicKey(creatorKeyPair.PublicKey),
                    Memo = "set voters few amount for voting."
                }));

            return tokenContractCallList;
        }
    }
}