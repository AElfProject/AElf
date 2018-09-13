using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using AElf.Common.Application;
using AElf.Cryptography;
using AElf.Management.Models;
using AElf.Management.Services;
using Xunit;

namespace AElf.Management.Tests
{
    public class SideChainServiceTests
    {
        private string _chainId = "0x462fef4710a75e132e0c8bacfd5ee70213c0";//Guid.NewGuid().ToString("N");
        
        //[Fact(Skip = "require aws account")]
        [Fact]
        public void DeployTest()
        {
            var password = "123";
            
            var arg = new DeployArg();
            arg.MainChainId = _chainId;
            arg.AccountPassword = password;
            arg.DBArg = new DeployDBArg();
            arg.LighthouseArg=new DeployLighthouseArg();
            arg.LighthouseArg.IsCluster = false;
            arg.WorkArg = new DeployWorkArg();
            arg.LauncherArg=new DeployLauncherArg();
            arg.LauncherArg.IsConsensusInfoGenerator = true;

            var service = new SideChainService();
            service.Deploy(arg);
        }
        
        [Fact(Skip = "require aws account")]
        //[Fact]
        public void RemoveTest()
        {
            var service = new SideChainService();

            service.Remove(_chainId);
        }
        
        private List<string> CreateAccount(int num,string password)
        {
            var result =new List<string>();
            for (var i = 0; i < num; i++)
            {
                var keyStore = new AElfKeyStore(ApplicationHelpers.GetDefaultDataDir());
                var key = keyStore.Create(password);
                var account = key.GetAddressHex();
                result.Add(account);
            }

            return result;
        }
    }
}