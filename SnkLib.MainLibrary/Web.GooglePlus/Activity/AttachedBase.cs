using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus
{
    public abstract class AttachedBase : IAttachable
    {
        public AttachedBase(ContentType type, Uri linkUrl)
        {
            Type = type;
            LinkUrl = linkUrl;
        }
        public readonly Uri LinkUrl;
        public readonly ContentType Type;
        Uri IAttachable.LinkUrl { get { return LinkUrl; } }
        ContentType IAttachable.Type { get { return Type; } }
    }
    public interface IAttachable
    {
        Uri LinkUrl { get; }
        ContentType Type { get; }
    }
}
