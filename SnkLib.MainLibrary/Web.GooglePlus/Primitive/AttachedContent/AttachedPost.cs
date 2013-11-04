using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class AttachedPost : IAttachable
    {
        public AttachedPost(
            string id, string html, string text, string ownerId, string ownerName,
            Uri ownerIconUrl, Uri postUrl, IAttachable attachedContent)
        {
            Id = id;
            Html = html;
            Text = text;
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
        public Uri OwnerIconUrl { get; private set; }
        public Uri LinkUrl { get; private set; }
        public IAttachable AttachedContent { get; private set; }
    }
}
