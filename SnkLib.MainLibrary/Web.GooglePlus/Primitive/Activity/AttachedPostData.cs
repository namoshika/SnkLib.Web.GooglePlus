using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class AttachedPostData : AttachedBase
    {
        public AttachedPostData(
            ContentType type, string id, string html, string text, StyleElement parsedText, string ownerId,
            string ownerName, string ownerIconUrl, Uri postUrl, IAttachable attachedContent)
            : base(type, postUrl)
        {
            Id = id;
            Html = html;
            Text = text;
            ParsedText = parsedText;
            OwnerId = ownerId;
            OwnerName = ownerName;
            OwnerIconUrl = ownerIconUrl;
            AttachedContent = attachedContent;
        }
        public readonly string Id;
        public readonly string Html;
        public readonly string Text;
        public readonly string OwnerId;
        public readonly string OwnerName;
        public readonly string OwnerIconUrl;
        public readonly StyleElement ParsedText;
        public readonly IAttachable AttachedContent;
    }
}
