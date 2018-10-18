using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using AElf.Network.Connection;
using AElf.Network.Data;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Network.Peers
{
    public partial class Peer
    {
        private readonly Timer _pingPongTimer;
        
        /// <summary>
        /// List of currently pending ping requests.
        /// </summary>
        private readonly List<Ping> _pings = new List<Ping>();
        private object pingLock = new Object();
        
        private TimeSpan pingWaitTime = TimeSpan.FromSeconds(2);

        private int _droppedPings = 0;
        
        private void TimerTimeoutElapsed(object sender, ElapsedEventArgs e)
        {
            if (IsDisposed)
                return;

            lock (pingLock)
            {
                DateTime lowerThreshold = DateTime.Now - pingWaitTime;
                var pings = _pings.Where(p => p.Time.ToDateTime() < lowerThreshold).ToList();

                if (pings.Count > 0)
                {
                    _droppedPings += pings.Count;
                    _logger?.Trace($"{DistantNodeData} - Current failed count {_droppedPings}.");
                    
                    var peerStr = _pings.Select(c => c.Id).Aggregate((a, b) => a.ToString() + ", " + b);
                    
                    _logger?.Trace($"{DistantNodeData} - {pings.Count} pings where dropped [ {peerStr} ].");
                    
                    foreach (var p in pings)
                        _pings.Remove(p);
                }
            }
            
            // Create a new ping
            try
            {
                Guid id = Guid.NewGuid();
                Ping ping = new Ping { Id = id.ToString(), Time = Timestamp.FromDateTime(DateTime.UtcNow)};
            
                byte[] payload = ping.ToByteArray();
            
                var pingMsg = new Message { Type = (int)MessageType.Ping, Length = payload.Length, Payload = payload };

                lock (pingLock)
                {
                    _pings.Add(ping);
                }

                Task.Run(() => EnqueueOutgoing(pingMsg));
            }
            catch (Exception exception)
            {
                _logger?.Trace(exception, "Error while sending ping message.");
            }
        }

        private void HandlePingMessage(Message pingMsg)
        {
            try
            {
                Ping ping = Ping.Parser.ParseFrom(pingMsg.Payload);
                Pong pong = new Pong { Id = ping.Id, Time = Timestamp.FromDateTime(DateTime.UtcNow) };
                        
                byte[] payload = pong.ToByteArray();
            
                var pongMsg = new Message
                {
                    Type = (int)MessageType.Pong,
                    Length = payload.Length,
                    Payload = payload
                };

                EnqueueOutgoing(pongMsg);
            }
            catch (Exception e)
            {
                _logger?.Trace(e, $"Failed to process ping message from {DistantNodeData}.");
            }
        }

        private void HandlePongMessage(Message pongMsg)
        {
            try
            {
                Pong pong = Pong.Parser.ParseFrom(pongMsg.Payload);
                
                lock (pingLock)
                {
                    Ping ping = _pings.FirstOrDefault(p => p.Id == pong.Id);
                            
                    if (ping != null)
                        _pings.Remove(ping);
                    else
                        _logger?.Trace($"Could not match pong reply {pong.Id}.");
                }
            }
            catch (Exception e)
            {
                _logger?.Trace(e, $"Failed to handle pong message from {DistantNodeData}.");
            }
        }
    }
}