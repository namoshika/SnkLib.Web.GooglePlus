using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class AttachedAlbum : AccessorBase, IAttachable
    {
        public AttachedAlbum(PlatformClient client, AttachedAlbumData data)
            : base(client)
        {
            _data = data;
            Album = new AlbumInfo(client, data.Album);
            Pictures = _data.Pictures.Select(dt => new AttachedImage(client, dt)).ToArray();
        }
        AttachedAlbumData _data;
        public ContentType Type { get { return _data.Type; } }
        public Uri LinkUrl { get { return _data.LinkUrl; } }
        public AlbumInfo Album { get; private set; }
        public AttachedImage[] Pictures { get; private set; }
    }
}
