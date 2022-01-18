using ExchangeModels.BybitFutures;
using ExchangeServices.ExtensionMethods;
using log4net;

namespace ServerApplication.Bybit.Models
{
    public class BybitFuturesExtendedCandle : BybitFuturesCandle
    {
        private readonly ILog _logger;

        public BybitFuturesExtendedCandle()
        {
            _logger = LogManager.GetLogger(typeof(BybitFuturesExtendedCandle));
        }

        public BybitFuturesExtendedCandle(BybitFuturesCandle candle, string topic)
        {
            _logger = LogManager.GetLogger(typeof(BybitFuturesExtendedCandle));
            MapFromBybitCandle(candle);
            ExtractDataFromTopic(topic);
        }
        
        public string Symbol { get; set; }
        public string Timeframe { get; set; }
        public long StartAsMilliseconds => Start * 1000;

        private void MapFromBybitCandle(BybitFuturesCandle candle)
        {
            Start = candle.Start;
            End = candle.End;
            Open = candle.Open;
            Close = candle.Close;
            High = candle.High;
            Low = candle.Low;
            Volume = candle.Volume;
            Turnover = candle.Turnover;
            Confirm = candle.Confirm;
            CrossSeq = candle.CrossSeq;
            Timestamp = candle.Timestamp;
        }

        private void ExtractDataFromTopic(string topic)
        {
            string[] topicMembers = topic.Split('.');
            if (topicMembers.Length != 3)
            {
                _logger.Error($"Unknown topic detected. topic: {topic}");
                Symbol = "UNKNOWN";
                Timeframe = "UNKNOWN";
            }
            Symbol = topicMembers[2];
            Timeframe = topicMembers[1].ToStandardTimeframe();
        }
    }
}