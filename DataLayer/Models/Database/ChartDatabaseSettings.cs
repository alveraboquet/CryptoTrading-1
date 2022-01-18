using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLayer
{
    public class ChartDatabaseSettings : IChartDatabaseSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        //public string UsersCollectionName { get; set; }
        public string PairInfoCollectionName { get; set; }
        public string StreamInfoCollectionName { get; set; }
    }
}
