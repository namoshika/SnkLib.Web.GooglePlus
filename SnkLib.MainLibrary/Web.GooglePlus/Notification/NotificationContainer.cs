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
        public NotificationInfoContainer GetNotifications(bool isReadedItemOnly)
        {
            var container = new NotificationInfoContainer(Client, isReadedItemOnly);
            return container;
        }
        public async Task<int> GetUnreadCount()
        {
            try { return await Client.ServiceApi.GetUnreadNotificationCountAsync(Client); }
            catch(ApiErrorException e)
            { throw new FailToOperationException("未読通知数取得に失敗しました。", e); }
        }
    }
}
