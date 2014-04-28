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
        public PhotoNotificationData(JToken source, Uri plusBaseUrl, NotificationFlag typeFlags) : base(source, plusBaseUrl, typeFlags) { }
        public Uri LinkUrl { get; private set; }
        public string[] ImagesUrl { get; private set; }
        protected override void ParseTemplate(JToken source, Uri plusBaseUrl, NotificationFlag typeFlags)
        {
            base.ParseTemplate(source, plusBaseUrl, typeFlags);
            var imgUrls = new List<string>();
            var tmp = source[4][1][0];
            var imgDatas = tmp[2];
            LinkUrl = new Uri((string)tmp[4][0][0][2]);
            foreach (var item in imgDatas)
                imgUrls.Add(ApiAccessorUtility.ConvertReplasableUrl((string)item[0][0]));
            ImagesUrl = imgUrls.ToArray();
        }
    }
}
