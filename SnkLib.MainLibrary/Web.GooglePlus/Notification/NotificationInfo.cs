using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class NotificationInfo : AccessorBase
    {
        public NotificationInfo(NotificationData data, NotificationInfoContainer container, PlatformClient client)
            : base(client)
        {
            _data = data;
            _container = container;
        }
        NotificationData _data;
        NotificationInfoContainer _container;

        public string Id { get { return _data.Id; } }
        public string Title { get { return _data.Title; } }
        public string Summary { get { return _data.Summary; } }
        public NotificationFlag Type { get { return _data.Type; } }
        public DateTime NoticedDate { get { return _data.NoticedDate; } }
        public async Task MarkAsReadAsync()
        {
            try { await Client.ServiceApi.MarkAsReadAsync(_data, Client); }
            catch (ApiErrorException e)
            { throw new FailToOperationException("既読化の通信に失敗しました。", e); }
        }
        internal virtual void Update(NotificationData data)
        {
            _data = data;
            OnUpdated(new EventArgs());
        }

        public event EventHandler Updated;
        protected virtual void OnUpdated(EventArgs e)
        {
            if (Updated != null)
                Updated(this, e);
        }
    }
}
