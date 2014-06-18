using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class ActivityData : CoreData
    {
        public ActivityData(
            string id, string html = null, string text = null, StyleElement parsedText = null, bool? isEditable = null, Uri postUrl = null,
            int? commentLength = null, CommentData[] comments = null,
            DateTime? postDate = null,
            DateTime? editDate = null,
            ServiceType serviceType = null, PostStatusType? status = null, IAttachable attachedContent = null,
            ProfileData owner = null, DateTime? getActivityDate = null,
            ActivityUpdateApiFlag updaterTypes = ActivityUpdateApiFlag.Unloaded)
        {
            LoadedApiTypes = updaterTypes;
            Id = id;
            IsEditable = isEditable;
            Html = html;
            Text = text;
            ParsedText = parsedText;
            CommentLength = commentLength;
            Comments = comments;
            PostUrl = postUrl;
            PostDate = postDate;
            EditDate = editDate;
            GetActivityDate = getActivityDate;
            PostStatus = status;
            Owner = owner;
            AttachedContent = attachedContent;
            ServiceType = serviceType;
        }

        public readonly ActivityUpdateApiFlag LoadedApiTypes;
        public readonly bool? IsEditable;
        [Identification]
        public readonly string Id;
        public readonly string Html;
        public readonly string Text;
        public readonly StyleElement ParsedText;
        public readonly Uri PostUrl;
        public readonly DateTime? PostDate;
        public readonly DateTime? EditDate;
        public readonly DateTime? GetActivityDate;
        public readonly ProfileData Owner;
        public readonly IAttachable AttachedContent;
        public readonly PostStatusType? PostStatus;
        public readonly ServiceType ServiceType;
        public int? CommentLength;
        public CommentData[] Comments;
        //public PlusOneInfo PlusOne;

        public static ActivityData operator +(ActivityData value1, ActivityData value2)
        {
            bool val1_isNotNull = value1 != null, val2_isNotNull = value2 != null;
            if (val1_isNotNull && val2_isNotNull)
            {
                var newUpdateType =
                (value2 != null ? value2.LoadedApiTypes : ActivityUpdateApiFlag.Unloaded)
                | (value1 != null ? value1.LoadedApiTypes : ActivityUpdateApiFlag.Unloaded);
                return new ActivityData(
                    Merge(value1, value2, obj => obj.Id),
                    Merge(value1, value2, obj => obj.Html),
                    Merge(value1, value2, obj => obj.Text),
                    Merge(value1, value2, obj => obj.ParsedText),
                    Merge(value1, value2, obj => obj.IsEditable),
                    Merge(value1, value2, obj => obj.PostUrl),
                    Merge(value1, value2, obj => obj.CommentLength),
                    Merge(value1, value2, obj => obj.Comments),
                    Merge(value1, value2, obj => obj.PostDate),
                    Merge(value1, value2, obj => obj.EditDate),
                    Merge(value1, value2, obj => obj.ServiceType),
                    Merge(value1, value2, obj => obj.PostStatus),
                    Merge(value1, value2, obj => obj.AttachedContent),
                    Merge(value1, value2, obj => obj.Owner, true),
                    Merge(value1, value2, obj => obj.GetActivityDate),
                    newUpdateType);
            }
            else if (val2_isNotNull)
                return value2;
            else
                return value1;
        }
    }
}
