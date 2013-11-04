using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class AttachedImage : AttachedLink
    {
        public ImageData Image { get; private set; }
        public AlbumData Album { get; private set; }

        protected override void ParseTemplate(JArray json)
        {
            base.ParseTemplate(json);
            ParseImage(json);
        }
        protected void ParseImage(JArray json)
        {
            Album = new AlbumData((string)json[37]);
            Image = new ImageData(
                false, (string)json[38], (string)json[2], (int)json[20], (int)json[21], ApiWrapper.ConvertReplasableUrl((string)json[1]),
                owner: new ProfileData(ProfileUpdateApiFlag.Unloaded, id: (string)json[26]));
        }
    }
}
