using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
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
    public class AttachedImageData : AttachedLink
    {
        public AttachedImageData(JArray json, Uri plusBaseUrl) : base(json, plusBaseUrl) { }
        public override ContentType Type { get { return ContentType.Image; } }
        public ImageData Image { get; private set; }
        public AlbumData Album { get; private set; }

        protected override void ParseTemplate(JArray json)
        {
            base.ParseTemplate(json);
            ParseImage(json);
        }
        protected void ParseImage(JArray json)
        {
            Album = new AlbumData((string)json[37], owner: new ProfileData((string)json[26]));
            Image = new ImageData(
                ImageUpdateApiFlag.Base, (string)json[38], (string)json[2], (int)json[20], (int)json[21], ApiAccessorUtility.ConvertReplasableUrl((string)json[1]),
                LinkUrl, owner: new ProfileData((string)json[26], loadedApiTypes: ProfileUpdateApiFlag.Unloaded));
        }
    }
}
