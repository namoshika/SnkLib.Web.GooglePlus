using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class NotificationData
    {
        public NotificationData(NotificationsFilter type, ChainingNotificationData[] followingNotifications)
        {
            Type = type;
            FollowingNotifications = followingNotifications;
        }
        public NotificationsFilter Type { get; private set; }
        public ChainingNotificationData[] FollowingNotifications { get; private set; }
        public string Id { get { return FollowingNotifications.First().Id; } }
        public ProfileData Actor { get { return FollowingNotifications.First().Actor; } }
        public DateTime NoticedDate { get { return FollowingNotifications.First().NoticedDate; } }
    }
    public class NotificationDataWithActivity : NotificationData
    {
        public NotificationDataWithActivity(ActivityData target, NotificationsFilter type, ChainingNotificationData[] followingNotifications)
            : base(type, followingNotifications) { Target = target; }
        public ActivityData Target { get; private set; }
    }
    public class NotificationDataWithImage : NotificationData
    {
        public NotificationDataWithImage(ImageData[] images, NotificationsFilter type, ChainingNotificationData[] followingNotifications)
            : base(type, followingNotifications) { Images = images; }
        public ImageData[] Images { get; private set; }
    }
    public class ChainingNotificationData : DataBase
    {
        public ChainingNotificationData(string id, ProfileData actor, DateTime noticeDate)
        {
            Id = id;
            Actor = actor;
            NoticedDate = noticeDate;
        }
        public string Id { get; private set; }
        public ProfileData Actor { get; private set; }
        public DateTime NoticedDate { get; private set; }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay_TypeName,nq}")]
    public class ApplicationType
    {
        ApplicationType(string id, bool isWellKnown)
        {
            Id = id;
            Segments = id.Split(':');
            IsMobile = Segments.Length > 1 ? Segments[1] == "updatesmobile" : false;
            IsWellKnown = isWellKnown;
        }
        public string Id { get; private set; }
        public string[] Segments { get; private set; }
        public bool IsMobile { get; private set; }
        public bool IsWellKnown { get; private set; }
        public override bool Equals(object obj)
        {
            var tmp = obj as ApplicationType;
            return obj != null && tmp != null ? Id == tmp.Id : false;
        }
        public override int GetHashCode()
        { return Id.GetHashCode(); }

        static ApplicationType()
        {
            Album = new ApplicationType("s:lh2", true);
            PlusOne = new ApplicationType("s:plusone", true);
            DesktopWeb = new ApplicationType("s:oz", true);
            Basic = new ApplicationType("s:updatesmobile:basic", true);
            MobileWeb = new ApplicationType("s:updatesmobile:web", true);
            MobileIPhone = new ApplicationType("s:updatesmobile:iphone", true);
            MobileAndroid = new ApplicationType("s:updatesmobile:android", true);
            PicasaClient = new ApplicationType("s:lh2:esactivity:picasaclient", true);
            Notification = new ApplicationType("s:notifications", true);
            Chili = new ApplicationType("s:chili", true);

        }
        public static ApplicationType Album { get; private set; }
        public static ApplicationType Basic { get; private set; }
        public static ApplicationType PlusOne { get; private set; }
        public static ApplicationType DesktopWeb { get; private set; }
        public static ApplicationType MobileWeb { get; private set; }
        public static ApplicationType MobileIPhone { get; private set; }
        public static ApplicationType MobileAndroid { get; private set; }
        public static ApplicationType PicasaClient { get; private set; }
        public static ApplicationType Chili { get; private set; }
        public static ApplicationType Notification { get; private set; }
        public static ApplicationType Create(string id)
        {
            switch (id)
            {
                case "s:lh2":
                    return Album;
                case "s:plusone":
                    return PlusOne;
                case "s:oz":
                    return DesktopWeb;
                case "s:updatesmobile:basic":
                    return Basic;
                case "s:updatesmobile:web":
                    return MobileWeb;
                case "s:updatesmobile:iphone":
                    return MobileIPhone;
                case "s:updatesmobile:android":
                    return MobileAndroid;
                case "s:lh2:esactivity:picasaclient":
                    return PicasaClient;
                case "s:chili":
                    return Chili;
                case "s:notifications":
                    return Notification;
                default:
                    return new ApplicationType(id, false);
            }
        }

        public static bool operator ==(ApplicationType valueA, ApplicationType valueB)
        { return valueA.Equals(valueB); }
        public static bool operator !=(ApplicationType valueA, ApplicationType valueB)
        { return !valueA.Equals(valueB); }
        string DebuggerDisplay_TypeName
        {
            get
            {
                string tmp;
                if (Id == ApplicationType.DesktopWeb.Id)
                    tmp = "DesktopWeb";
                else if (Id == ApplicationType.MobileWeb.Id)
                    tmp = "MobileWeb";
                else if (Id == ApplicationType.MobileIPhone.Id)
                    tmp = "iPhone";
                else if (Id == ApplicationType.MobileAndroid.Id)
                    tmp = "Android";
                else if (Id == ApplicationType.PlusOne.Id)
                    tmp = "PlusOne";
                else
                    tmp = "Unknown";
                return string.Format("({1}){{Id = {0}, IsMobile = {2}}}", Id, tmp, IsMobile);
            }
        }
    }
}
