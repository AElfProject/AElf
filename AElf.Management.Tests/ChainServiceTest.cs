using System.Collections.Generic;
using AElf.Management.Models;
using AElf.Management.Services;
using Xunit;

namespace AElf.Management.Tests
{
    public class ChainServiceTest
    {
        private string chainId = "0x86c41f71da5f1fb193660f9267d083d77e6a";
        
        //[Fact(Skip = "require aws account")]
        [Fact]
        public void DeployTest()
        {
            var arg = new DeployArg();
            arg.MainChainAccount = "0x04b8b111fdbc2f5409a006339fa1758e1ed1";
            arg.AccountPassword = "123";
            arg.DBArg = new DeployDBArg();
            arg.ManagerArg=new DeployManagerArg();
            arg.ManagerArg.IsCluster = false;
            arg.WorkArg = new DeployWorkArg();
            arg.WorkArg.ActorCount = 4;
            arg.LauncherArg=new DeployLauncherArg();
            arg.LauncherArg.IsConsensusInfoGenerator = true;

            var service = new ChainService();
            service.DeployMainChain(chainId, arg);
        }
        
        [Fact]
        public void DeployNode2Test()
        {
            var arg = new DeployArg();
            arg.MainChainAccount = "0x0429c477d551aa91abc193d7088f69082000";
            arg.AccountPassword = "123";
            arg.DBArg = new DeployDBArg();
            arg.ManagerArg=new DeployManagerArg();
            arg.ManagerArg.IsCluster = false;
            arg.WorkArg = new DeployWorkArg();
            arg.WorkArg.ActorCount = 4;
            arg.LauncherArg=new DeployLauncherArg();
            arg.LauncherArg.IsConsensusInfoGenerator = false;
            arg.LauncherArg.Bootnodes=new List<string>{"af4328122a5ba11e8b7c80ac491fc1bb-219376992.us-west-2.elb.amazonaws.com:30800"};

            var service = new ChainService();
            service.DeployMainChain(chainId+"-2", arg);
        }
        
        [Fact]
        public void DeployNode3Test()
        {
            var arg = new DeployArg();
            arg.MainChainAccount = "0x04bce3e67ec4fbd0fad2822e6e5ed097812c";
            arg.AccountPassword = "123";
            arg.DBArg = new DeployDBArg();
            arg.ManagerArg=new DeployManagerArg();
            arg.ManagerArg.IsCluster = false;
            arg.WorkArg = new DeployWorkArg();
            arg.WorkArg.ActorCount = 4;
            arg.LauncherArg=new DeployLauncherArg();
            arg.LauncherArg.IsConsensusInfoGenerator = false;
            arg.LauncherArg.Bootnodes=new List<string>{"af4328122a5ba11e8b7c80ac491fc1bb-219376992.us-west-2.elb.amazonaws.com:30800"};

            var service = new ChainService();
            service.DeployMainChain(chainId+"-3", arg);
        }
        
        //[Fact(Skip = "require aws account")]
        [Fact]
        public void RemoveTest()
        {
            var service = new SideChainService();

            service.Remove(chainId);
        }
        
        [Fact]
        public void RemoveNode2Test()
        {
            var service = new SideChainService();

            service.Remove(chainId+"-2");
        }
        
        [Fact]
        public void RemoveNode3Test()
        {
            var service = new SideChainService();

            service.Remove(chainId+"-3");
        }
    }
}