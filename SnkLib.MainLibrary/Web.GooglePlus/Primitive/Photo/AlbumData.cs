using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    [Stubable]
    public class AlbumData : CoreData
    {
        public AlbumData(string id, string name = null, Uri albumUrl = null, DateTime? createDate = null,
            ImageData[] bookCovers = null, ImageData[] images = null, string attachedActivity = null,
            ProfileData owner = null, AlbumUpdateApiFlag loadedApiTypes = AlbumUpdateApiFlag.Unloaded)
        {
            Id = id;
            Name = name;
            AlbumUrl = albumUrl;
            CreateDate = createDate;
            BookCovers = bookCovers;
            Images = images;
            AttachedActivity = attachedActivity;
            Owner = owner;
            LoadedApiTypes = loadedApiTypes;
        }
        public readonly AlbumUpdateApiFlag LoadedApiTypes;
        [Identification]
        public readonly string Id;
        public readonly string Name;
        public readonly Uri AlbumUrl;
        public readonly DateTime? CreateDate;
        public readonly ImageData[] BookCovers;
        public readonly ImageData[] Images;
        public readonly string AttachedActivity;
        public readonly ProfileData Owner;

        public static AlbumData operator +(AlbumData value1, AlbumData value2)
        {
            bool val1_isNotNull = value1 != null, val2_isNotNull = value2 != null;
            if (val1_isNotNull && val2_isNotNull)
            {
                var newUpdateType =
                    (value2 != null ? value2.LoadedApiTypes : AlbumUpdateApiFlag.Unloaded)
                    | (value1 != null ? value1.LoadedApiTypes : AlbumUpdateApiFlag.Unloaded);

                return new AlbumData(
                    Merge(value1, value2, obj => obj.Id),
                    Merge(value1, value2, obj => obj.Name),
                    Merge(value1, value2, obj => obj.AlbumUrl),
                    Merge(value1, value2, obj => obj.CreateDate),
                    Merge(value1, value2, obj => obj.BookCovers),
                    Merge(value1, value2, obj => obj.Images),
                    Merge(value1, value2, obj => obj.AttachedActivity),
                    Merge(value1, value2, obj => obj.Owner),
                    newUpdateType);
            }
            else if (val2_isNotNull)
                return value2;
            else
                return value1;
        }
    }
    [Flags]
    public enum AlbumUpdateApiFlag
    { Unloaded = 0, Base = 1, Albums = 2, Full = 5 }
}
