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
        public ContentNotificationData(JToken source, Uri plusBaseUrl) : base(source, plusBaseUrl) { }
        public ActivityData Target { get; private set; }

        protected override void ParseTemplate(JToken source, Uri plusBaseUrl)
        {
            base.ParseTemplate(source, plusBaseUrl);

            //通知の発生地点となるActivityを抽出
            var activityJson = source[4][1][0];
            var activityId = (string)source[6].First(token => (int)token[0] == 1)[1];
            var activityText = (string)activityJson[1];
            var profileJson = activityJson[3];
            var activityActor = new ProfileData(
                (string)profileJson[1], (string)profileJson[2],
                ApiAccessorUtility.ConvertReplasableUrl((string)profileJson[0]),
                loadedApiTypes: ProfileUpdateApiFlag.Base);
            Target = new ActivityData(
                activityId, null, activityText, null, status: PostStatusType.First, owner: activityActor,
                updaterTypes: ActivityUpdateApiFlag.Notification);

            //通知の発生に関わったメンバーを抽出
            var actorDict = source[4][0][0][1]
                .Select(item => new ProfileData((string)item[1], (string)item[2], ApiAccessorUtility.ConvertReplasableUrl((string)item[0]), loadedApiTypes: ProfileUpdateApiFlag.Base))
                .ToDictionary(item => item.Id);
            var actors = new List<NotificationItemData>();
            foreach (var item in source[5].Select(item => (string)item[1]))
            {
                var datas = item.Split(':');
                NotificationFlag type;
                switch (datas[1])
                {
                    case "STREAM_COMMENT_NEW":
                        type = NotificationFlag.Response;
                        break;
                    case "STREAM_COMMENT_FOLLOWUP":
                        type = NotificationFlag.Followup;
                        break;
                    case "STREAM_POST_AT_REPLY":
                    case "STREAM_COMMENT_AT_REPLY":
                        type = NotificationFlag.Mension;
                        break;
                    case "STREAM_PLUSONE_POST":
                    case "STREAM_PLUSONE_COMMENT":
                        type = NotificationFlag.PlusOne;
                        break;
                    case "STREAM_POST_SHARED":
                        type = NotificationFlag.DirectMessage;
                        break;
                    case "STREAM_RESHARE":
                        type = NotificationFlag.Reshare;
                        break;
                    default:
                        throw new InvalidDataException("未知の通知データが入力されました。", source, null);
                }
                actors.Add(new NotificationItemData(actorDict[datas[0]], type, item));
            }
            LogItems = actors.ToArray();
        }
    }
}
