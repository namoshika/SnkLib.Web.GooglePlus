using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus
{
    public class NotificationSignal
    {
        public NotificationSignal(NotificationEventType _type, string _attachedNotificationId)
        {
            Type = _type;
            AttachedNotificationId = _attachedNotificationId;
        }
        public NotificationEventType Type { get; private set; }
        public string AttachedNotificationId { get; private set; }
    }
    public enum NotificationEventType { RaiseNew, ChangedAllRead }
}
