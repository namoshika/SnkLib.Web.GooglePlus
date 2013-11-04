using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class ActivityData : DataBase
    {
        public ActivityData(
            string id, string html = null, string text = null, bool? isEditable = null, Uri postUrl = null,
            CommentData[] comments = null,  DateTime? postDate = null, DateTime? editDate = null, DateTime? getActivityDate = null,
            ServiceType serviceType = null, PostStatusType? status = null, IAttachable attachedContent = null,
            ProfileData owner = null, ActivityUpdateApiFlag updaterTypes = ActivityUpdateApiFlag.Unloaded)
        {
            LoadedApiTypes = updaterTypes;
            Id = id;
            IsEditable = isEditable;
            Html = html;
            Text = text;
            Comments = comments;
            PostUrl = postUrl;
            PostDate = postDate;
            EditDate = editDate;
            GetActivityDate = getActivityDate;
            PostStatus = status;
            Owner = owner;
            AttachedContent = attachedContent;
            ServiceType = serviceType;
            //PlusOne               = Merge(baseData, plusOne, obj => obj.Html);
            //ParsedContent         = Merge(baseData, , obj => obj.Html);
        }

        public ActivityUpdateApiFlag LoadedApiTypes { get; private set; }
        public bool? IsEditable { get; private set; }
        public string Id { get; private set; }
        public string Html { get; private set; }
        public string Text { get; private set; }
        public Uri PostUrl { get; private set; }
        public DateTime? PostDate { get; private set; }
        public DateTime? EditDate { get; private set; }
        public DateTime? GetActivityDate { get; set; }
        public ProfileData Owner { get; private set; }
        public CommentData[] Comments { get; set; }
        public IAttachable AttachedContent { get; private set; }
        public PostStatusType? PostStatus { get; private set; }
        public ServiceType ServiceType { get; private set; }
        //public PlusOneInfo PlusOne { get; private set; }

        public static ActivityData operator +(ActivityData value1, ActivityData value2)
        {
            var flg = 
                (value2 != null ? value2.LoadedApiTypes : ActivityUpdateApiFlag.Unloaded)
                | (value1 != null ? value1.LoadedApiTypes : ActivityUpdateApiFlag.Unloaded);
            return new ActivityData(
                Merge(value1, value2, obj => obj.Id),
                Merge(value1, value2, obj => obj.Html),
                Merge(value1, value2, obj => obj.Text),
                Merge(value1, value2, obj => obj.IsEditable),
                Merge(value1, value2, obj => obj.PostUrl),
                Merge(value1, value2, obj => obj.Comments),
                Merge(value1, value2, obj => obj.PostDate),
                Merge(value1, value2, obj => obj.EditDate),
                Merge(value1, value2, obj => obj.GetActivityDate),
                Merge(value1, value2, obj => obj.ServiceType),
                Merge(value1, value2, obj => obj.PostStatus),
                Merge(value1, value2, obj => obj.AttachedContent),
                Merge(value1, value2, obj => obj.Owner, true),
                flg);
            
            //PlusOne               = AAA(baseData, plusOne, obj => obj.Html);
        }
    }
}
