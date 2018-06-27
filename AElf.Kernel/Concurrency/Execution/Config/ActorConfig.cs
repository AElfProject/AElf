using AElf.Configuration;

namespace AElf.Kernel.Concurrency.Execution.Config
{
    public class ActorConfig : ConfigBase<ActorConfig>
    {
        public bool IsCluster { get; set; }

        public string HostName { get; set; }

        public int Port { get; set; }

        public string HoconContent { get; set; }

        public ActorConfig()
        {
            IsCluster = false;
            HostName = "127.0.0.1";
            Port = 0;
            HoconContent = @"
                akka {
                    actor {
                        deployment {
                            /router {
                                router = tracked-group
                                routees.paths = [""/user/worker"",""/user/worker1"",""/user/worker2"",""/user/worker3""]
                            }                
                        }
                        router.type-mapping {
                           tracked-group = ""AElf.Kernel.Concurrency.Execution.TrackedGroup, AElf.Kernel""
                        }
                    }
                }";
            HoconContent = @"
                akka {
                    actor {
                        provider = cluster
                        deployment {
                            /router {
                                router = tracked-group
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
                        router.type-mapping {
                           tracked-group = ""AElf.Kernel.Concurrency.Execution.TrackedGroup, AElf.Kernel""
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
                            port = 0
                        }
                    }
                    cluster {
                        seed-nodes = [""akka.tcp://AElfSystem@127.0.0.1:32551""]
                        roles = [""manager""]
                    }
                }";
        }
    }
}