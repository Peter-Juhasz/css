using EditorTest.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Css.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Assert.IsTrue(CssWebData.Index.NamedColorsSorted.Count > 0);
        }
    }
}
