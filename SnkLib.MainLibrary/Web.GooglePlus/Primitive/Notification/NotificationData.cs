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
        public NotificationData(JToken source, Uri plusBaseUrl, NotificationFlag typeFlags) { ParseTemplate(source, plusBaseUrl, typeFlags); }
        public NotificationFlag Type { get; private set; }
        public string Id { get; private set; }
        public string RawNoticedDate { get; private set; }
        public string Title { get; private set; }
        public string Summary { get; private set; }
        public DateTime NoticedDate { get; private set; }
        protected virtual void ParseTemplate(JToken source, Uri plusBaseUrl, NotificationFlag typeFlags)
        {
            Id = (string)source[0];
            Title = (string)source[4][0][0][2];
            Summary = (string)source[4][0][0][3];
            RawNoticedDate = ((ulong)source[9]).ToString();
            NoticedDate = ApiWrapper.GetDateTime((ulong)source[9] / 1000);
            Type = typeFlags;
        }

        public static NotificationData Create(JToken source, Uri plusBaseUrl)
        {
            NotificationFlag type = default(NotificationFlag);
            foreach (string typeTxt in source[8])
                type |= ConvertFlags(typeTxt);

            NotificationData data;
            switch((string)source[7])
            {
                case "gplus_circles":
                    data = new ContactNotificationData(source, plusBaseUrl, type);
                    break;
                case "gplus_communities":
                    //招待通知であり、それ以外のフラグが立っていない場合、
                    //招待通知ではなく、購読フラグが立っている場合、
                    //これらに該当しないデータが存在している場合は完全に予想外。
                    if (type == NotificationFlag.InviteCommunitiy)
                        data = new ContactNotificationData(source, plusBaseUrl, type);
                    else if (type.HasFlag(NotificationFlag.InviteCommunitiy) == false && type.HasFlag(NotificationFlag.SubscriptionCommunitiy))
                        data = new ContentNotificationData(source, plusBaseUrl, type);
                    else
                        throw new InvalidDataException("未知の通知データを検出。", source, null);
                    break;
                case "gplus_hangout":
                    data = new HangoutNotificationData(source, plusBaseUrl, type);
                    break;
                case "gplus_stream":
                    data = new ContentNotificationData(source, plusBaseUrl, type);
                    break;
                case "gplus_photos":
                    data = new PhotoNotificationData(source, plusBaseUrl, type);
                    break;
                default:
                    throw new InvalidDataException("未知の通知データを検出。", source, null);
            }
            return data;
        }
        protected static NotificationFlag ConvertFlags(string typeTxt)
        {
            NotificationFlag type;
            switch (typeTxt)
            {
                case "CIRCLE_PERSONAL_ADD":
                    type = NotificationFlag.CircleIn;
                    break;
                case "CIRCLE_RECIPROCATING_ADD":
                    type = NotificationFlag.CircleAddBack;
                    break;
                case "HANGOUT_INVITE":
                    type = NotificationFlag.InviteHangout;
                    break;
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
                case "SQUARE_SUBSCRIPTION":
                    type = NotificationFlag.SubscriptionCommunitiy;
                    break;
                case "SQUARE_INVITE":
                    type = NotificationFlag.InviteCommunitiy;
                    break;
                case "PHOTOS_CAMERASYNC_UPLOADED":
                    type = NotificationFlag.CameraSyncUploaded;
                    break;
                case "PHOTOS_NEW_PHOTO_ADDED":
                    type = NotificationFlag.NewPhotosAdded;
                    break;
                default:
                    throw new InvalidDataException("未知の通知データを検出。", typeTxt, null);
            }
            return type;
        }
    }
    [Flags]
    public enum NotificationFlag
    {
        CameraSyncUploaded = 0x00000001,
        CircleAddBack = 0x00000002,
        CircleIn = 0x00000004,
        DirectMessage = 0x00000008,
        Followup = 0x00000010,
        InviteHangout = 0x00000020,
        InviteCommunitiy = 0x00000040,
        Mension = 0x00000080,
        NewPhotosAdded = 0x00000100,
        SubscriptionCommunitiy = 0x00000200,
        PlusOne = 0x00000400,
        Reshare = 0x00000800,
        Response = 0x00001000,
        TaggedImage = 0x00002000,
    }
}
