using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Linq;

namespace AElf.Network.Sim.Node
{
    public class ProcNetNode : INetNode
    {
        public event EventHandler EventReceived;
        
        private readonly SimNodeConfiguration _conf;
        private Process _process;

        private NodeEventStream _nodeEventStream;

        public int Port
        {
            get
            {
                return _conf.ListeningPort;
            }
        }
        
        public ProcNetNode(SimNodeConfiguration conf)
        {
            _conf = conf;
        }
        
        public void Start()
        {
            var btndes = _conf?.Bootnodes != null && _conf.Bootnodes.Any() ? string.Join(' ', _conf.Bootnodes) : string.Empty;
                
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    Arguments = $"run --no-build -p ../../../../AElf.Network.Sim.Node/AElf.Network.Sim.Node.csproj {_conf.RpcPort} {_conf.ListeningPort} {btndes}",
                    UseShellExecute = false
                }
            };

            _process.Start();

            Thread.Sleep(TimeSpan.FromSeconds(2));
            
            _nodeEventStream = new NodeEventStream(_conf.RpcPort, "net");
            _nodeEventStream.EventReceived += NodeEventStreamOnEventReceived;
            _nodeEventStream.StartAsync();
        }

        private void NodeEventStreamOnEventReceived(object sender, EventArgs e)
        {
            EventReceived?.Invoke(this, e);
        }

        public void Stop()
        {
            _process.Close();
            _nodeEventStream?.StopAsync();
        }
    }
}