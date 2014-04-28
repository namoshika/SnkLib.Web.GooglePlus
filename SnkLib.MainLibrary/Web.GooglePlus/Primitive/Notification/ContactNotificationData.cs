using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class ContactNotificationData : SocialNotificationData
    {
        public ContactNotificationData(JToken source, Uri plusBaseUrl, NotificationFlag typeFlags) : base(source, plusBaseUrl, typeFlags) { }
        protected override void ParseTemplate(JToken source, Uri plusBaseUrl, NotificationFlag typeFlags)
        {
            base.ParseTemplate(source, plusBaseUrl, typeFlags);
            var details = new List<NotificationItemData>();
            switch (Type == NotificationFlag.CircleAddBack ? 1
                : Type == NotificationFlag.InviteCommunitiy ? 0
                : (int)source[2])
            {
                case 0:
                case 2:
                    var detailDatas = source[4][1][1];
                    foreach (var item in detailDatas
                        .Select((item, idx) => new { Type = (string)source[5][idx][0], Detail = item }))
                    {
                        var tmpJson = item.Detail[0][1][0];
                        details.Add(new NotificationItemData(
                            new ProfileData((string)tmpJson[1], (string)tmpJson[2], ApiAccessorUtility.ConvertReplasableUrl((string)tmpJson[0]), AccountStatus.Active, loadedApiTypes: ProfileUpdateApiFlag.Base), Type, (string)tmpJson[1]));
                    }
                    break;
                case 1:
                    {
                        var tmpJson = source[4][1][0][3];
                        details.Add(new NotificationItemData(
                            new ProfileData((string)tmpJson[1], (string)tmpJson[2], ApiAccessorUtility.ConvertReplasableUrl((string)tmpJson[0]), AccountStatus.Active, loadedApiTypes: ProfileUpdateApiFlag.Base), Type, (string)tmpJson[1]));
                        break;
                    }
                default:
                    System.Diagnostics.Debug.WriteLine("通知JSONで未確認の値を確認(source[2]: {0})。", (int)source[2]);
                    break;
            }

            LogItems = details.ToArray();
        }
    }
}
