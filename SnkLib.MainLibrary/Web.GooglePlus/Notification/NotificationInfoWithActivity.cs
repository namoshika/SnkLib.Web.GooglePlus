using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class NotificationInfoWithActivity : NotificationInfo
    {
        public NotificationInfoWithActivity(NotificationDataWithActivity data, NotificationInfoContainer container, PlatformClient client)
            : base(data, container, client) { Activity = client.Activity.InternalGetAndUpdateActivity(data.Target); }
        public ActivityInfo Activity { get; private set; }
    }
}
