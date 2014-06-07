using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class AttachedYouTube : AttachedLink
    {
        public AttachedYouTube(ContentType type, string title, string summary, Uri linkUrl, Uri embedMovieUrl, Uri faviconUrl,
            Uri originalThumbnailUrl, string thumbnailUrl, int thumbnailWidth, int thumbnailHeight, Uri plusBaseUrl)
            : base(type, title, summary, linkUrl, faviconUrl, originalThumbnailUrl, thumbnailUrl, thumbnailWidth, thumbnailHeight)
        { EmbedMovieUrl = embedMovieUrl; }
        public readonly Uri EmbedMovieUrl;
    }
}
