using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    static class Utility
    {
        public static void AssertException<TException>(Func<object> proc) where TException : Exception
        {
            try
            {
                proc();
                Assert.Fail("{0}例外が発生しませんでした。", typeof(TException).Name);
            }
            catch (TException) { }
        }
    }
}
