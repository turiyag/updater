using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.Win32;

namespace EGUpdate {
    public enum DeltaStepStatus {
        Error,
        Initial,
        Waiting,
        Running,
        Complete
    };
    public enum DeltaStepCautionLevel {
        Die,
        Safe,
        Skip,
        Force
    };
    public enum DeltaStepCode {
        Admin,
        Delta,
        Set,
        Exec,
        Wait,
        Move,
        Get,
        Copy,
        Del,
        MkDir,
        Kill,
        Startup,
        None
    };
    public class DeltaStep {
        protected DeltaStepStatus _dssStatus = DeltaStepStatus.Initial;
        protected DeltaStepCode _dscCode = DeltaStepCode.None;
        protected string _sXML;
        protected AppConfig _acApp;
        protected DeltaStep _dsParent;
        protected string _sRemote;
        protected string _sLocal;
        protected DeltaStepCautionLevel _dsclCaution;
        protected string _sID;
        protected int _iStepIndex;

        public void Run() {
            _dssStatus = DeltaStepStatus.Running;
            Message("Running");
            System.Console.ReadLine();
            try {
                Attempt();
            } catch (Exception e) {
                _dssStatus = DeltaStepStatus.Error;
                Message("Error [" + e.Message + "]");
                try {
                    switch (_dsclCaution) {
                        case DeltaStepCautionLevel.Safe:
                            Message("Handling safely");
                            Safe(e);
                            Message("Handled safely");
                            break;
                        case DeltaStepCautionLevel.Skip:
                            Message("Skipping this step");
                            Skip(e);
                            Message("Skipped this step");
                            break;
                        case DeltaStepCautionLevel.Force:
                            Message("Attempting to force");
                            Force(e);
                            Message("Forced");
                            break;
                        case DeltaStepCautionLevel.Die:
                            Message("Dying");
                            Die(e);
                            Message("Dead");
                            break;
                    }
                } catch (Exception eHandling) {
                    Message("Handling failed on error: " + eHandling.ToString());
                    Program.FailAndDie("");
                }
            }
            Message("Complete");
            _dssStatus = DeltaStepStatus.Complete;
        }
        public virtual void Attempt() {
        }
        public virtual void Safe(Exception e) {
        }
        public virtual void Skip(Exception e) {
        }
        public virtual void Force(Exception e) {
        }
        public virtual void Die(Exception e) {
            Message("Fatal error: " + e.Message);
            Program.FailAndDie("");
        }
        protected void Message(string sMsg) {
            System.Console.WriteLine("(" + _iStepIndex + "/" + _acApp.GetStepCount() + ") - " + ToString() + ": " + sMsg);
        }
        public virtual List<DeltaStep> GetChildSteps(DeltaStepCode dscCode = DeltaStepCode.None) {
            return new List<DeltaStep>();
        }
        public virtual List<DeltaStep> GetDescendantSteps(DeltaStepCode dscCode = DeltaStepCode.None) {
            List<DeltaStep> dsl = GetChildSteps();
            List<DeltaStep> dslRet = new List<DeltaStep>();
            if (dscCode == DeltaStepCode.None) {
                foreach (DeltaStep ds in dsl) {
                    dslRet.Add(ds);
                    dslRet.AddRange(ds.GetDescendantSteps(dscCode));
                }
            } else {
                foreach (DeltaStep ds in dsl) {
                    if (ds.GetStepCode() == dscCode) {
                        dslRet.Add(ds);
                    }
                    dslRet.AddRange(ds.GetDescendantSteps(dscCode));
                }
            }
            return dslRet;
        }

        //Nerfed Select function, only works with ID selector ("#theid") and name ("exec")
        //Also recognizes descendant ("#deltests del") and multiples ("#deltests, #copytests")
        public IEnumerable<DeltaStep> Select(string sSelector) {
            HashSet<DeltaStep> hsdsReturn = new HashSet<DeltaStep>();
            if (sSelector.Contains(',')) {
                sSelector = sSelector.Replace(", ", ",");
                foreach (string s in sSelector.Split(',')) {
                    hsdsReturn.AddRange(Select(s));
                }
            } else {
                int iIndex = sSelector.IndexOf(" ");
                if (iIndex != -1) {
                    string sSelectorHead = sSelector.Substring(0, iIndex);
                    string sSelectorTail = sSelector.Substring(iIndex + 1);
                    foreach (DeltaStep ds in GetChildSteps()) {
                        if (ds.Matches(sSelectorHead)) {
                            hsdsReturn.AddRange(ds.Select(sSelectorTail));
                        } else {
                            hsdsReturn.AddRange(ds.Select(sSelector));
                        }
                    }
                } else {
                    foreach (DeltaStep ds in GetChildSteps()) {
                        if (ds.Matches(sSelector)) {
                            hsdsReturn.Add(ds);
                        } else {
                            hsdsReturn.AddRange(ds.Select(sSelector));
                        }
                    }
                }
            }
            return hsdsReturn;
        }

