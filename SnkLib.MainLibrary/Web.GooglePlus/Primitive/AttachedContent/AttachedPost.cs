using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class AttachedPost : IAttachable
    {
        public AttachedPost(PlatformClient client, AttachedPostData data)
        {
            _data = data;
            AttachedContent = data.AttachedContent != null
                ? ActivityInfo.AttachedContentDecorator(data.AttachedContent, client) : null;
        }
        AttachedPostData _data;
        public ContentType Type { get { return ContentType.Reshare; } }
        public string Id { get { return _data.Id; } }
        public string Html { get { return _data.Html; } }
        public string Text { get { return _data.Text; } }
        public string OwnerId { get { return _data.OwnerId; } }
        public string OwnerName { get { return _data.OwnerName; } }
        public string OwnerIconUrl { get { return _data.OwnerIconUrl; } }
        public Uri LinkUrl { get { return _data.LinkUrl; } }
        public StyleElement ParsedText { get { return _data.ParsedText; } }
        public IAttachable AttachedContent { get; private set; }
    }
    public class AttachedPostData : IAttachable
    {
        public AttachedPostData(
            string id, string html, string text, StyleElement parsedText, string ownerId, string ownerName,
            string ownerIconUrl, Uri postUrl, IAttachable attachedContent)
        {
            Id = id;
            Html = html;
            Text = text;
            ParsedText = parsedText;
            LinkUrl = postUrl;
            OwnerId = ownerId;
            OwnerName = ownerName;
            OwnerIconUrl = ownerIconUrl;
            AttachedContent = attachedContent;
        }
        public ContentType Type { get { return ContentType.Reshare; } }
        public string Id { get; private set; }
        public string Html { get; private set; }
        public string Text { get; private set; }
        public string OwnerId { get; private set; }
        public string OwnerName { get; private set; }
        public string OwnerIconUrl { get; private set; }
        public Uri LinkUrl { get; private set; }
        public StyleElement ParsedText { get; private set; }
        public IAttachable AttachedContent { get; private set; }
    }
}
