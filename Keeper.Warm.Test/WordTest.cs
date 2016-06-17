using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keeper.Warm.Test
{
    [TestClass]
    public class WordTest
    {
        [TestMethod]
        public void CanUnionInt64ToInt32()
        {
            var target = new Word()
            {
                Int64 = 0x1234
            };

            Assert.AreEqual(0x1234, target.Int32);
        }
    }
}