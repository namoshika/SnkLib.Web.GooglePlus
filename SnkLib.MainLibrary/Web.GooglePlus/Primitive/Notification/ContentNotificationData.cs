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
        public ContentNotificationData(
            NotificationFlag type, string id, string rawNoticedDate, string title, string summary,
            ActivityData target, NotificationItemData[] logItems, DateTime noticedDate)
            : base(type, id, rawNoticedDate, title, summary, logItems, noticedDate) { Target = target; }
        public readonly ActivityData Target;
    }
}
