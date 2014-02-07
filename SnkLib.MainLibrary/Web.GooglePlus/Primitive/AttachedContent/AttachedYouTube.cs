using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class AttachedYouTube : AttachedLink
    {
        public AttachedYouTube(JArray json, Uri plusBaseUrl) : base(json, plusBaseUrl) { }
        public override ContentType Type { get { return ContentType.YouTube; } }
        public Uri EmbedMovieUrl { get; private set; }

        protected override void ParseTemplate(JArray json)
        {
            base.ParseTemplate(json);
            ParseYouTube(json);
        }
        protected void ParseYouTube(JArray tmpJson)
        { EmbedMovieUrl = new Uri((string)tmpJson[65]); }
    }
}
