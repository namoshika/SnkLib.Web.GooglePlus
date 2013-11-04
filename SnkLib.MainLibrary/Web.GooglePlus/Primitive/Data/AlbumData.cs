using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class AlbumData : DataBase
    {
        public AlbumData(string id, string name = null, Uri albumUrl = null, DateTime? createDate = null,
            ImageData[] bookCovers = null, ImageData[] images = null, string attachedActivity = null, ProfileData owner = null,
            bool? isUpdatedAlbum = null, bool? isUpdatedAlbumComments = null)
        {
            Id = id;
            Name = name;
            AlbumUrl = albumUrl;
            CreateDate = createDate;
            BookCovers = bookCovers;
            Images = images;
            AttachedActivity = attachedActivity;
            Owner = owner;
            IsUpdatedAlbum = false;
            IsUpdatedAlbumComments = false;
        }
        public bool IsUpdatedAlbum { get; private set; }
        public bool IsUpdatedAlbumComments { get; private set; }
        public string Id { get; private set; }
        public string Name { get; private set; }
        public Uri AlbumUrl { get; private set; }
        public DateTime? CreateDate { get; private set; }
        public ImageData[] BookCovers { get; private set; }
        public ImageData[] Images { get; private set; }
        public string AttachedActivity { get; private set; }
        public ProfileData Owner { get; private set; }

        public static AlbumData operator +(AlbumData value1, AlbumData value2)
        {
            var aaa = (value1 != null ? value1.IsUpdatedAlbum : false) || (value2 != null ? value2.IsUpdatedAlbumComments : false);
            var bbb = (value1 != null ? value1.IsUpdatedAlbumComments : false) || (value2 != null ? value2.IsUpdatedAlbumComments : false);
            return new AlbumData(
                Merge(value1, value2, obj => obj.Id),
                Merge(value1, value2, obj => obj.Name),
                Merge(value1, value2, obj => obj.AlbumUrl),
                Merge(value1, value2, obj => obj.CreateDate),
                Merge(value1, value2, obj => obj.BookCovers),
                Merge(value1, value2, obj => obj.Images),
                Merge(value1, value2, obj => obj.AttachedActivity),
                Merge(value1, value2, obj => obj.Owner),
                aaa, bbb);
        }
    }
}
