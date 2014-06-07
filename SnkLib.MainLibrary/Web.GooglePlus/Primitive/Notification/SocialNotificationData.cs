using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class SocialNotificationData : NotificationData
    {
        public SocialNotificationData(
            NotificationFlag type, string id, string rawNoticedDate, string title, string summary,
            NotificationItemData[] logItems, DateTime noticedDate)
            : base(type, id, rawNoticedDate, title, summary, noticedDate) { LogItems = logItems; }
        public ProfileData Actor { get { return LogItems.First().Actor; } }
        public readonly NotificationItemData[] LogItems;
    }
    public class NotificationItemData
    {
        public NotificationItemData(ProfileData actor, NotificationFlag type, string rawData)
        {
            Actor = actor;
            Type = type;
            RawData = rawData;
        }
        public ProfileData Actor { get; private set; }
        public NotificationFlag Type { get; private set; }
        public string RawData { get; private set; }
    }
}
