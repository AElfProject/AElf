using AElf.Configuration;

namespace AElf.Kernel.Concurrency.Execution.Config
{
    public class ActorWorkerConfig : ConfigBase<ActorWorkerConfig>
    {
        public bool IsSeedNode { get; set; }
        
        public string HostName { get; set; }

        public int Port { get; set; }

        public string HoconContent { get; set; }

        public ActorWorkerConfig()
        {
            IsSeedNode = false;
            HostName = "127.0.0.1";
            Port = 32551;
            HoconContent = @"
               akka {
                    actor {
                        provider = cluster
                        serializers {
                          hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                        }
                        serialization-bindings {
                          ""System.Object"" = hyperion
                        }
                    }
                    remote {
                        maximum-payload-bytes = 30000000 bytes
                        dot-netty.tcp {
                            hostname = ""127.0.0.1""
                            port = 32551
                            message-frame-size =  30000000b
                            send-buffer-size =  30000000b
                            receive-buffer-size =  30000000b
                            maximum-frame-size = 30000000b
                        }
                    }
                    cluster {
                        seed-nodes = [""akka.tcp://AElfSystem@127.0.0.1:32551""]
                        roles = [""worker""]
                    }
                }";
        }
    }
}