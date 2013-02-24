using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Net;
using NUnit.Framework;
using System.Xml.Linq;

namespace EGUpdate {
	public enum AppStatus { Error, OutOfDate, UpToDate, Unknown };
    class AppConfig {
        private string _sRemoteRoot = "";
        private string _sRemoteVerFilePath = "";
        private string _sLocalRoot = "";
        private string _sLocalVerFilePath = "";
        private string _sDesc = "";
        private string _sVerLocal = "";
        private string _sVerRemote = "";
        private DSDelta _dsDelta = null;
        private List<DeltaStep> _dslAllSteps = new List<DeltaStep>();
        private AppStatus _asAppStatus = AppStatus.Unknown;
        private Exception _eLast;
        private Dictionary<string,string> _dPathMap = new Dictionary<string,string>();

        public AppConfig(XElement xe) {
            _sRemoteRoot = xe.Select("remote root").Text().Trim();
            _sRemoteVerFilePath = xe.Select("remote ver").Text().Trim();
            _sLocalRoot = xe.Select("local root").Text().Trim();
            _sLocalVerFilePath = xe.Select("local ver").Text().Trim();
            _sDesc = xe.Select("desc").Text().Trim();
            if (string.IsNullOrEmpty(_sRemoteRoot)) {
                throw new FormatException("Incomplete XML: Missing remote path");
            }
            if (string.IsNullOrEmpty(_sRemoteVerFilePath)) {
                throw new FormatException("Incomplete XML: Missing version file name");
            }
            if (string.IsNullOrEmpty(_sLocalRoot)) {
                throw new FormatException("Incomplete XML: Missing local path");
            }
            if (string.IsNullOrEmpty(_sLocalVerFilePath)) {
                throw new FormatException("Incomplete XML: Missing version file name");
            }
            if (_sRemoteRoot[_sRemoteRoot.Length - 1] == '/') {
                _sRemoteRoot = _sRemoteRoot.Substring(0, _sRemoteRoot.Length - 1);
            }
        }

        public AppStatus GetAppStatus() {
            if (_asAppStatus != AppStatus.Unknown) {
                return _asAppStatus;
            }
            try {
                if (GetRemoteVersion() == GetLocalVersion()) {
                    _asAppStatus = AppStatus.UpToDate;
                } else {
                    _asAppStatus = AppStatus.OutOfDate;
                }
            } catch (Exception e) {
                _eLast = e;
                _asAppStatus = AppStatus.Error;
            }
            return _asAppStatus;
        }

        public string GetRemoteRoot() {
            return _sRemoteRoot;
        }
        public string GetLocalRoot() {
            return _sLocalRoot;
        }
        public string GetDescription() {
            return _sDesc;
        }

        public string GetRemoteVersion() {
            if (string.IsNullOrEmpty(_sVerRemote)) {
                _sVerRemote = HTTP.GetString(GetRemoteVerFilePath());
            }
            return _sVerRemote;
        }

        public string GetLocalVersion() {
            if (string.IsNullOrEmpty(_sVerLocal)) {
                _sVerLocal = File.ReadAllText(GetLocalVerFilePath());
            }
            return _sVerLocal;
        }

        public DSDelta GetDelta() {
            if (_dsDelta != null) {
                return _dsDelta;
            }
            if (GetAppStatus() == AppStatus.OutOfDate) {
                _dsDelta = new DSDelta(this);
                return _dsDelta;
            } else {
                return null;
            }
        }

        public string GetRemoteVerPath() {
            return _sRemoteRoot + "/" + GetRemoteVersion() + "/";
        }

        public string GetRemoteVerFilePath() {
            return _sRemoteRoot + "/" + _sRemoteVerFilePath;
        }

        public string GetLocalVerFilePath() {
            return System.IO.Path.Combine(_sLocalRoot, _sLocalVerFilePath);
        }

        public void AddToPathMap(string sKey, string sValue) {
            _dPathMap.Add(sKey, sValue);
        }

