using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class CircleData
    {
        public CircleData(string id, string name, string[] members)
        {
            Id = id;
            Name = name;
            Members = members;
        }
        public readonly string Id;
        public readonly string Name;
        public readonly string[] Members;
    }
}
