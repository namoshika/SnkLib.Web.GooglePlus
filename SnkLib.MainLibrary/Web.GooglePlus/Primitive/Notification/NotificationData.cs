using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class NotificationData
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
}
