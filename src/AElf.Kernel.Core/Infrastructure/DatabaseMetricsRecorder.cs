using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.Infrastructure
{
    public enum CompressionType
    {
        Gzip,
        SevenZip
    }
    
    public interface IDatabaseMetricsRecorder
    {
        void Start();
        void EnqueueMetric(DatabaseCompressionRecord record);
    }
    
    public class DatabaseMetricsRecorder : IDatabaseMetricsRecorder
    {
        public ILogger<DatabaseMetricsRecorder> Logger { get; set; }
        
        private Timer _timer;
        private readonly object _dumpLock = new object();
        
        private readonly List<DatabaseCompressionRecord> _metrics;
        private readonly BlockingCollection<DatabaseCompressionRecord> _serializationMetrics;

        private bool _isStarted;

        public DatabaseMetricsRecorder()
        {
            _serializationMetrics = new BlockingCollection<DatabaseCompressionRecord>(new ConcurrentQueue<DatabaseCompressionRecord>());
            _metrics = new List<DatabaseCompressionRecord>();
            
        }
        
        public void Start()
        {
            _timer = new Timer( OnTimerTick, null, 60_000, 5_000);

            Task.Run(() =>
            {
                while (true)
                {
                    var metric = _serializationMetrics.Take();
                    lock (_dumpLock)
                    {
                        _metrics.Add(metric);
                    }
                }
            });
        }

        private void OnTimerTick(object state)
        {
            if (!_isStarted)
            {
                _isStarted = true;
                return;
            }
            
            Dump();
        }

        public void EnqueueMetric(DatabaseCompressionRecord record)
        {
            if (!_isStarted)
                return;

            _serializationMetrics.Add(record);
        }

        private void Dump()
        {
            lock (_dumpLock)
            {
                DumpStats(CompressionType.Gzip);
                DumpStats(CompressionType.SevenZip);
            }
        }

        private void DumpStats(CompressionType compressionType)
        {
            var stats = new List<CompressionStat>();
            var metrics = _metrics.Where(m => m.CompressionType == compressionType).ToList();
            var metricsByType = metrics.ToLookup(t => t.SerializedType);

            foreach (var type in metricsByType)
            {
                var compressionStat = new CompressionStat
                {
                    CompressionType = compressionType,
                    SerializedType = type.Key,
                    TotalGain = type.Sum(m => m.InitialSize - m.CompressedSize),
                    BiggestGain = type.Max(m => m.InitialSize - m.CompressedSize),
                    LowestGain = type.Min(m => m.InitialSize - m.CompressedSize),
                    AverageGain = type.Average(m => m.InitialSize - m.CompressedSize),
                    AverageCompressionDuration = (long)type.Average(m => m.CompressionDuration)
                };
                    
                stats.Add(compressionStat);
            }
            
            foreach (var stat in stats.OrderByDescending(c => c.AverageGain).ToList())
            {
                Logger.LogDebug($"[{stat.CompressionType} - {stat.SerializedType}] \t Gain range: {stat.LowestGain} - {stat.BiggestGain} \t -- Total saved: {stat.TotalGain} \t -- Average: {stat.AverageGain} \t -- compression time {stat.AverageCompressionDuration} ms");
            }
        }
    }

    /// <summary>
    /// For a given (compression type and data type) pair.
    /// </summary>
    public class CompressionStat
    {
        public CompressionType CompressionType { get; set; }
        public Type SerializedType { get; set; }
        
        public long TotalGain { get; set; }
        public long BiggestGain { get; set; }
        public long LowestGain { get; set; }
        public double AverageGain { get; set; }
        public long AverageCompressionDuration { get; set; }

        public CompressionStat()
        {
        }
    }

    public class DatabaseCompressionRecord
    {
        public DateTime RecordTime { get; set; }
        public CompressionType CompressionType { get; set; }
        public Type SerializedType { get; set; }
        
        public int InitialSize { get; set; }
        public int CompressedSize { get; set; }
        public long CompressionDuration { get; set; }
    }
}