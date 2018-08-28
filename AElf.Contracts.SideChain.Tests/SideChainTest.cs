using System.Threading.Tasks;
using AElf.Kernel;
using AElf.SmartContract;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Contracts.SideChain.Tests
{
    [UseAutofacTestFramework]
    public class SideChainTest
    {
        private SideChainContractShim _contract;
        private MockSetup _mock;

        public SideChainTest(MockSetup mock)
        {
            _mock = mock;
            Init();
        }

        private void Init()
        {
            _contract = new SideChainContractShim(_mock, 
                new Hash(_mock.ChainId1.CalculateHashWith(SmartContractType.SideChainContract.ToString())).ToAccount());
        }

        // TODO: To fix
        [Fact(Skip = "")]
        public async Task Test()
        {
            /*var chainId = Hash.Generate();
            var lockedAddress = Hash.Generate().ToAccount();
            ulong lockedToken = 10000;
            // create new chain
            var bytes = await _contract.CreateSideChain(chainId, lockedAddress, lockedToken);
            Assert.Equal(chainId.GetHashBytes(), bytes);

            // check status
            var status = await _contract.GetChainStatus(chainId);
            Assert.Equal(1, status);

            var sn = await _contract.GetCurrentSideChainSerialNumber();
            Assert.Equal(1, (int) sn);

            var tokenAmount = await _contract.GetLockedToken(chainId);
            Assert.Equal(lockedToken, tokenAmount);

            var address = await _contract.GetLockedAddress(chainId);
            Assert.Equal(lockedAddress, address);
            
            // authorize the chain 
            await _contract.ApproveSideChain(chainId);
            Assert.True(_contract.TransactionContext.Trace.IsSuccessful());
            
            status = await _contract.GetChainStatus(chainId);
            Assert.Equal(2, status);
            
            // dispose 
            await _contract.DisposeSideChain(chainId);
            Assert.True(_contract.TransactionContext.Trace.IsSuccessful());
            
            status = await _contract.GetChainStatus(chainId);
            Assert.Equal(3, status);*/
        }
    }
}