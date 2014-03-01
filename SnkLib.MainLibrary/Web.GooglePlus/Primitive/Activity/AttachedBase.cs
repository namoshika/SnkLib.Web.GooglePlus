using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public abstract class AttachedBase : IAttachable
    {
        public AttachedBase(JArray json, Uri plusBaseUrl)
        {
            PlusBaseUrl = plusBaseUrl;
            ParseTemplate(json);
        }
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
            AttachedBase content;
            var prop = GetContentBody(attachedContentJson);
            switch(prop.Name)
            {
                case "40154698":
                case "39748951":
                case "42861421":
                    //リンク
                    content = new AttachedLink((JArray)prop.Value, plusBaseUrl);
                    break;
                case "41561070":
                    //インタラクティブ
                    content = new AttachedInteractiveLink((JArray)prop.Value, plusBaseUrl);
                    break;
                case "40655821":
                    //写真
                    content = new AttachedImageData((JArray)prop.Value, plusBaseUrl);
                    break;
                case "40842909":
                    //アルバム
                    content = new AttachedAlbumData((JArray)prop.Value, plusBaseUrl);
                    break;
                case "41186541":
                    //youtube
                    content = new AttachedYouTube((JArray)prop.Value, plusBaseUrl);
                    break;
                case "41359510":
                    //現在地共有
                    content = null;
                    break;
                default:
                    content = null;
                    System.Diagnostics.Debug.WriteLine(string.Format("未確認の添付コンテンツが発見されました。JSON:{0}", attachedContentJson));
                    break;
            }

            return content;
        }
        protected static JProperty GetContentBody(JArray attachedContentJson)
        {
            JObject json = (JObject)(
                attachedContentJson.Count > 7 && attachedContentJson[7].Type == JTokenType.Object ? attachedContentJson[7] :
                attachedContentJson.Count > 6 && attachedContentJson[6].Type == JTokenType.Object ? attachedContentJson[6] :
                attachedContentJson.Count > 5 && attachedContentJson[5].Type == JTokenType.Object ? attachedContentJson[5] :
                attachedContentJson.Count > 4 && attachedContentJson[4].Type == JTokenType.Object ? attachedContentJson[4] :
                attachedContentJson.Count > 2 && attachedContentJson[2].Type == JTokenType.Object ? attachedContentJson[2] :
                attachedContentJson.Count > 1 && attachedContentJson[1].Type == JTokenType.Object ? attachedContentJson[1] :
                null);
            if (json == null)
                throw new Exception("添付コンテンツの読み取りに予想外のデータが入ってきました。");
            var prop = json.Properties().First();
            return prop;
        }
    }
    public interface IAttachable
    {
        Uri LinkUrl { get; }
        ContentType Type { get; }
    }
}
