using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class AttachedPost : IAttachable
    {
        public AttachedPost(PlatformClient client, AttachedPostData data)
        {
            _data = data;
            AttachedContent = data.AttachedContent != null
                ? ActivityInfo.AttachedContentDecorator(data.AttachedContent, client) : null;
        }
        readonly AttachedPostData _data;
        public ContentType Type { get { return _data.Type; } }
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
}
