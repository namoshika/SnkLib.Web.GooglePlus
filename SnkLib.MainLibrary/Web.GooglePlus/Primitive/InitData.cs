using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class InitData : DataBase
    {
        public InitData(string atValue, string pvtValue, string ejxValue, string buildLevel, string lang, string afsid, CircleData[] circleInfos, ActivityData[] latestActivities)
        {
            AtValue = atValue;
            PvtValue = pvtValue;
            EjxValue = ejxValue;
            BuildLevel = buildLevel;
            Lang = lang;
            Afsid = afsid;
            CircleInfos = circleInfos;
            LatestActivities = latestActivities;
        }
        public string AtValue { get; private set; }
        public string PvtValue { get; private set; }
        public string EjxValue { get; private set; }
        public string BuildLevel { get; private set; }
        public string Lang { get; private set; }
        public string Afsid { get; private set; }
        public CircleData[] CircleInfos { get; private set; }
        public ActivityData[] LatestActivities { get; private set; }
    }
}
