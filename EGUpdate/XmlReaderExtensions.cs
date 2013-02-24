using System;
using System.Xml;
using System.IO;
using NUnit.Framework;

namespace EGUpdate {
    public static class XmlReaderExtensions {
        public static bool ReadToNextSibling(this XmlReader xmlr) {
            int i = 1;
            if (xmlr.IsEmptyElement) {
                xmlr.Read();
                while (xmlr.NodeType != XmlNodeType.Element) {
                    if (!xmlr.Read()) return false;
                }
                return true;
            }
            if (xmlr.NodeType == XmlNodeType.Element) xmlr.Read();
            while (true) {
                if (xmlr.NodeType == XmlNodeType.Element) i++;
                if (xmlr.NodeType == XmlNodeType.EndElement) i--;
                if (xmlr.Read()) {
                    if (i == 0) {
                        while (xmlr.NodeType != XmlNodeType.Element) {
                            if (!xmlr.Read()) return false;
                        }
                        return true;
                    }
                } else {
                    return false;
                }
            }
        }

        public static string Attr(this XmlReader xmlr, string sName) {
            if (xmlr.MoveToAttribute(sName)) {
                return xmlr.Value;
            } else {
                return null;
            }
        }
    }

    [TestFixture]
    public class XMLReaderExtensionsTest {
        private string sXML = @"<?xml version=""1.0""?> <app> <remote boo=""ghost"" mario=""human"">http://home.edgemontgeek.com/dev/updater/testapp/</remote> <local is=""5"" was=""7"" willbe=""9"" /> <desc>Test App</desc> </app>";

        [Test]
        public void Test() {
            XmlReader xmlr = XmlTextReader.Create(new StringReader(sXML));
            xmlr.Read();
            xmlr.Read();
            xmlr.Read();
            xmlr.Read();
            while (xmlr.NodeType != XmlNodeType.Element) {
                if (!xmlr.Read()) break;
            }
            do {
                Console.Error.WriteLine(xmlr.Name);
                if (xmlr.Name == "local") {
                    Assert.AreEqual("7", xmlr.Attr("was"));
                    Assert.AreEqual("5", xmlr.Attr("is"));
                    Assert.AreEqual("9", xmlr.Attr("willbe"));
                    Assert.AreEqual(null, xmlr.Attr("boo"));
                    Assert.AreEqual("7", xmlr.Attr("was"));
                    Assert.AreEqual("5", xmlr.Attr("is"));
                    Assert.AreEqual("9", xmlr.Attr("willbe"));
                    Assert.AreEqual(null, xmlr.Attr("boo"));
                    Assert.AreEqual(null, xmlr.Attr("boo"));
                    Assert.AreEqual("9", xmlr.Attr("willbe"));
                    Assert.AreEqual("9", xmlr.Attr("willbe"));
                    Assert.AreEqual(null, xmlr.Attr("goo"));
                }
                if (xmlr.Name == "remote") {
                    Assert.AreEqual("ghost", xmlr.Attr("boo"));
                    Assert.AreEqual("human", xmlr.Attr("mario"));
                    Assert.AreEqual(null, xmlr.Attr("goo"));

                }
            } while (xmlr.ReadToNextSibling());
        }
    }
}
