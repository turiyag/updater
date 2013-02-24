using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NUnit.Framework;
using System.Xml;

namespace EGUpdate {
    public static class XContainerExtensions {
        //public static IEnumerable<XElement> Select(this IEnumerable<XElement> xeenum, string sSelector) {
        //    List<XElement> xelReturn = new List<XElement>();
        //    foreach (XElement xe in xeenum) {
        //        xelReturn.AddRange(xe.Select(sSelector));
        //    }
        //    return xelReturn;
        //}

        //Currently only implements comma and space separators with class ('.'), 
        //  id ('#'), attribute ('[name]'), attribute value ('[name=value]') and name selectors
        public static IEnumerable<XElement> Select(this XContainer xd, string sSelector) {
            HashSet<XElement> xelReturn = new HashSet<XElement>();
            if (sSelector.Contains(',')) {
                sSelector.Replace(", ", ",");
                foreach (string s in sSelector.Split(',')) {
                    xelReturn.AddRange(xd.Select(s));
                }
            } else {
                int iIndex = sSelector.IndexOf(" ");
                if (iIndex != -1) {
                    string sSelectorHead = sSelector.Substring(0, iIndex);
                    string sSelectorTail = sSelector.Substring(iIndex + 1);
                    foreach (XElement xe in xd.Elements()) {
                        if (xe.Matches(sSelectorHead)) {
                            xelReturn.AddRange(xe.Select(sSelectorTail));
                        } else {
                            xelReturn.AddRange(xe.Select(sSelector));
                        }
                    }
                } else {
                    foreach (XElement xe in xd.Elements()) {
                        if (xe.Matches(sSelector)) {
                            xelReturn.Add(xe);
                        } else {
                            xelReturn.AddRange(xe.Select(sSelector));
                        }
                    }
                }
            }
            return xelReturn;
        }

        public static bool Matches(this XElement xe, string sSelector) {
            if (string.IsNullOrWhiteSpace(sSelector)) {
                return false;
            }
            string sFirst = sSelector.Substring(0, 1);
            if (sSelector == "*") {
                return true;
            } else {
                switch (sFirst) {
                    case ".":
                        string sClassAttr = xe.Attr("class");
                        sSelector = sSelector.ToLower().Substring(1);
                        if (string.IsNullOrWhiteSpace(sClassAttr)) {
                            return false;
                        } else {
                            string[] saClasses = sClassAttr.ToLower().Split(' ');
                            foreach (string sClass in saClasses) {
                                if (sClass == sSelector) {
                                    return true;
                                }
                            }
                            return false;
                        }
                    case "#":
                        string sID = xe.Attr("id");
                        if (string.IsNullOrWhiteSpace(sID)) {
                            return false;
                        }
                        sSelector = sSelector.ToLower().Substring(1);
                        return sID.ToLower() == sSelector;
                    case "[":
                        if (sSelector.Substring(sSelector.Length - 1) != "]") {
                            throw new ArgumentException("Opening square bracket has no closing bracket in selector: " + sSelector);
                        } else {
                            string[] saNameValue = sSelector.Substring(1, sSelector.Length - 2).Split('=');
                            if (saNameValue.Length == 1) {
                                return xe.Attr(saNameValue[0]) != null;
                            } else if (saNameValue.Length == 2) {
                                return xe.Attr(saNameValue[0]) == saNameValue[1];
                            } else {
                                throw new ArgumentException("Incorrect format in selector: " + sSelector);
                            }
                        }
                    default:
                        return xe.Name.ToString().ToLower() == sSelector;
                }
            }
        }
        public static string Attr(this XElement xe, string sName, string sDefault = null) {
            XAttribute xa = xe.Attribute(sName);
            if (xa == null) {
                return sDefault;
            } else {
                return xa.Value;
            }
        }

        public static string Attr(this IEnumerable<XElement> xel, string sName, string sDefault = null) {
            return xel.ElementAt(0).Attr(sName, sDefault);
        }

