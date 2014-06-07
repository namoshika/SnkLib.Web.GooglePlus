using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    [Stubable]
    public class InitData : CoreData
    {
        public InitData(string atValue, string pvtValue, string ejxValue, string buildLevel,
            string lang, string afsid, CircleData[] circleInfos, ActivityData[] latestActivities)
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
        public readonly string AtValue;
        public readonly string PvtValue;
        public readonly string EjxValue;
        public readonly string BuildLevel;
        public readonly string Lang;
        public readonly string Afsid;
        public readonly CircleData[] CircleInfos;
        public readonly ActivityData[] LatestActivities;
    }
}
