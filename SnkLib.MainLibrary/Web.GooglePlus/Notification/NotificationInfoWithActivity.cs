using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class NotificationInfoWithActivity : NotificationInfoWithActor
    {
        public NotificationInfoWithActivity(ContentNotificationData data, NotificationInfoContainer container, PlatformClient client)
            : base(data, container, client) { Activity = client.Activity.InternalGetAndUpdateActivity(data.Target); }
        public ActivityInfo Activity { get; private set; }
    }
}
