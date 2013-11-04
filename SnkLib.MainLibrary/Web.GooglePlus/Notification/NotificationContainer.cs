using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class NotificationContainer : AccessorBase
    {
        public NotificationContainer(PlatformClient client) : base(client) { }
        public NotificationInfoContainer GetNotifications(NotificationsFilter filter)
        {
            var container = new NotificationInfoContainer(Client, filter);
            return container;
        }
    }
}
