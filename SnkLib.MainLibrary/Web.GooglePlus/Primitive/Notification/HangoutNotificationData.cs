using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class HangoutNotificationData : NotificationData
    {
        public HangoutNotificationData(
            NotificationFlag type, string id, string rawNoticedDate, string title, string summary,
            Uri linkUrl, ProfileData actor, DateTime noticedDate)
            : base(type, id, rawNoticedDate, title, summary, noticedDate)
        {
            LinkUrl = linkUrl;
            Actor = actor;
        }
        public readonly Uri LinkUrl;
        public readonly ProfileData Actor;
    }
}
