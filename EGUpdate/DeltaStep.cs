using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

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
        Set,
        Fetch,
        Get,
        In,
        Move,
        Copy,
        Del,
        Delta,
        Exec,
        Kill,
        Wait,
        MkDir,
        None
    };
    class DeltaStep {
        protected DeltaStepStatus _dssStatus = DeltaStepStatus.Initial;
        protected DeltaStepCode _dscCode = DeltaStepCode.None;
        protected string _sXML;
        protected AppConfig _acApp;
        protected DeltaStep _dsParent;
        protected string _sRemote;
        protected string _sLocal;
        protected DeltaStepCautionLevel _dsclCaution;
        protected string _sID;

        public void Run() {
            _dssStatus = DeltaStepStatus.Running;
            System.Console.WriteLine("Running: " + ToString());
            try {
                Attempt();
            } catch (Exception e) {
                _dssStatus = DeltaStepStatus.Error;
                System.Console.Error.WriteLine("Primary error: " + e.Message);
                try {
                    switch (_dsclCaution) {
                        case DeltaStepCautionLevel.Safe:
                            System.Console.Error.WriteLine("Handling safely");
                            Safe(e);
                            break;
                        case DeltaStepCautionLevel.Skip:
                            System.Console.Error.WriteLine("Skipping this step");
                            Skip(e);
                            break;
                        case DeltaStepCautionLevel.Force:
                            System.Console.Error.WriteLine("Attempting to force");
                            Force(e);
                            break;
                        case DeltaStepCautionLevel.Die:
                            System.Console.Error.WriteLine("Dying");
                            Die(e);
                            break;
                    }
                } catch (Exception eHandling) {
                    Program.FailAndDie("Handling failed on error: " + eHandling.ToString());
                }
            }
            System.Console.WriteLine("Complete: " + ToString());
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
            Program.FailAndDie("Fatal error in element: " + e.Message);
        }
        public virtual List<DeltaStep> GetSubSteps() {
            return new List<DeltaStep>();
        }
        public int CountSteps() {
            List<DeltaStep> dsl = GetSubSteps();
            int iCount = 1;
            foreach (DeltaStep ds in dsl) {
                iCount += ds.CountSteps();
            }
            return iCount;
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
            System.Console.WriteLine("Ctn: " + dsParent._dsclCaution.ToString());
            sCautionLevel = xe.Attr("caution");
            try {
                _dsclCaution = (DeltaStepCautionLevel)(Enum.Parse(typeof(DeltaStepCautionLevel), sCautionLevel, true));
            } catch (Exception) {
                _dsclCaution = DeltaStepCautionLevel.Die;
            }
            _acApp.AddStep(this);
        }

        public static DeltaStep Parse(XElement xe, DeltaStep dsParent, AppConfig acApp) {
            System.Console.WriteLine("Parsing: " + xe.Name());
            DeltaStep ds;
            switch (xe.Name()) {
                case "exec":
                    ds = new DSExec(xe, dsParent, acApp);
                    break;
                case "set":
                    ds = new DSSet(xe, dsParent, acApp);
                    break;
                case "wait":
                    ds = new DSWait(xe, dsParent, acApp);
                    break;
                default:
                    ds = null;
                    break;
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
            if (sPath.Substring(0,4) == "http") {
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
        [Test]
        public void MiniTest() {
            //XDocument xdApp = XDocument.Load("testAppConfig.xml");
            //AppConfig ac = new AppConfig(xdApp.Element("app"));
            //XDocument xdDelta = XDocument.Load("testDeltaStep.xml");
            //XElement xeDelta = xdDelta.Element("delta");
            //foreach (XElement xe in xeDelta.Elements()) {
            //    DeltaStep ds = DeltaStep.Parse(xe, dc);
            //    ds.Attempt();
            //}
        }

        [Test]
        public void LoadXML() {
            Assert.AreEqual(true, File.Exists("testDeltaStep.xml"));
        }

        [Test]
        public void TestPathBuilders() {
            Assert.AreEqual(@"C:\Users\User\Desktop\updatertest\test1 (2).txt", DeltaStep.SafePath(@"C:\Users\User\Desktop\updatertest\test1.txt"));
            Assert.AreEqual(@"C:\Users\User\Desktop\updatertest\merp.txt", DeltaStep.SafePath(@"C:\Users\User\Desktop\updatertest\merp.txt"));
        }

        public DSDelta GetTestDelta() {
            XDocument xd = XDocument.Load("testAppConfig.xml");
            AppConfig ac = new AppConfig(xd.Element("app"));
            return ac.GetDelta();
        }
    }
}
