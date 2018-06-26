using System.Collections.Generic;
using AElf.Configuration;

namespace AElf.Kernel.Concurrency.Execution.Config
{
    public class ActorConfig : ConfigBase<ActorConfig>
    {
        public bool IsCluster { get; set; }

        public string HoconContent { get; set; }

        public List<string> WorkerNames { get; set; }

        public ActorConfig()
        {
            IsCluster = true;
            HoconContent = @"
                akka {
                    actor {
                        provider = cluster
                        deployment {
                            /router {
                                router = consistent-hashing-group
                                routees.paths = [""/user/worker""]
                                virtual-nodes-factor = 8
                                cluster {
                                    enabled = on
                                    max-nr-of-instances-per-node = 1
                                    allow-local-routees = off
                                    use-role = worker
                                }
                            }                
                        }
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
                        dot-netty.tcp {
                            hostname = ""127.0.0.1""
                            port = 0
                        }
                    }
                    cluster {
                        seed-nodes = [""akka.tcp://AElfSystem@127.0.0.1:32551""]
                        roles = [""manager""]
                    }
                }";

            WorkerNames = new List<string>();
            for (var i = 1; i <= 10; i++)
            {
                WorkerNames.Add("worker" + i);
            }
        }
    }
}