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

            var detailDatas = source[4][1][1];
            var actors = new List<NotificationItemData>();
            foreach (var item in detailDatas
                .Select((item, idx) => new { Type = (string)source[5][idx][0], Detail = item }))
            {
                var detailJson = item.Detail[0][1][0];
                NotificationFlag type;
                switch ((string)item.Type)
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
                        throw new NotImplementedException();
                }
                actors.Add(new NotificationItemData(
                    new ProfileData((string)detailJson[1], (string)detailJson[2], ApiAccessorUtility.ConvertReplasableUrl((string)detailJson[0]), loadedApiTypes: ProfileUpdateApiFlag.Base),
                    type, ApiWrapper.GetDateTime((ulong)item.Detail[1] / 1000)));
            }
            LogItems = actors.ToArray();
        }
    }
}
