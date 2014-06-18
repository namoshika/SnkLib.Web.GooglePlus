using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class AttachedImageData : AttachedLink
    {
        public AttachedImageData(ContentType type, string title, string summary,
            Uri linkUrl, Uri faviconUrl, Uri originalThumbnailUrl, string thumbnailUrl, int thumbnailWidth,
            int thumbnailHeight, ImageData image, AlbumData album, Uri plusBaseUrl)
            : base(type, title, summary, linkUrl, faviconUrl, originalThumbnailUrl, thumbnailUrl, thumbnailWidth, thumbnailHeight)
        {
            Image = image;
            Album = album;
        }
        public readonly ImageData Image;
        public readonly AlbumData Album;
    }
}
