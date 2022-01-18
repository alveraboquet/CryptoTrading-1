using System;
using System.Collections.Generic;
using System.Text;

namespace DataLayer
{
    public interface IChartDatabaseSettings
    {
        string ConnectionString { get; set; }
        //string UsersCollectionName { get; set; }
        string PairInfoCollectionName { get; set; }
        string DatabaseName { get; set; }
        string StreamInfoCollectionName { get; set; }
    }
}
