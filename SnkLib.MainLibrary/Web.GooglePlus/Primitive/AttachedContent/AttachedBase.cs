using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public abstract class AttachedBase : IAttachable
    {
        public Uri LinkUrl { get; private set; }
        public Uri PlusBaseUrl { get; private set; }
        public abstract ContentType Type { get; }
        
        protected virtual void ParseTemplate(JArray json)
        {
            string tmp;
            LinkUrl = string.IsNullOrEmpty(tmp = (string)json[0])
                ? null
                : tmp[0] == '/' ? new Uri(PlusBaseUrl, tmp) : new Uri(tmp);
        }
        public static AttachedBase Create(JArray attachedContentJson, Uri plusBaseUrl)
        {
            JObject json = (JObject)(
                attachedContentJson.Count > 7 && attachedContentJson[7].Type == JTokenType.Object ? attachedContentJson[7] :
                attachedContentJson.Count > 6 && attachedContentJson[6].Type == JTokenType.Object ? attachedContentJson[6] :
                attachedContentJson.Count > 4 && attachedContentJson[4].Type == JTokenType.Object ? attachedContentJson[4] :
                attachedContentJson.Count > 2 && attachedContentJson[2].Type == JTokenType.Object ? attachedContentJson[2] :
                attachedContentJson.Count > 1 && attachedContentJson[1].Type == JTokenType.Object ? attachedContentJson[1] :
                null);
            if(json == null)
                throw new Exception("添付コンテンツの読み取りに予想外のデータが入ってきました。");

            AttachedBase content;
            var prop = json.Properties().First();
            var tmpJson = (JArray)prop.Value;
            switch(prop.Name)
            {
                case "40154698":
                case "39748951":
                    //リンク
                    content = new AttachedLink();
                    content.PlusBaseUrl = plusBaseUrl;
                    content.ParseTemplate((JArray)json.First.First);
                    break;
                case "40655821":
                    //写真
                    content = new AttachedImage();
                    content.PlusBaseUrl = plusBaseUrl;
                    content.ParseTemplate((JArray)json.First.First);
                    break;
                case "40842909":
                    //アルバム
                    content = new AttachedAlbum();
                    content.PlusBaseUrl = plusBaseUrl;
                    content.ParseTemplate((JArray)json.First.First);
                    break;
                case "41186541":
                    //youtube
                    content = new AttachedYouTube();
                    content.PlusBaseUrl = plusBaseUrl;
                    content.ParseTemplate((JArray)json.First.First);
                    break;
                case "41359510":
                    //現在地共有
                    content = null;
                    break;
                default:
                    content = null;
                    System.Diagnostics.Debug.Assert(false, string.Format("未確認の添付コンテンツが発見されました。Type:{0}", prop.Name));
                    break;
            }

            return content;
        }
    }
    public interface IAttachable
    {
        Uri LinkUrl { get; }
        ContentType Type { get; }
    }
}
