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
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay_TypeName,nq}")]
    public class ServiceType
    {
        public ServiceType(string id)
        {
            Id = id;
            if (id != null)
                Segments = id.Split(':');
        }
        public ServiceType(string id, params string[] segments)
        {
            Id = id;
            Segments = segments ?? new string[] { };
        }
        public string Id { get; private set; }
        public string[] Segments { get; private set; }
        public bool IsMobile { get { return Segments.Length > 1 ? Segments[1] == "updatesmobile" : false; } }
        public override bool Equals(object obj)
        {
            var tmp = obj as ServiceType;
            return obj != null && tmp != null ? Id == tmp.Id : false;
        }
        public override int GetHashCode()
        { return Id.GetHashCode(); }

        static ServiceType()
        {
            Desktop = new ServiceType("s:updates:esshare", "s", "updates", "esshare");
            Mobile = new ServiceType("s:updatesmobile:esshare", "s", "updates", "esshare");
            Checkins = new ServiceType("s:updatesmobile:checkins", "s", "updatesmobile", "checkins");
            Hangout = new ServiceType("s:talk:gcomm", "s", "talk", "gcomm");
            Unknown = new ServiceType(null);
        }
        static ServiceType _unknown;
        public static ServiceType Desktop { get; private set; }
        public static ServiceType Mobile { get; private set; }
        public static ServiceType Checkins { get; private set; }
        public static ServiceType Hangout { get; private set; }
        public static ServiceType Unknown
        {
            get
            {
                Debug_BreakIfIsAttached();
                return _unknown;
            }
            set { _unknown = value; }
        }

        public static bool operator ==(ServiceType valueA, ServiceType valueB)
        { return (object)valueA != null && !valueA.Equals(valueB) || (object)valueA == null && (object)valueB == null; }
        public static bool operator !=(ServiceType valueA, ServiceType valueB)
        { return !(valueA == valueB); }

        [System.Diagnostics.Conditional("DEBUG")]
        static void Debug_BreakIfIsAttached()
        {
            if (System.Diagnostics.Debugger.IsAttached)
                System.Diagnostics.Debugger.Break();
        }
        string DebuggerDisplay_TypeName
        {
            get
            {
                string tmp;
                if (Id == ServiceType.Desktop.Id)
                    tmp = "Desktop";
                else if (Id == ServiceType.Mobile.Id)
                    tmp = "Mobile";
                else if (Id == ServiceType.Checkins.Id)
                    tmp = "Checkins";
                else if (Id == ServiceType.Hangout.Id)
                    tmp = "Hangout";
                else
                    tmp = "ServiceType.Unknown";
                return string.Format("({1}){{Id = {0}, IsMobile = {2}}}", Id, tmp, IsMobile);
            }
        }
    }
    public enum PostStatusType { Removed, First, Edited }
    public enum ActivityUpdateApiFlag { Unloaded = 0, GetActivities = 1, Notification = 3, GetActivity = 7 }
}
