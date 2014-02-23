using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class AttachedAlbum : AccessorBase, IAttachable
    {
        public AttachedAlbum(PlatformClient client, AttachedAlbumData data)
            : base(client)
        {
            _data = data;
            Album = new AlbumInfo(client, data.Album);
            Pictures = _data.Pictures.Select(dt => new ImageInfo(client, dt)).ToArray();
        }
        AttachedAlbumData _data;
        public ContentType Type { get { return _data.Type; } }
        public Uri LinkUrl { get { return _data.LinkUrl; } }
        public AlbumInfo Album { get; private set; }
        public ImageInfo[] Pictures { get; private set; }
    }
    public class AttachedAlbumData : AttachedBase
    {
        public AttachedAlbumData(JArray json, Uri plusBaseUrl) : base(json, plusBaseUrl) { }
        public override ContentType Type { get { return ContentType.Album; } }
        public AlbumData Album { get; private set; }
        public ImageData[] Pictures { get; private set; }

        protected override void ParseTemplate(JArray json)
        {
            base.ParseTemplate(json);
            ParseAlbum(json);
        }
        protected void ParseAlbum(JArray json)
        {
            //アルバム
            var albumTitle = Primitive.ApiAccessorUtility.DecodeHtmlText((string)json[2]);
            var albumId = (string)json[37];
            var ownerId = (string)json[26];
            Album = new AlbumData(albumId, albumTitle, LinkUrl, owner: new ProfileData(ownerId), loadedApiTypes: AlbumUpdateApiFlag.Base);
            Pictures = json[41].Select(item => ((AttachedImageData)Create((JArray)item, PlusBaseUrl)).Image).ToArray();
        }
    }
}
