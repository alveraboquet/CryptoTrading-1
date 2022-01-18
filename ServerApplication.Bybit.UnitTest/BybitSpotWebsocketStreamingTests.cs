using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataLayer;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using ServerApplication.Bybit.Queues;
using ServerApplication.Bybit.StreamingServices;
using ServerApplication.Bybit.UnitTest.Helpers;
using Utilities;
using Xunit;

namespace ServerApplication.Bybit.UnitTest
{
    public class BybitSpotWebsocketStreamingTests
    {
        private readonly BybitKlineMessageQueue _klineQueue;
        private readonly BybitTradeMessageQueue _tradeQueue;
        private readonly BybitZeroMQTradeQueue _zeroMqTradeQueue;
        private readonly BybitZeroMQDepthQueue _depthQueue;
        private readonly BybitRedisSavingDataQueue _redisQueue;
        private readonly Mock<IMemoryCache> _cache;
        private readonly IMemoryCache _realCache;

        public BybitSpotWebsocketStreamingTests()
        {
            _cache = new Mock<IMemoryCache>();
            _realCache = new MemoryCache(new MemoryCacheOptions());
            _klineQueue = new BybitKlineMessageQueue();
            _tradeQueue = new BybitTradeMessageQueue();
            _zeroMqTradeQueue = new BybitZeroMQTradeQueue();
            _depthQueue = new BybitZeroMQDepthQueue();
            _redisQueue = new BybitRedisSavingDataQueue();
        }

        [Fact]
        public async Task ShouldStreamBybitSpotKline()
        {
            // Arrange
            var stream = new BybitKlineStreaming(_cache.Object, _klineQueue);
            var pairs = new List<PairInfo>
            {
                SymbolHelper.GetPair("BTCUSDT", ApplicationValues.BybitName),
                SymbolHelper.GetPair("ETHUSDT", ApplicationValues.BybitName)
            };
            
            // Act
            stream.Connect(pairs);
            await Task.Delay(8000);
            var receivedKlines = _klineQueue.ToArray();
            
            // Assertion
            _klineQueue.Count.Should().BeGreaterThan(0);
            receivedKlines.Should().NotBeNullOrEmpty();
            foreach (var kline in receivedKlines)
            {
                kline.Candle.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task ShouldStreamBybitSpotTrades()
        {
            // Arrange
            var stream = new BybitTradeStreaming(_cache.Object, _tradeQueue, _zeroMqTradeQueue);
            var pairs = new List<PairInfo>
            {
                SymbolHelper.GetPair("BTCUSDT", ApplicationValues.BybitName),
                SymbolHelper.GetPair("ETHUSDT", ApplicationValues.BybitName)
            };
            
            // Act
            stream.Connect(pairs);
            await Task.Delay(40000);
            var receivedTrades = _tradeQueue.ToArray();
            var publishedTrades = _zeroMqTradeQueue.ToArray();
            
            // Assertion
            _tradeQueue.Count.Should().BeGreaterThan(0);
            receivedTrades.Should().NotBeNullOrEmpty();
            foreach (var trade in receivedTrades)
                trade.Should().NotBeNull();

            _zeroMqTradeQueue.Should().NotBeNullOrEmpty();
            publishedTrades.Should().NotBeNullOrEmpty();
            foreach (var trade in publishedTrades)
                trade.Should().NotBeNull();
            
            receivedTrades.Length.Should().Be(publishedTrades.Length);
        }

        [Fact]
        public async Task ShouldStreamBybitSpotDepth()
        {
            // Arrange
            var stream = new BybitDepthStreaming(new CancellationToken(), _depthQueue, _redisQueue, _realCache);
            var pairs = new List<PairInfo>
            {
                SymbolHelper.GetPair("BTCUSDT", ApplicationValues.BybitName),
                SymbolHelper.GetPair("ETHUSDT", ApplicationValues.BybitName)
            };
            
            // Act
            stream.Connect(pairs);
            await Task.Delay(8000);
            var depthsQueueData = _depthQueue.ToArray();
            var redisQueueData = _redisQueue.ToArray();
            
            _depthQueue.Count.Should().BeGreaterThan(0);
            depthsQueueData.Should().NotBeNullOrEmpty();
            foreach (var depth in depthsQueueData)
            {
                depth.Symbol.Should().NotBeNullOrEmpty();
                depth.Asks.Should().NotBeNull();
                depth.Bids.Should().NotBeNull();
            }

            _redisQueue.Count.Should().BeGreaterThan(0);
            redisQueueData.Should().NotBeNullOrEmpty();
            foreach (var data in redisQueueData)
                data.Should().NotBeNullOrEmpty().And.StartWith("o:").And.EndWith("USDT");
            
            depthsQueueData.Length.Should().Be(redisQueueData.Length);
            _realCache.Dispose();
        }
    }
}