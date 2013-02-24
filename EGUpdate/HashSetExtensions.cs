using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace EGUpdate {

    public static class HashSetExtensions {
        public static void AddRange<T>(this ICollection<T> ienum1, IEnumerable<T> ienum2) {
            foreach (var item in ienum2) {
                ienum1.Add(item);
            }
        }
    }
    [TestFixture]
    public class HashSetExtensionsTest {

        [Test]
        public void Test() {
            var hs1 = new HashSet<string> { "one", "two", "three" };
            var hs2 = new HashSet<string> { "four", "five" };
            hs2.AddRange(hs1);
            Assert.IsTrue(hs2.Contains("one"));
            Assert.IsTrue(hs2.Contains("two"));
            Assert.IsTrue(hs2.Contains("three"));
            Assert.IsTrue(hs2.Contains("four"));
            Assert.IsTrue(hs2.Contains("five"));
            Assert.IsFalse(hs2.Contains("nine"));
            Assert.IsFalse(hs1.Contains("four"));
            Assert.IsFalse(hs1.Contains("five"));
            Assert.IsFalse(hs1.Contains("nine"));
        }
    }
}