        public string Interpret(string sPath) {
            int iPos;
            int iEndPos;
            int iBracketPos;
            string sKey;
            string sRet;
            iPos = sPath.IndexOf('?');

            if (iPos != -1) {
                sRet = sPath.Substring(0, iPos);
                iEndPos = iPos;
                while (iPos != -1) {
                    sRet += sPath.Substring(iEndPos, iPos - iEndPos);
                    if (sPath[iPos + 1] != '?') {
                        if (sPath.Substring(iPos, 4) == "?app") {
                            sRet += GetLocalRoot();
                            iPos += 3;
                        } else if (sPath.Substring(iPos, 4) == "?new") {
                            sRet += GetRemoteVersion();
                            iPos += 3;
                        } else if (sPath.Substring(iPos, 4) == "?old") {
                            sRet += GetLocalVersion();
                            iPos += 3;
                        } else if (sPath[iPos + 1] == '(') {
                            iBracketPos = sPath.IndexOf(')', iPos + 1);
                            if (iBracketPos == -1) {
                                throw new ArgumentException("'?(' notation is missing closing ')'");
                            } else {
                                sKey = sPath.Substring(iPos+2,iBracketPos - (iPos + 2));
                                if (_dPathMap.ContainsKey(sKey)) {
                                    sRet += _dPathMap[sKey];
                                    iPos = iBracketPos;
                                } else {
                                    throw new ArgumentException("'?(" + sKey + ")' not found in path map");
                                }
                            }
                        }
                    } else {
                        sRet += "?";
                        iPos++;
                    }
                    iEndPos = iPos + 1;
                    iPos = sPath.IndexOf('?', iEndPos);
                }
                if (iEndPos < sPath.Length) {
                    sRet += sPath.Substring(iEndPos);
                }
                return sRet;
            } else {
                return sPath;
            }
        }

        public void AddStep(DeltaStep ds) {
            _dslAllSteps.Add(ds);
        }

        public List<DeltaStep> GetSteps(DeltaStepCode dsc = DeltaStepCode.None) {
            if (dsc == DeltaStepCode.None) {
                return _dslAllSteps;
            } else {
                List<DeltaStep> dslRet = new List<DeltaStep>();
                foreach (DeltaStep ds in _dslAllSteps) {
                    if (ds.GetStepCode() == dsc) {
                        dslRet.Add(ds);
                    }
                }
                return dslRet;
            }
        }

        public override string ToString() {
            return "AppConfig: " + _sDesc;
        }

        public Exception GetLastError() {
            return _eLast;
        }

    }


    [TestFixture]
    public class AppConfigTest {

        [Test]
        public void Test() {
            XDocument xd = XDocument.Load("testAppConfig.xml");
            AppConfig ac = new AppConfig(xd.Element("app"));
            Assert.AreEqual("http://localhost/dev/updater/testapp", ac.GetRemoteRoot());
            Assert.AreEqual(@"C:\Users\User\Desktop\updatertest", ac.GetLocalRoot());
            Assert.AreEqual(@"C:\Users\User\Desktop\updatertest\version.txt", ac.GetLocalVerFilePath());
            Assert.AreEqual("Test App", ac.GetDescription());
            Assert.AreEqual("v1.0", ac.GetLocalVersion());
            Assert.AreEqual("v2.0", ac.GetRemoteVersion());
            Assert.AreEqual(AppStatus.OutOfDate, ac.GetAppStatus());
        }

        [Test]
        public void TestInterpretation() {
            XDocument xd = XDocument.Load("testAppConfig.xml");
            AppConfig ac = new AppConfig(xd.Element("app"));
            Assert.AreEqual(@"C:\Users\User\Desktop\updatertest", ac.Interpret(@"?app"));
            Assert.AreEqual(@"C:\Users\User\Desktop\updatertest", ac.Interpret(@"C:\Users\User\Desktop\updatertest"));
            Assert.AreEqual(@"merp.exe", ac.Interpret(@"merp.exe"));
            Assert.AreEqual(@"v1.0", ac.Interpret(@"?old"));
            Assert.AreEqual(@"v2.0", ac.Interpret(@"?new"));
            Assert.AreEqual(@"?new?old", ac.Interpret(@"??new??old"));
            Assert.AreEqual(@"??new?old", ac.Interpret(@"????new??old"));
            Assert.AreEqual(@"C:\Users\User\Desktop\updatertest\v1.0", ac.Interpret(@"?app\?old"));
            Assert.AreEqual(@"?app\v1.0", ac.Interpret(@"??app\?old"));
            Assert.AreEqual(@"?app\?old", ac.Interpret(@"??app\??old"));
            Assert.AreEqual(@"C:\Users\User\Desktop\updatertest\v2.0", ac.Interpret(@"?app\?new"));
        }
    }
}
