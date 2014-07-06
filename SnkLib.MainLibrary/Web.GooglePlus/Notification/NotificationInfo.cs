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
    [Flags]
    public enum NotificationFlag
    {
        CameraSyncUploaded = 0x00000001,
        CircleAddBack = 0x00000002,
        CircleIn = 0x00000004,
        DirectMessage = 0x00000008,
        Followup = 0x00000010,
        InviteHangout = 0x00000020,
        InviteCommunitiy = 0x00000040,
        Mension = 0x00000080,
        NewPhotosAdded = 0x00000100,
        SubscriptionCommunitiy = 0x00000200,
        PlusOne = 0x00000400,
        Reshare = 0x00000800,
        Response = 0x00001000,
        TaggedImage = 0x00002000,
        Unknown = 0x40000000,
    }
}
