using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class HangoutNotificationData : NotificationData
    {
        public HangoutNotificationData(JToken source, Uri plusBaseUrl, NotificationFlag typeFlags) : base(source, plusBaseUrl, typeFlags) { }
        public Uri LinkUrl { get; private set; }
        public ProfileData Actor { get; private set; }
        protected override void ParseTemplate(JToken source, Uri plusBaseUrl, NotificationFlag typeFlags)
        {
            base.ParseTemplate(source, plusBaseUrl, typeFlags);
            var tmp = source[4][0];
            LinkUrl = new Uri((string)tmp[2][2]);
            tmp = tmp[0][1][0];
            Actor = new ProfileData(
                (string)tmp[1], (string)tmp[2], ApiAccessorUtility.ConvertReplasableUrl((string)tmp[0]),
                AccountStatus.Active, loadedApiTypes: ProfileUpdateApiFlag.Base);
        }
    }
}
