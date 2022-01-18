using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseRepository
{
    internal static class CollectionNameProvider
    {
        private const string _candlesFormat = "Candles.{0}.{1}.{2}";  // ex.symbol.timeFrame
        private const string _candlesExSy = "Candles.{0}.{1}";  // ex.symbol

        /// <summary>
        /// provides the collection name using arguments, exchange.symbol.timeframe format
        /// </summary>
        public static string Candles(string exchange, string symbol, string timeFrame)
        {
            return string.Format(_candlesFormat, exchange, symbol, timeFrame);
        }

        /// <summary>
        /// provides the collection name using arguments, exchange.symbol format
        /// </summary>
        public static string CandleExSy(string exchange, string symbol)
        {
            return string.Format(_candlesExSy, exchange, symbol);
        }
    }
}
