using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    [Stubable]
    public class CircleData : CoreData
    {
        public CircleData(string id, string name, ProfileData[] members)
        {
            Id = id;
            Name = name;
            Members = members;
        }
        [Identification]
        public readonly string Id;
        public readonly string Name;
        public readonly ProfileData[] Members;
    }
}
