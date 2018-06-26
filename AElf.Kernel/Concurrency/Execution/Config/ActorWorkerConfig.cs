using AElf.Configuration;

namespace AElf.Kernel.Concurrency.Execution.Config
{
    public class ActorWorkerConfig : ConfigBase<ActorWorkerConfig>
    {
        public bool IsSeedNode { get; set; }

        public string HoconContent { get; set; }

        public ActorWorkerConfig()
        {
            IsSeedNode = false;
            
            HoconContent = @"
               akka {
                    actor {
                        provider = cluster
                        debug {  
                          receive = on 
                          autoreceive = on
                          lifecycle = on
                          event-stream = on
                          unhandled = on
                        }
                        serializers {
                          hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                        }
                        serialization-bindings {
                          ""System.Object"" = hyperion
                        }
                    }
                    remote {
                        dot-netty.tcp {
                            hostname = ""127.0.0.1""
                            port = 32551
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