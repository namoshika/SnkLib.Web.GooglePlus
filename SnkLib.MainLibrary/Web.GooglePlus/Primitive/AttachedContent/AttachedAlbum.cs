using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class AttachedAlbum : AttachedBase
    {
        public override ContentType Type { get { return ContentType.Album; } }
        public AlbumData Album { get; private set; }
        public AttachedImage[] Pictures { get; private set; }

        protected override void ParseTemplate(JArray json)
        {
            base.ParseTemplate(json);
            ParseAlbum(json);
        }
        protected void ParseAlbum(JArray json)
        {
            //アルバム
            var albumTitle = Primitive.ApiWrapper.DecodeHtmlText((string)json[2]);
            var albumId = (string)json[37];
            var ownerId = (string)json[26];
            Album = new AlbumData(albumId, albumTitle, LinkUrl, owner: new ProfileData(id: ownerId));
            Pictures = json[41].Select(item => (AttachedImage)Create((JArray)item, PlusBaseUrl)).ToArray();
        }
    }
}
