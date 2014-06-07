using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class PhotoNotificationData : NotificationData
    {
        public PhotoNotificationData(
            NotificationFlag type, string id, string rawNoticedDate, string title, string summary,
            Uri linkUrl, string[] imagesUrl, DateTime noticedDate)
            : base(type, id, rawNoticedDate, title, summary, noticedDate)
        {
            LinkUrl = linkUrl;
            ImagesUrl = imagesUrl;
        }
        public readonly Uri LinkUrl;
        public readonly string[] ImagesUrl;
    }
}
