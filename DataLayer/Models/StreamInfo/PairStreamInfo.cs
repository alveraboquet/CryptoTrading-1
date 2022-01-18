using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    /// <summary>
    /// Reports for when server application stoped/started (needed in API)
    /// </summary>
    public class PairStreamInfo
    {
        public PairStreamInfo()
        {}
        public PairStreamInfo(string exchange, string symbol, string timeframe)
        {
            this.Exchange = exchange;
            this.Symbol = symbol;
            this.TimeFrame = timeframe;
        }
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Exchange { get; set; }
        public string Symbol { get; set; }
        public string TimeFrame { get; set; }
        public List<StreamReport> StreamReports { get; set; }

        public void AddReport(long stop, long start)
        {
            if (StreamReports == null)
                StreamReports = new List<StreamReport>();

            var report = this.StreamReports.FirstOrDefault(r => r.Stop == stop);

            if (report == default)
                this.StreamReports.Add(new StreamReport()
                {
                    Stop = stop,
                    Start = start
                });
            else
                report.Start = start;
        }
    }

    public class StreamReport
    {
        public long Stop { get; set; }
        public long Start { get; set; }
    }
}
