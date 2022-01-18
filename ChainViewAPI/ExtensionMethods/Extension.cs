using Binance.Net.Enums;
using ChainViewAPI.Services;
using DatabaseRepository;
using ExchangeServices;
using ExchangeServices.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace ChainViewAPI
{
    public static class Extension
    {
        public static string SymbolListResponseMessage(this List<DataLayer.PairInfo> pairsList)
        {
            StringBuilder json = new StringBuilder();
            json.Append('[');
            int c = pairsList.Count;
            for (int i = 0; i < c; i++)
            {
                var pair = pairsList[i];
                json.Append($"[\"{pair.Exchange}:{pair.Symbol}:{pair.IsListed.ToInt()}\"],");
            }
            if (pairsList.Any())
                json = json.Remove(json.Length - 1, 1);
            json.Append(']');
            return json.ToString();
        }

        public static string SymbolSearchResponseMessage(this IEnumerable<DataLayer.PairInfo> pairs)
        {
            var json = new StringBuilder();
            json.Append('[');
            foreach (var p in pairs)
            {
                json.Append($"[\"{p.Symbol}\",\"{p.Exchange}\",{p.QuoteAssetPrecision}");
                json.Append($",{p.IsAvailableFootprint.ToInt()},{p.IsAvailableHeatmap.ToInt()},{p.IsLinechart.ToInt()},");
                json.Append($"{p.IsAvailableVolume.ToInt()},{p.IsListed.ToInt()}],");
            }
            if (pairs.Any())
                json = json.Remove(json.Length - 1, 1);
            json.Append(']');

            return json.ToString();
        }

        /// <exception cref="BinanceTooManyRequestException"/>
        public static async Task<string> SymbolInfoResponseMessage(this DataLayer.PairInfo p,
            SymbolsStartAndEndTimeProvider getStart_End, IPairInfoRepository pairinfoRepo)
        {
            StringBuilder json = new StringBuilder();
            try
            {
                json.Append($"[\"{p.Symbol}\",\"{p.Exchange}\",{await CreateTimeframe(p.TimeFrameOptions)},{p.QuoteAssetPrecision}");
                json.Append($",{p.IsAvailableFootprint.ToInt()},{p.IsAvailableHeatmap.ToInt()},{p.IsLinechart.ToInt()},");
                json.Append($"{p.IsAvailableVolume.ToInt()},{p.IsListed.ToInt()}]");
            }
            catch (BinanceTooManyRequestException)
            { throw; }

            return json.ToString();

            async Task<string> CreateTimeframe(List<DataLayer.Models.TimeFrameOption> tO)
            {
                StringBuilder json = new StringBuilder();
                bool hasChanged = false;

                json.Append('[');
                try
                {
                    foreach (var option in tO)
                    {
                        var startRes = await getStart_End.GetStartTimeMax(p, option.TimeFrame);
                        bool startB = startRes.hasChanged;
                        option.StartTimeMax = startRes.startTimeMax;

                        var endRes = await getStart_End.TryGetEndTimeMax(p, option.TimeFrame);
                        bool endB = endRes.hasChanged;
                        option.EndTimeMax = endRes.endTimeMax;

                        hasChanged = hasChanged || endB || startB;

                        string endTimeMax = option.EndTimeMax.HasValue ? option.EndTimeMax.ToString() : "null";
                        json.Append($"[\"{option.TimeFrame}\",{option.StartTimeMax},{endTimeMax}],");
                    }
                }
                catch (BinanceTooManyRequestException)
                { throw; }
                finally
                {
                    if (hasChanged)
                        pairinfoRepo.Update(p.PairId, p);
                }

                json = json.Remove(json.Length - 1, 1);
                json.Append(']');
                return json.ToString();
            }
        }
    }
}