        public static string RequiredAttr(this XElement xe, string sName) {
            XAttribute xa = xe.Attribute(sName);
            if (xa == null) {
                Program.FailAndDie("Required attribute '" + sName + "' not found in <" + xe.Name() + " />");
                return null;
            } else {
                if (string.IsNullOrWhiteSpace(xa.Value)) {
                    Program.FailAndDie("Required attribute '" + sName + "' empty in <" + xe.Name() + " />");
                    return null;
                } else {
                    return xa.Value;
                }
            }
        }

        public static string RequiredAttr(this IEnumerable<XElement> xel, string sName) {
            return xel.ElementAt(0).RequiredAttr(sName);
        }

        public static string Text(this XElement xe) {
            XmlReader xmlr = xe.CreateReader();
            xmlr.MoveToContent();
            return xmlr.ReadInnerXml();
        }

        public static string Text(this IEnumerable<XElement> xel) {
            return xel.ElementAt(0).Text();
        }

        public static string Name(this XElement xe) {
            return xe.Name.ToString();
        }

        public static string Name(this IEnumerable<XElement> xel) {
            return xel.ElementAt(0).Name.ToString();
        }

    }
    
    [TestFixture]
    public class XContainerExtensionsTest {
        private string sXML = @"<?xml version=""1.0""?> <app> <player id=""mario"" color=""red"" ismain=""true"">The Main Protagonist</player> <player id=""luigi"" color=""green"" /> <desc>Test App</desc> <monsters> <boo id=""m1"" name=""nick"" invisible=""true"" /> <koopa id=""m2"" name=""joe"" /> <boo id=""m3"" name=""calvin"" /> </monsters> </app>";

        [Test]
        public void Test() {
            XDocument xd = XDocument.Load(new StringReader(sXML));
            //Players
            IEnumerable<XElement> xcenum = xd.Select("app player");
            Assert.AreEqual("The Main Protagonist", xcenum.Text());
            Assert.AreEqual("mario", xcenum.Attr("id"));
            Assert.AreEqual("red", xcenum.ElementAt(0).Attr("color"));
            Assert.AreEqual("luigi", xcenum.ElementAt(1).Attr("id"));
            Assert.AreEqual("green", xcenum.ElementAt(1).Attr("color"));
            Assert.AreEqual(null, xcenum.ElementAt(1).Attr("class"));
            Assert.AreEqual(null, xcenum.ElementAt(1).Attr("skill"));
            Assert.AreEqual(2, xcenum.Count());
            //Monsters
            xcenum = xd.Select("app monsters *");
            Assert.AreEqual("boo", xcenum.ElementAt(0).Name.ToString());
            Assert.AreEqual("nick", xcenum.ElementAt(0).Attr("name"));
            Assert.AreEqual("koopa", xcenum.ElementAt(1).Name.ToString());
            Assert.AreEqual("joe", xcenum.ElementAt(1).Attr("name"));
            Assert.AreEqual("boo", xcenum.ElementAt(2).Name.ToString());
            Assert.AreEqual("calvin", xcenum.ElementAt(2).Attr("name"));
            //Boo names
            foreach (XElement xeBoo in xd.Select("app monsters boo")) {
                Console.WriteLine(xeBoo.Attr("id"));
            }
            //Attribute selector
            xcenum = xd.Select("app monsters [name=nick]");
            Assert.AreEqual(1, xcenum.Count());
            Assert.AreEqual("boo", xcenum.ElementAt(0).Name.ToString());
            Assert.AreEqual("true", xcenum.ElementAt(0).Attr("invisible"));
            //Attribute selector
            xcenum = xd.Select("monsters [invisible]");
            Assert.AreEqual(1, xcenum.Count());
            Assert.AreEqual("boo", xcenum.Name());
            Assert.AreEqual("true", xcenum.ElementAt(0).Attr("invisible"));
            //ID Selector
            xcenum = xd.Select("#m3");
            Assert.AreEqual(1, xcenum.Count());
            Assert.AreEqual("boo", xcenum.Name());
            Assert.AreEqual("boo", xcenum.ElementAt(0).Name());
            Assert.AreEqual("calvin", xcenum.ElementAt(0).Attr("name"));
            //Done
            Console.WriteLine("Done!");
        }
    }
}