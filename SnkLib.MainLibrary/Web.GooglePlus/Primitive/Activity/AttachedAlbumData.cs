using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class AttachedAlbumData : AttachedBase
    {
        public AttachedAlbumData(ContentType type, Uri linkUrl,
            AlbumData album, AttachedImageData[] pictures, Uri plusBaseUrl)
            : base(type, linkUrl)
        {
            Album = album;
            Pictures = pictures;
        }
        public readonly AlbumData Album;
        public readonly AttachedImageData[] Pictures;
    }
}
