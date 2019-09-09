using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using AElf.Database;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using SevenZip;

namespace AElf.Kernel.Infrastructure
{
    public interface IStoreKeyPrefixProvider<T>
        where T : IMessage<T>, new()
    {
        string GetStoreKeyPrefix();
    }

    public class StoreKeyPrefixProvider<T> : IStoreKeyPrefixProvider<T>
        where T : IMessage<T>, new()
    {
        private static readonly string _typeName = typeof(T).Name;

        public string GetStoreKeyPrefix()
        {
            return _typeName;
        }
    }

    public class FastStoreKeyPrefixProvider<T> : IStoreKeyPrefixProvider<T>
        where T : IMessage<T>, new()
    {
        private readonly string _prefix;

        public FastStoreKeyPrefixProvider(string prefix)
        {
            _prefix = prefix;
        }

        public string GetStoreKeyPrefix()
        {
            return _prefix;
        }
    }


    public abstract class KeyValueStoreBase<TKeyValueDbContext, T> : IKeyValueStore<T>
        where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
        where T : class, IMessage<T>, new()
    {
        
        static int dictionary = 1 << 23;

        static bool eos = false;

        static readonly CoderPropID[] propIDs = 
        {
            CoderPropID.DictionarySize,
            CoderPropID.PosStateBits,
            CoderPropID.LitContextBits,
            CoderPropID.LitPosBits,
            CoderPropID.Algorithm,
            CoderPropID.NumFastBytes,
            CoderPropID.MatchFinder,
            CoderPropID.EndMarker
        };

        // these are the default properties, keeping it simple for now:
        readonly object[] properties = 
        {
            (System.Int32)(dictionary),
            (System.Int32)(2),
            (System.Int32)(3),
            (System.Int32)(0),
            (System.Int32)(2),
            (System.Int32)(128),
            "bt4",
            eos
        };
        
        private readonly TKeyValueDbContext _keyValueDbContext;
        private readonly IDatabaseMetricsRecorder _metricsRecorder;

        private readonly IKeyValueCollection _collection;

        private readonly MessageParser<T> _messageParser;
        
        public ILogger<TKeyValueDbContext> Logger { get; set; }

        public KeyValueStoreBase(TKeyValueDbContext keyValueDbContext, IStoreKeyPrefixProvider<T> prefixProvider, IDatabaseMetricsRecorder metricsRecorder)
        {
            _keyValueDbContext = keyValueDbContext;
            _metricsRecorder = metricsRecorder;
            _collection = keyValueDbContext.Collection(prefixProvider.GetStoreKeyPrefix());

            _messageParser = new MessageParser<T>(() => new T());
        }

        public async Task SetAsync(string key, T value)
        {
            var serialized = Serialize(value);
            
            /* dotnet */
            
            Stopwatch dotnetSw = Stopwatch.StartNew();
            var compressedDotnet = CompressDotnet(serialized);
            dotnetSw.Stop();
            
            _metricsRecorder.EnqueueMetric(new DatabaseCompressionRecord
            {
                RecordTime = DateTime.Now,
                CompressionType = CompressionType.Gzip,
                SerializedType = typeof(T),
                InitialSize = serialized.Length,
                CompressedSize = compressedDotnet.Length,
                CompressionDuration = dotnetSw.ElapsedMilliseconds,
            });

            /* End dotnet **/
            
            /* Seven zip (LZMA) */

            Stopwatch sw = Stopwatch.StartNew();
            var compressedSevenZip = Compress7Zip(serialized);
            sw.Stop();

            _metricsRecorder.EnqueueMetric(new DatabaseCompressionRecord
            {
                RecordTime = DateTime.Now,
                CompressionType = CompressionType.SevenZip,
                SerializedType = typeof(T),
                InitialSize = serialized.Length,
                CompressedSize = compressedSevenZip.Length,
                CompressionDuration = sw.ElapsedMilliseconds,
            });

            /* END LZMA */

            await _collection.SetAsync(key, serialized);
        }

        private static byte[] Serialize(T value)
        {
            return value?.ToByteArray();
        }

        public byte[] Compress7Zip(byte[] inputBytes)
        {
            byte[] retVal = null;
            SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();
            encoder.SetCoderProperties(propIDs, properties);

            using (MemoryStream strmInStream = new MemoryStream(inputBytes))
            {
                using (MemoryStream strmOutStream = new MemoryStream())
                {
                    encoder.WriteCoderProperties(strmOutStream);
                    long fileSize = strmInStream.Length;
                    for (int i = 0; i < 8; i++)
                        strmOutStream.WriteByte((byte)(fileSize >> (8 * i)));

                    encoder.Code(strmInStream, strmOutStream, -1, -1, null);
                    retVal = strmOutStream.ToArray();
                }
            }

            return retVal;
        }

        public static byte[] CompressDotnet(byte[] data)
        {
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                dstream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        public async Task SetAllAsync(Dictionary<string, T> pipelineSet)
        {
            await _collection.SetAllAsync(
                pipelineSet.ToDictionary(k => k.Key, v => Serialize(v.Value)));
        }

        public virtual async Task<T> GetAsync(string key)
        {
            var result = await _collection.GetAsync(key);

            return result == null ? default(T) : Deserialize(result);
        }

        private T Deserialize(byte[] result)
        {
            return _messageParser.ParseFrom(result);
        }

        public virtual async Task RemoveAsync(string key)
        {
            await _collection.RemoveAsync(key);
        }
    }
}