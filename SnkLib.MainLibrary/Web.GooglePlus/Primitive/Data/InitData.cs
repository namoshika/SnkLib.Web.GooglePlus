using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class InitData : DataBase
    {
        public InitData(string atValue, string pvtValue, string ejxValue, CircleData[] circleInfos, ActivityData[] latestActivities)
        {
            AtValue = atValue;
            PvtValue = pvtValue;
            EjxValue = ejxValue;
            CircleInfos = circleInfos;
            LatestActivities = latestActivities;
        }
        public string AtValue { get; private set; }
        public string PvtValue { get; private set; }
        public string EjxValue { get; private set; }
        public CircleData[] CircleInfos { get; private set; }
        public ActivityData[] LatestActivities { get; private set; }
    }
}
