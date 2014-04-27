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
        public SocialNotificationData(JToken source, Uri plusBaseUrl) : base(source, plusBaseUrl) { }
        public ProfileData Actor { get { return LogItems.First().Actor; } }
        public NotificationItemData[] LogItems { get; protected set; }
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
