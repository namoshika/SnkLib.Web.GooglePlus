using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public abstract class NotificationData
    {
        public NotificationData(JToken source, Uri plusBaseUrl) { ParseTemplate(source, plusBaseUrl); }
        public NotificationFlag Type { get; private set; }
        public string Id { get; private set; }
        public string RawNoticedDate { get; private set; }
        public DateTime NoticedDate { get; private set; }

        public static NotificationData Create(JToken source, Uri plusBaseUrl)
        {
            NotificationData data;
            switch((string)source[7])
            {
                case "gplus_stream":
                    data = new ContentNotificationData(source, plusBaseUrl);
                    break;
                case "gplus_circles":
                case "gplus_communities":
                    data = new ContactNotificationData(source, plusBaseUrl);
                    break;
                case "gplus_photos":
                    data = new PhotoNotificationData(source, plusBaseUrl);
                    break;
                case "gplus_hangout":
                    data = new HangoutNotificationData(source, plusBaseUrl);
                    break;
                default:
                    throw new NotImplementedException();
            }
            return data;
        }
        protected virtual void ParseTemplate(JToken source, Uri plusBaseUrl)
        {
            Id = (string)source[0];
            RawNoticedDate = ((ulong)source[9]).ToString();
            NoticedDate = ApiWrapper.GetDateTime((ulong)source[9] / 1000);

            List<Tuple<JToken, string>> itemPairs = new List<Tuple<JToken, string>>();
            foreach (var type in source[5].Select(item => (string)item[0]))
            {
                switch (type)
                {
                    case "CIRCLE_PERSONAL_ADD":
                        Type |= NotificationFlag.CircleIn;
                        break;
                    case "CIRCLE_RECIPROCATING_ADD":
                        Type |= NotificationFlag.CircleAddBack;
                        break;
                    case "STREAM_COMMENT_NEW":
                        Type |= NotificationFlag.Response;
                        break;
                    case "STREAM_COMMENT_FOLLOWUP":
                        Type |= NotificationFlag.Followup;
                        break;
                    case "STREAM_POST_AT_REPLY":
                    case "STREAM_COMMENT_AT_REPLY":
                        Type |= NotificationFlag.Mension;
                        break;
                    case "STREAM_PLUSONE_POST":
                    case "STREAM_PLUSONE_COMMENT":
                        Type |= NotificationFlag.PlusOne;
                        break;
                    case "HANGOUT_INVITE":
                        Type |= NotificationFlag.InviteHangout;
                        break;
                    case "SQUARE_INVITE":
                        Type |= NotificationFlag.InviteCommunitiy;
                        break;
                    case "PHOTOS_CAMERASYNC_UPLOADED":
                        Type |= NotificationFlag.CameraSyncUploaded;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
    [Flags]
    public enum NotificationFlag
    {
        CameraSyncUploaded = 0x00000001,
        CircleAddBack = 0x00000002,
        CircleIn = 0x00000004,
        Followup = 0x00000008,
        InviteHangout = 0x00000010,
        InviteCommunitiy = 0x00000020,
        Mension = 0x00000040,
        PlusOne = 0x00000080,
        Response = 0x00000100,
        TaggedImage = 0x00000200,
    }
}
