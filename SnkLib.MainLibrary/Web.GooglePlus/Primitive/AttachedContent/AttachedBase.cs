using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public abstract class AttachedContent : IAttachable
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
        public static AttachedContent Create(JArray attachedContentJson, Uri plusBaseUrl)
        {
            AttachedContent atchCnt;
            if (attachedContentJson[1].Type == JTokenType.Object)
            {
                //単発写真
                var tmpJson = (JArray)attachedContentJson[1].First.First;
                atchCnt = new AttachedAlbum();
                atchCnt.PlusBaseUrl = plusBaseUrl;
                atchCnt.ParseTemplate(tmpJson);
            }
            else if (attachedContentJson[2].Type == JTokenType.Object
                && ((string)attachedContentJson[1]).Substring(0, 30) == "https://plus.google.com/photos")
            {
                var tmpJson = (JArray)attachedContentJson[2].First.First;
                if (tmpJson[1].Type == JTokenType.Null)
                {
                    //アルバム
                    atchCnt = new AttachedAlbum();
                    atchCnt.PlusBaseUrl = plusBaseUrl;
                    atchCnt.ParseTemplate(tmpJson);
                }
                else
                {
                    //独立複数枚写真
                    atchCnt = new AttachedImage();
                    atchCnt.PlusBaseUrl = plusBaseUrl;
                    atchCnt.ParseTemplate(tmpJson);
                }
            }
            else if (attachedContentJson[2].Type == JTokenType.Object
                || attachedContentJson[7].Type == JTokenType.Object)
            {
                
                JToken tmpJson;
                tmpJson = ((tmpJson = attachedContentJson[2]).Type == JTokenType.Object
                    ? tmpJson : attachedContentJson[7]).First.First;
                if (((JArray)tmpJson).Count < 66 || tmpJson[65].Type == JTokenType.Null)
                {
                    //リンク
                    atchCnt = new AttachedLink();
                    atchCnt.PlusBaseUrl = plusBaseUrl;
                    atchCnt.ParseTemplate((JArray)tmpJson);
                }
                else
                {
                    //youtube
                    atchCnt = new AttachedYouTube();
                    atchCnt.PlusBaseUrl = plusBaseUrl;
                    atchCnt.ParseTemplate((JArray)tmpJson);
                }
            }
            else
                throw new Exception("添付コンテンツの読み取りに予想外のデータが入ってきました。");
            
            return atchCnt;
        }
    }
    public interface IAttachable
    {
        Uri LinkUrl { get; }
        ContentType Type { get; }
    }
}
