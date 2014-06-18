using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class AttachedImage : AccessorBase, IAttachable
    {
        public AttachedImage(PlatformClient client, AttachedImageData data)
            : base(client)
        {
            _data = data;
            Image = new ImageInfo(client, data.Image);
            Album = new AlbumInfo(client, data.Album);
        }
        AttachedImageData _data;
        public ContentType Type { get { return _data.Type; } }
        public Uri LinkUrl { get { return _data.LinkUrl; } }
        public ImageInfo Image { get; private set; }
        public AlbumInfo Album { get; private set; }
    }
}
