using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class AttachedLink : AttachedBase
    {
        public AttachedLink(JArray json, Uri plusBaseUrl) : base(json, plusBaseUrl) { }
        public override ContentType Type { get { return ContentType.Link; } }
        public string Title { get; protected set; }
        public string Summary { get; protected set; }
        public Uri FaviconUrl { get; protected set; }
        public Uri OriginalThumbnailUrl { get; protected set; }
        public string ThumbnailUrl { get; protected set; }
        public int ThumbnailWidth { get; protected set; }
        public int ThumbnailHeight { get; protected set; }

        protected override void ParseTemplate(JArray json)
        {
            base.ParseTemplate(json);
            ParseLink(json);
        }
        protected void ParseLink(JArray json)
        {
            string tmp;
            Title = (string)json[2];
            Summary = string.IsNullOrEmpty(tmp = (string)json[3]) ? null : tmp;
            FaviconUrl = string.IsNullOrEmpty(tmp = (string)json[6]) ? null : new Uri(ApiAccessorUtility.ComplementUrl(tmp, null));
            var thumbJson = json[5].Type == JTokenType.Array ? (JArray)json[5] : null;
            if (thumbJson != null)
            {
                var urlTxt = ApiAccessorUtility.ComplementUrl((string)thumbJson[0], null);
                ThumbnailWidth = (int)thumbJson[1];
                ThumbnailHeight = (int)thumbJson[2];
                ThumbnailUrl = System.Text.RegularExpressions.Regex
                    .Replace(urlTxt, string.Format("w{0}-h{1}(-[^-]+)*", ThumbnailWidth, ThumbnailHeight), "$SIZE_SEGMENT");
                OriginalThumbnailUrl = new Uri((string)json[1]);
            }
        }
    }
}
