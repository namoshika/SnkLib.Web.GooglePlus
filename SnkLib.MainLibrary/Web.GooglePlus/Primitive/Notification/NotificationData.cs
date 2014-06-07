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
        public NotificationData(
            NotificationFlag type, string id, string rawNoticedDate, string title, string summary, DateTime noticedDate)
        {
            Type = type;
            Id = id;
            RawNoticedDate = rawNoticedDate;
            Title = title;
            Summary = summary;
            NoticedDate = noticedDate;
        }
        public readonly NotificationFlag Type;
        public readonly string Id;
        public readonly string RawNoticedDate;
        public readonly string Title;
        public readonly string Summary;
        public readonly DateTime NoticedDate;
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
