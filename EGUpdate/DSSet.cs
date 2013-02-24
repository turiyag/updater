using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Threading;

namespace EGUpdate {
    class DSSet : DeltaStep {
        private bool _bConcurrent;
        private List<DeltaStep> _dslSubSteps;

        public DSSet(XElement xe, DeltaStep dsParent, AppConfig acApp) {
            Initialize(xe, dsParent, "set");
            _sRemote = xe.Attr("remote", _sRemote);
            _sRemote = acApp.Interpret(_sRemote);
            _sRemote = FullRemotePath(_sRemote);
            _sLocal = xe.Attr("local", _sLocal);
            _sLocal = acApp.Interpret(_sLocal);
            _sLocal = FullLocalPath(_sLocal);
            _bConcurrent = (xe.Attr("concurrent") == "true");
            _dslSubSteps = new List<DeltaStep>();
            foreach (XElement xeChild in xe.Elements()) {
                _dslSubSteps.Add(DeltaStep.Parse(xeChild, this, acApp));
            }
            _dssStatus = DeltaStepStatus.Waiting;
        }

        public override List<DeltaStep> GetSubSteps() {
            return _dslSubSteps;
        }

        public override void Attempt() {
            if (_bConcurrent) {
                List<Thread> tl = new List<Thread>();
                Thread t;
                //Start each step
                foreach (DeltaStep ds in _dslSubSteps) {
                    t = new Thread(ds.Run);
                    tl.Add(t);
                    t.Start();
                }
                //Wait until all steps are done
                foreach (Thread tRunning in tl) {
                    tRunning.Join();
                }
            } else {
                //Start each step and wait for each step
                foreach (DeltaStep ds in _dslSubSteps) {
                    ds.Run();
                }
            }
        }
    }

    [TestFixture]
    public class DSSetTest {

        [Test]
        public void Test() {
            XDocument xd = XDocument.Load("testAppConfig.xml");
            AppConfig ac = new AppConfig(xd.Element("app"));
            DSDelta dsdDelta = ac.GetDelta();
            Assert.AreEqual(5, ac.GetSteps().Count);
            foreach (DeltaStep ds in ac.GetSteps(DeltaStepCode.Set)) {
                DSExec dse = (DSExec)(ds);
                switch (ds.GetID()) {
                    case "ie":
                        Assert.AreEqual(@"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe", dse.GetPath());
                        Assert.AreEqual(@"http://www.stackoverflow.com", dse.GetArgs());
                        Assert.AreEqual(false, dse.GetWaitCmd());
                        Assert.AreEqual(DeltaStepCautionLevel.Die, dse.GetCautionLevel());
                        break;
                    case "np":
                        Assert.AreEqual(@"C:\Windows\System32\notepad.exe", dse.GetPath());
                        Assert.AreEqual(@"C:\Windows\csup.txt", dse.GetArgs());
                        Assert.AreEqual(false, dse.GetWaitCmd());
                        Assert.AreEqual(DeltaStepCautionLevel.Die, dse.GetCautionLevel());
                        break;
                    case "calc":
                        Assert.AreEqual(@"C:\Windows\System32\calc.exe", dse.GetPath());
                        Assert.AreEqual(@"", dse.GetArgs());
                        Assert.AreEqual(true, dse.GetWaitCmd());
                        Assert.AreEqual(DeltaStepCautionLevel.Die, dse.GetCautionLevel());
                        break;
                    case "update":
                        Assert.AreEqual(@"C:\Users\User\Desktop\updatertest\update.exe", dse.GetPath());
                        Assert.AreEqual(@"C:\Users\User\Desktop\updatertest", dse.GetArgs());
                        Assert.AreEqual(false, dse.GetWaitCmd());
                        Assert.AreEqual(DeltaStepCautionLevel.Skip, dse.GetCautionLevel());
                        break;
                    case "oldnew":
                        Assert.AreEqual(@"C:\Users\User\Desktop\updatertest\v2.0\cmd.exe", dse.GetPath());
                        Assert.AreEqual(@"v1.0", dse.GetArgs());
                        Assert.AreEqual(true, dse.GetWaitCmd());
                        Assert.AreEqual(DeltaStepCautionLevel.Die, dse.GetCautionLevel());
                        break;
                    default:
                        Assert.Fail("Unnamed <exec> step");
                        break;
                }
            }
            //dsdDelta.Run();
        }
    }
}
