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

        private async void OnTimerTick(object state)
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
            var stats = new List<CompressionStat>();
            lock (_dumpLock)
            {
                // seven zip
                var sevenZipMetrics = _metrics.Where(m => m.CompressionType == CompressionType.SevenZip).ToList();
                var sevenZipMetricsByType = sevenZipMetrics.ToLookup(t => t.SerializedType); //.ToDictionary(g => g.Key, g => g.ToList());

                foreach (var type in sevenZipMetricsByType)
                {
                    var sevenZipResult = new CompressionStat
                    {
                        CompressionType = CompressionType.SevenZip,
                        SerializedType = type.Key,
                        TotalGain = type.Sum(m => m.InitialSize - m.CompressedSize),
                        BiggestGain = type.Max(m => m.InitialSize - m.CompressedSize),
                        LowestGain = type.Min(m => m.InitialSize - m.CompressedSize),
                        AverageGain = type.Average(m => m.InitialSize - m.CompressedSize),
                        AverageCompressionDuration = (long)type.Average(m => m.CompressionDuration)
                    };
                    
                    stats.Add(sevenZipResult);
                }

                // gzip
//                var gzipMetrics = _metrics.Where(m => m.CompressionType == CompressionType.Gzip).ToList();
//                var gzipResult = new CompressionStat
//                {
//                
//                };
            }
            
            foreach (var sevenZipResult in stats.OrderByDescending(c => c.AverageGain).ToList())
            {
                Logger.LogDebug($"[{sevenZipResult.CompressionType} - {sevenZipResult.SerializedType}] \t Gain range: {sevenZipResult.LowestGain} - {sevenZipResult.BiggestGain} \t -- Total saved: {sevenZipResult.TotalGain} \t -- Average: {sevenZipResult.AverageGain} \t -- compression time {sevenZipResult.AverageCompressionDuration} ms");
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