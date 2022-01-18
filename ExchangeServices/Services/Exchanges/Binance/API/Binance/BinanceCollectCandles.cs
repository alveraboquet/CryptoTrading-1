using System.Linq;
using Utilities;
using Binance.Net.Enums;
using DatabaseRepository;
using DataLayer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using ExchangeServices.ExtensionMethods;
using MongoDB.Driver;
using DataLayer.Models;

namespace ExchangeServices
{
    public class BinanceCollectCandles : IBinanceCollectCandles
    {
        private readonly ICandleService _candleRepo;
        private readonly IPairInfoRepository _pairsRepo;
        private readonly IPairStreamInfoRepository _pairStreamRepo;
        private readonly IBinanceServices _api;
        private readonly string exchange = ApplicationValues.BinanceName;

        public BinanceCollectCandles(ICandleService candleRepo, IPairInfoRepository pairsRepo, IBinanceServices api,
            IPairStreamInfoRepository pairStreamRepo)
        {
            this._api = api;
            this._pairStreamRepo = pairStreamRepo;
            this._pairsRepo = pairsRepo;
            this._candleRepo = candleRepo;
        }

        /// <exception cref="BinanceTooManyRequestException"/>
        public async Task<IEnumerable<ResCandle>> CollectCandleIfDoesNotExist(string symbol, KlineInterval interval, 
            long start, long end, long startRecorded, PairInfo pair)
        {
            string strTimeFrame = interval.ToStringFormat();

            if (pair == default)
                throw new Exception("Invalid or onavailable symbol.");

            var timeFrameOption = pair.TimeFrameOptions.FirstOrDefault(c => c.TimeFrame == strTimeFrame);

            if (timeFrameOption == default)
                throw new Exception("This time frame is not avaliable for this symbol");

            // correct the 'start'
            if (!timeFrameOption.StartTimeMax.HasValue)
            {
                try
                {
                    var startRes = await pair.TryGetBinanceStartTimeMax(strTimeFrame, _api);
                    timeFrameOption.StartTimeMax = startRes.startTimeMax;
                }
                catch (BinanceTooManyRequestException)
                { throw; }
                _pairsRepo.Update(pair.PairId, pair);
            }
            start = Math.Max(start, timeFrameOption.StartTimeMax.Value);
            start = Math.Min(start, DateTime.UtcNow.ToUnixTimestamp());

            List<ResCandle> candles = new List<ResCandle>();

            #region get stop/start of server reports

            var streamInfo = await _pairStreamRepo.GetOne(exchange, symbol, strTimeFrame);

            // is there any stop/start of ServerApplication for this symbol
            if (streamInfo != null && streamInfo.StreamReports != null && streamInfo.StreamReports.Any())
            {
                // is there any stop/start of ServerApplication for this start/end
                var reports = streamInfo.StreamReports.Where(r =>
                            start <= r.Stop && r.Stop <= end
                                ||
                            start <= r.Start && r.Start <= end).ToList();

                if (reports.Any())
                {
                    foreach (var report in reports)
                    {
                        while (report.Stop < report.Start)
                        {
                            try
                            {
                                Console.WriteLine("doing api of stop/start");
                                var res = await _api.GetCandelsAsync(symbol, interval, report.Stop.UnixTimeStampToDateTime(), null);
                                if (res == null)
                                    continue;

                                // stop/start range contains no candle for this timeframe
                                if (!res.Any())
                                    break;

                                report.Stop = res.Max(c => c.OpenTime) + 2000;

                                if (report.Stop > report.Start) // this was the last api call
                                {
                                    if (!res.Any())
                                        break;

                                    candles.AddRange(res.Select(c => new ResCandle()
                                    {
                                        OpenTime = c.OpenTime,
                                        Open = c.OpenPrice,
                                        High = c.HighPrice,
                                        Low = c.LowPrice,
                                        Close = c.ClosePrice,
                                        Volume = c.Volume
                                    }));
                                
                                    await _candleRepo.CreateMany(exchange, symbol, strTimeFrame, res);
                                }
                                else // this was not last api call
                                {
                                    candles.AddRange(res.Select(c => new ResCandle()
                                    {
                                        OpenTime = c.OpenTime,
                                        Open = c.OpenPrice,
                                        High = c.HighPrice,
                                        Low = c.LowPrice,
                                        Close = c.ClosePrice,
                                        Volume = c.Volume
                                    }));
                                    await _candleRepo.CreateMany(exchange, symbol, strTimeFrame, res);
                                }
                            }
                            catch (BinanceTooManyRequestException)
                            {
                                await _pairStreamRepo.Update(streamInfo.Id, streamInfo);
                                throw;
                            }
                            catch (MongoBulkWriteException) { }
                        }
                        streamInfo.StreamReports.Remove(report);
                    }
                    await _pairStreamRepo.Update(streamInfo.Id, streamInfo);
                }
            }

            #endregion

            #region get old candles from binance

            if (candles.Any())
                start = Math.Min(start, candles.Max(c => c.OpenTime));

            var ms = timeFrameOption.GetMiliseconds();

            Console.Write($"{startRecorded}");
            if (startRecorded == 0)
            {
                startRecorded = DateTime.UtcNow.ToUnixTimestamp();
                Console.WriteLine($" | {startRecorded}");
            }

            if (startRecorded > start + ms)
            {
                var ApiRes = new List<Candle>();
                while (startRecorded > start)
                {
                    try
                    {
                        Console.WriteLine("doing old api");
                        var res = await _api.GetCandelsAsync(symbol, interval, null, (startRecorded - ms).UnixTimeStampToDateTime(), 1000);
                        if (res == null)
                            continue;
                        ApiRes.AddRange(res);
                        startRecorded = res.Min(c => c.OpenTime);
                    }
                    catch (BinanceTooManyRequestException)
                    { throw; }
                }

                // add new candles from api
                if (ApiRes.Any())
                {
                    try
                    {
                        await _candleRepo.CreateMany(exchange, symbol, strTimeFrame, ApiRes);
                    }
                    catch (MongoBulkWriteException)
                    {  }
                }

                candles.AddRange(ApiRes.Select(c => new ResCandle()
                {
                    OpenTime = c.OpenTime,
                    Open = c.OpenPrice,
                    High = c.HighPrice,
                    Low = c.LowPrice,
                    Close = c.ClosePrice,
                    Volume = c.Volume
                }));
            }

            #endregion

            return candles;
        }
    }
}