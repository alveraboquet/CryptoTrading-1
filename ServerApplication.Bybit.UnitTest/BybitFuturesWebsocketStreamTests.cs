using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataLayer;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using ServerApplication.Bybit.Queues;
using ServerApplication.Bybit.Queues.BybitFutures;
using ServerApplication.Bybit.StreamingServices.BybitFutures;
using ServerApplication.Bybit.UnitTest.Helpers;
using Utilities;
using Xunit;

namespace ServerApplication.Bybit.UnitTest
{
    public class BybitFuturesWebsocketStreamTests
    {
        private readonly Mock<IMemoryCache> _cache;
        private readonly IMemoryCache _realCache;
        private readonly BybitFuturesFrCalculateQueue _frCalcQueue;
        private readonly BybitFuturesLiquidationCalculateQueue _liqCalcQueue;
        private readonly BybitFuturesKlineMessageQueue _klineQueue;
        private readonly BybitFuturesTradeMessageQueue _tradeQueue;
        private readonly BybitFuturesZeroMqTradeQueue _zeroMqTradeQueue;
        private readonly BybitFuturesZeroMqDepthQueue _depthQueue;
        private readonly BybitFuturesRedisSavingDataQueue _redisQueue;

        public BybitFuturesWebsocketStreamTests()
        {
            _cache = new Mock<IMemoryCache>();
            _realCache = new MemoryCache(new MemoryCacheOptions());
            _frCalcQueue = new();
            _liqCalcQueue = new();
            _klineQueue = new();
            _tradeQueue = new();
            _zeroMqTradeQueue = new();
            _depthQueue = new();
            _redisQueue = new();
        }

        private List<PairInfo> GetTestPairs() => new List<PairInfo>
        {
            SymbolHelper.GetPair("BTCUSDT", ApplicationValues.BybitName),
            SymbolHelper.GetPair("ETHUSDT", ApplicationValues.BybitName)
        };

        [Fact]
        public async Task ShouldStreamFundingRate()
        {
            // Arrange
            var stream = new BybitFuturesFrStreaming(_realCache, _frCalcQueue);
            var pairs = GetTestPairs();
            
            // Act
            stream.Connect(pairs);
            await Task.Delay(10000);
            var frQueueData = _frCalcQueue.ToArray();

            // Assertion
            var hasUsdt = frQueueData.Any(e => e.Symbol.EndsWith("USDT"));
            var hasUsd = frQueueData.Any(e => e.Symbol.EndsWith("USD"));
            hasUsdt.Should().BeTrue();
            hasUsd.Should().BeTrue();
            _frCalcQueue.Count.Should().BeGreaterThan(0);
            frQueueData.Should().NotBeNullOrEmpty();
            foreach (var fundingRate in frQueueData)
            {
                fundingRate.Symbol.Should().NotBeNullOrEmpty();
            }
            
            _realCache.Dispose();
        }

        [Fact]
        public async Task ShouldStreamLiquidation()
        {
            // Arrange
            var stream = new BybitFuturesLiqStreaming(_realCache, _liqCalcQueue);
            var pairs = GetTestPairs();
            
            // Act
            stream.Connect(pairs);
            await Task.Delay(1800000);
            var liqQueueData = _liqCalcQueue.ToArray();
            
            // Assertion
            var usdtCount = liqQueueData.Count(e => e.Symbol.EndsWith("USDT"));
            var usdCount = liqQueueData.Count(e => e.Symbol.EndsWith("USD"));
            var othersCount = liqQueueData.Count(e => !e.Symbol.EndsWith("USD") && !e.Symbol.EndsWith("USDT"));
            usdtCount.Should().BeGreaterThan(0);
            usdCount.Should().BeGreaterThan(0);
            othersCount.Should().Be(0);
            liqQueueData.Should().NotBeNullOrEmpty();
            foreach (var liq in liqQueueData)
            {
                liq.Symbol.Should().NotBeNullOrEmpty();
            }
            
            _realCache.Dispose();
        }

        
        [Fact]
        public async Task ShouldStreamKlines()
        {
            // Arrange
            var stream = new BybitUsdtFuturesKlineStreaming(_realCache, _klineQueue);
            var pairs = GetTestPairs();
            
            // Act
            stream.Connect(pairs);
            await Task.Delay(10000);
            var klineQueueData = _klineQueue.ToArray();
            
            // Assertion
            var usdtCount = klineQueueData.Count(e => e.Symbol.EndsWith("USDT"));
            var usdCount = klineQueueData.Count(e => e.Symbol.EndsWith("USD"));
            usdtCount.Should().BeGreaterThan(0);
            usdCount.Should().BeGreaterThan(0);
            foreach (var kline in klineQueueData)
                kline.Symbol.Should().NotBeNullOrEmpty();
            
            _realCache.Dispose();
        }

        [Fact]
        public async Task ShouldStreamTrades()
        {
            // Arrange
            var stream = new BybitUsdtFuturesTradeStreaming(_realCache, _zeroMqTradeQueue, _tradeQueue);
            var pairs = GetTestPairs();
            
            // Act
            stream.Connect(pairs);
            await Task.Delay(10000);
            var tradeQueueData = _tradeQueue.ToArray();
            
            // Assertion
            var usdtCount = tradeQueueData.Count(e => e.Symbol.EndsWith("USDT"));
            var usdCount = tradeQueueData.Count(e => e.Symbol.EndsWith("USD"));
            usdtCount.Should().BeGreaterThan(0);
            usdCount.Should().BeGreaterThan(0);
            _tradeQueue.Count.Should().BeGreaterThan(0);
            tradeQueueData.Should().NotBeNullOrEmpty();
            foreach (var trade in tradeQueueData)
                trade.Symbol.Should().NotBeNullOrEmpty();
            
            _realCache.Dispose();
        }

        [Fact]
        public async Task ShouldStreamOrderbook()
        {
            // Arrange
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken cancellationToken = source.Token;
            var stream = new BybitUsdtFuturesDepthStreaming(_depthQueue,
                _redisQueue, _realCache, cancellationToken);
            var pairs = GetTestPairs();
            
            // Act
            stream.Connect(pairs);
            await Task.Delay(6000);
            var depthQueueData = _depthQueue.ToArray();
            var redisQueueData = _redisQueue.ToArray();
            source.Cancel();
            
            // Assertion
            var usdtCount = depthQueueData.Count(e => e.Symbol.EndsWith("USDT"));
            var usdCount = depthQueueData.Count(e => e.Symbol.EndsWith("USD"));
            var othersCount = depthQueueData.Count(e => !e.Symbol.EndsWith("USD") && !e.Symbol.EndsWith("USDT"));
            usdtCount.Should().BeGreaterThan(0);
            usdCount.Should().BeGreaterThan(0);
            othersCount.Should().Be(0);
            depthQueueData.Length.Should().Be(redisQueueData.Length);
            depthQueueData.Length.Should().Be(usdtCount + usdCount);
            foreach (var depth in depthQueueData)
                depth.Symbol.Should().NotBeNullOrEmpty();
            
            _realCache.Dispose();
        }
    }
}