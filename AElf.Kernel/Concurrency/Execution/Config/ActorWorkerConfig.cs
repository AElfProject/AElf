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
                        debug {  
                          receive = on 
                          autoreceive = on
                          lifecycle = on
                          event-stream = on
                          unhandled = on
                        }
                    }
                    remote {
                        log-sent-messages = on
                        dot-netty.tcp {
                            hostname = ""127.0.0.1""
                            port = 32551
                        }
                    }
                    cluster {
                        seed-nodes = [""akka.tcp://AElfSystem@127.0.0.1:32551""]
                        roles = [""worker""]
                        log-info = on
                        debug {
                              verbose-heartbeat-logging = on
                              verbose-receive-gossip-logging = on
                            }
                    }
                }";
        }
    }
}