        //Nerfed Select function, only works with ID selector ("#theid") and name ("exec")
        public bool Matches(string sSelector) {
            if (string.IsNullOrWhiteSpace(sSelector)) {
                return false;
            }
            string sFirst = sSelector.Substring(0, 1);
            if (sSelector == "*") {
                return true;
            } else {
                sSelector = sSelector.ToLower();
                switch (sFirst) {
                    case "#":
                        if (string.IsNullOrWhiteSpace(_sID)) {
                            return false;
                        }
                        sSelector = sSelector.Substring(1);
                        return _sID.ToLower() == sSelector;
                    default:
                        return _dscCode.ToString().ToLower() == sSelector;
                }
            }
        }

        public void Initialize(XElement xe, DeltaStep dsParent, string sType) {
            string sCautionLevel;
            _dssStatus = DeltaStepStatus.Initial;
            if (xe.Name() != sType) {
                throw new ArgumentException("XElement input is not a " + sType + " step");
            }
            _dscCode = (DeltaStepCode)(Enum.Parse(typeof(DeltaStepCode), sType, true));
            _sXML = xe.Text();
            _sID = xe.Attr("id");
            _dsParent = dsParent;
            _acApp = dsParent._acApp;
            _sRemote = dsParent._sRemote;
            _sLocal = dsParent._sLocal;
            sCautionLevel = xe.Attr("caution");
            if (string.IsNullOrWhiteSpace(sCautionLevel)) {
                _dsclCaution = dsParent.GetCautionLevel();
            } else {
                try {
                    _dsclCaution = (DeltaStepCautionLevel)(Enum.Parse(typeof(DeltaStepCautionLevel), sCautionLevel, true));
                } catch (Exception) {
                    _dsclCaution = DeltaStepCautionLevel.Die;
                }
            }
            _acApp.AddStep(this);
            _iStepIndex = _acApp.GenerateStepIndex();
        }

        public static DeltaStep Parse(XElement xe, DeltaStep dsParent, AppConfig acApp) {
            DeltaStep ds;
            switch (xe.Name()) {
                case "set":
                    ds = new DSSet(xe, dsParent, acApp);
                    break;
                case "exec":
                    ds = new DSExec(xe, dsParent, acApp);
                    break;
                case "wait":
                    ds = new DSWait(xe, dsParent, acApp);
                    break;
                case "move":
                    ds = new DSMove(xe, dsParent, acApp);
                    break;
                case "get":
                    ds = new DSGet(xe, dsParent, acApp);
                    break;
                case "copy":
                    ds = new DSCopy(xe, dsParent, acApp);
                    break;
                case "del":
                    ds = new DSDel(xe, dsParent, acApp);
                    break;
                case "mkdir":
                    ds = new DSMkDir(xe, dsParent, acApp);
                    break;
                case "kill":
                    ds = new DSKill(xe, dsParent, acApp);
                    break;
                case "startup":
                    ds = new DSStartup(xe, dsParent, acApp);
                    break;
                case "admin":
                    ds = new DSAdmin(xe, dsParent, acApp);
                    break;
                default:
                    throw new InvalidDataException("Undefined XML name: <" + xe.Name + "/>");
            }
            return ds;
        }

        public static string SafePath(string sPath) {
            int iCount = 2;
            string sPathNoExt = Path.Combine(Path.GetDirectoryName(sPath), Path.GetFileNameWithoutExtension(sPath));
            string sExt = Path.GetExtension(sPath);
            if (File.Exists(sPath) || Directory.Exists(sPath)) {
                while (File.Exists(sPathNoExt + " (" + iCount + ")" + sExt) || Directory.Exists(sPathNoExt + " (" + iCount + ")" + sExt)) {
                    iCount++;
                }
                return sPathNoExt + " (" + iCount + ")" + sExt;
            } else {
                return sPath;
            }
        }

        public static string SafeRegistryValueName(RegistryKey rkKey, string sValueName) {
            int iCount = 2;
            if (rkKey.GetValue(sValueName) != null) {
                while (rkKey.GetValue(sValueName + " (" + iCount + ")") != null) {
                    iCount++;
                }
                return sValueName + " (" + iCount + ")";
            } else {
                return sValueName;
            }
        }

        public string FullLocalPath(string sPath, string sParentPath = null) {
            if (sParentPath == null) {
                sParentPath = _sLocal;
            }
            if (Path.IsPathRooted(sPath)) {
                return sPath;
            } else {
                return Path.Combine(sParentPath, sPath);
            }
        }

