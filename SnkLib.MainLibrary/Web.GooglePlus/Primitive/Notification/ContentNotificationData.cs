using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class ContentNotificationData : SocialNotificationData
    {
        public ContentNotificationData(JToken source, Uri plusBaseUrl, NotificationFlag typeFlags) : base(source, plusBaseUrl, typeFlags) { }
        public ActivityData Target { get; private set; }

        protected override void ParseTemplate(JToken source, Uri plusBaseUrl, NotificationFlag typeFlags)
        {
            base.ParseTemplate(source, plusBaseUrl, typeFlags);

            //通知の発生地点となるActivityを抽出
            var activityJson = source[4][1][0];
            var activityId = (string)source[6].First(token => (int)token[0] == 1)[1];
            
            //コミュ新着通知の場合はActivity本体の情報がほとんど含まれない
            if (typeFlags.HasFlag(NotificationFlag.SubscriptionCommunitiy))
                Target = new ActivityData(activityId);
            else
            {
                var activityText = (string)activityJson[1];
                var profileJson = activityJson[3];
                var activityActor = new ProfileData(
                    (string)profileJson[1], (string)profileJson[2],
                    ApiAccessorUtility.ConvertReplasableUrl((string)profileJson[0]),
                    loadedApiTypes: ProfileUpdateApiFlag.Base);
                Target = new ActivityData(
                    activityId, null, activityText, null, status: PostStatusType.First, owner: activityActor,
                    updaterTypes: ActivityUpdateApiFlag.Notification);
            }

            //通知の発生に関わったメンバーを抽出
            var actorDict = source[4][1][1]
                .Select(item =>
                    {
                        var tmp = item[0][1][0];
                        return new ProfileData((string)tmp[1], (string)tmp[2], ApiAccessorUtility.ConvertReplasableUrl((string)tmp[0]), loadedApiTypes: ProfileUpdateApiFlag.Base);
                    })
                .GroupBy(dt => dt.Id)
                .Select(grp => grp.First())
                .ToDictionary(dt => dt.Id);
            var actors = new List<NotificationItemData>();
            foreach (var item in source[5].Select(item => (string)item[1]))
            {
                var datas = item.Split(':');
                if (actorDict.ContainsKey(datas[0]) == false)
                    continue;
                actors.Add(new NotificationItemData(actorDict[datas[0]], ConvertFlags(datas[1]), item));
            }
            LogItems = actors.ToArray();
        }
    }
}
