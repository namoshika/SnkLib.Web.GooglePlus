using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class AttachedLink : AttachedBase
    {
        public AttachedLink(
            ContentType type, string title, string summary, Uri linkUrl, Uri faviconUrl, Uri originalThumbnailUrl,
            string thumbnailUrl, int thumbnailWidth, int thumbnailHeight)
            : base(type, linkUrl)
        {
            Title = title;
            Summary = summary;
            FaviconUrl = faviconUrl;
            OriginalThumbnailUrl = originalThumbnailUrl;
            ThumbnailUrl = thumbnailUrl;
            ThumbnailWidth = thumbnailWidth;
            ThumbnailHeight = thumbnailHeight;
        }

        public readonly string Title;
        public readonly string Summary;
        public readonly Uri FaviconUrl;
        public readonly Uri OriginalThumbnailUrl;
        public readonly string ThumbnailUrl;
        public readonly int ThumbnailWidth;
        public readonly int ThumbnailHeight;
    }
}