        public string FullRemotePath(string sPath, string sParentPath = null) {
            if (sParentPath == null) {
                sParentPath = _sRemote;
            }
            if (sPath.Substring(0, 4) == "http") {
                return sPath;
            } else {
                if (sParentPath[sParentPath.Length - 1] == '/') {
                    if (sPath[0] == '/') {
                        return sParentPath + sPath.Substring(1);
                    } else {
                        return sParentPath + sPath;
                    }
                } else {
                    if (sPath[0] == '/') {
                        return sParentPath + sPath;
                    } else {
                        return sParentPath + "/" + sPath;
                    }
                }
            }
        }


        public DeltaStepStatus GetStepStatus() {
            return _dssStatus;
        }
        public DeltaStepCode GetStepCode() {
            return _dscCode;
        }
        public string GetRemote() {
            return _sRemote;
        }
        public string GetLocal() {
            return _sLocal;
        }
        public string GetID() {
            return _sID;
        }
        public DeltaStep GetParent() {
            return _dsParent;
        }
        public DeltaStepCautionLevel GetCautionLevel() {
            return _dsclCaution;
        }
        public override string ToString() {
            if (string.IsNullOrWhiteSpace(_sID)) {
                return "<" + _dscCode.ToString() + " />";
            } else {
                return "<" + _dscCode.ToString() + " id=\"" + _sID + "\" />";
            }
        }
    }

    [TestFixture]
    public class DeltaStepTest {

        public void FullTest() {
            DSDelta dsdDelta = DeltaStepTest.GetTestDelta();
            dsdDelta.Run();
        }

        [Test]
        public void LoadXML() {
            Assert.True(File.Exists("testDeltaStep.xml"));
        }

        [Test]
        public void TestPathBuilders() {
            DSDelta dsd = DeltaStepTest.GetTestDelta();
            Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\version (2).txt", DeltaStep.SafePath(AppConfigTest.sLocalTestPath + @"\version.txt"));
            Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\merp.txt", DeltaStep.SafePath(AppConfigTest.sLocalTestPath + @"\merp.txt"));
            Assert.AreEqual(@"C:\merp.txt", dsd.FullLocalPath(@"merp.txt", @"C:\"));
            Assert.AreEqual(@"D:\merp.txt", dsd.FullLocalPath(@"D:\merp.txt", @"C:\herp"));
            Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\merp.txt", dsd.FullLocalPath(@"merp.txt"));

            Assert.AreEqual(AppConfigTest.sRemoteTestPath + @"/merp.txt", dsd.FullRemotePath(@"merp.txt"));
            Assert.AreEqual(AppConfigTest.sRemoteTestPath + @"/merp.txt", dsd.FullRemotePath(@"/merp.txt"));
            Assert.AreEqual(AppConfigTest.sRemoteTestPath + @"/merp.txt", dsd.FullRemotePath(@"merp.txt", AppConfigTest.sRemoteTestPath));
            Assert.AreEqual(AppConfigTest.sRemoteTestPath + @"/merp.txt", dsd.FullRemotePath(@"/merp.txt", AppConfigTest.sRemoteTestPath));
            Assert.AreEqual(AppConfigTest.sRemoteTestPath + @"/merp.txt", dsd.FullRemotePath(@"merp.txt", AppConfigTest.sRemoteTestPath + "/"));
            Assert.AreEqual(AppConfigTest.sRemoteTestPath + @"/merp.txt", dsd.FullRemotePath(@"/merp.txt", AppConfigTest.sRemoteTestPath + "/"));
        }

        [Test]
        public void TestSelect() {
            DSDelta dsd = DeltaStepTest.GetTestDelta();
            IEnumerable<DeltaStep> ids = dsd.Select("#getforce");
            Assert.AreEqual(1, ids.Count());
            Assert.AreEqual(DeltaStepCode.Get, ids.ElementAt(0).GetStepCode());
            ids = dsd.Select("#getforce, #getdie");
            Assert.AreEqual(2, ids.Count());
            ids = dsd.Select("exec");
            Assert.Greater(ids.Count(), 5);
            ids = dsd.Select("#getforce, #getdie,exec");
            Assert.Greater(ids.Count(), 7);
            ids = dsd.Select("#deltests copy");
            Assert.AreEqual(2, ids.Count());
            ids = dsd.Select("#deltests del");
            Assert.AreEqual(2, ids.Count());
            ids = dsd.Select("#deltests copy,#deltests del");
            Assert.AreEqual(4, ids.Count());
        }

        [Test]
        public void TestSafeRegPath() {
            RegistryKey rkLMMWC = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion", false);
            Assert.AreEqual("DevicePath (2)", DeltaStep.SafeRegistryValueName(rkLMMWC, "DevicePath"));
            Assert.AreEqual("NotHere", DeltaStep.SafeRegistryValueName(rkLMMWC, "NotHere"));
        }

        public static DSDelta GetTestDelta() {
            XDocument xd = XDocument.Load("testAppConfig.xml");
            AppConfig ac = new AppConfig(xd.Element("app"));
            return ac.GetDelta();
        }



    }
}
