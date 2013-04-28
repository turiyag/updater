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
        bool _bConcurrent;
        List<DeltaStep> _dslSubSteps;

        public DSSet(XElement xe, DeltaStep dsParent, AppConfig acApp) {
            string sTempRemote;
            string sTempLocal;
            Initialize(xe, dsParent, "set");
            sTempRemote = xe.Attr("remote", _sRemote);
            sTempRemote = acApp.Interpret(sTempRemote);
            _sRemote = FullRemotePath(sTempRemote);
            sTempLocal = xe.Attr("local", _sLocal);
            sTempLocal = acApp.Interpret(sTempLocal);
            _sLocal = FullLocalPath(sTempLocal);
            _bConcurrent = (xe.Attr("concurrent") == "true");
            _dslSubSteps = new List<DeltaStep>();
            foreach (XElement xeChild in xe.Elements()) {
                _dslSubSteps.Add(DeltaStep.Parse(xeChild, this, acApp));
            }
            _dssStatus = DeltaStepStatus.Waiting;
        }

        public override List<DeltaStep> GetChildSteps(DeltaStepCode dscCode = DeltaStepCode.None) {
            if (dscCode == DeltaStepCode.None) {
                return _dslSubSteps;
            } else {
                List<DeltaStep> dslRet = new List<DeltaStep>();
                foreach (DeltaStep ds in _dslSubSteps) {
                    if (ds.GetStepCode() == dscCode) {
                        dslRet.Add(ds);
                    }
                }
                return dslRet;
            }
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

        public bool GetConcurrecy() {
            return _bConcurrent;
        }
    }

    [TestFixture]
    public class DSSetTest {

        [Test]
        public void Test() {
            DSDelta dsdDelta = DeltaStepTest.GetTestDelta();
            Assert.Less(7, dsdDelta.GetDescendantSteps(DeltaStepCode.Set).Count);
            foreach (DeltaStep ds in dsdDelta.GetDescendantSteps(DeltaStepCode.Set)) {
                DSSet dss = (DSSet)(ds);
                switch (ds.GetID()) {
                    case "appdir":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath, dss.GetLocal());
                        Assert.AreEqual(@"http://www.google.com/", dss.GetRemote());
                        Assert.AreEqual(DeltaStepCautionLevel.Die, dss.GetCautionLevel());
                        Assert.True(dss.GetConcurrecy());
                        break;
                    case "remdir":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\v2.0", dss.GetLocal());
                        Assert.AreEqual(AppConfigTest.sRemoteTestPath, dss.GetRemote());
                        Assert.AreEqual(DeltaStepCautionLevel.Safe, dss.GetCautionLevel());
                        Assert.False(dss.GetConcurrecy());
                        break;
                    case "movetests":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\movetests", dss.GetLocal());
                        Assert.AreEqual(AppConfigTest.sRemoteTestPath, dss.GetRemote());
                        Assert.AreEqual(DeltaStepCautionLevel.Force, dss.GetCautionLevel());
                        Assert.False(dss.GetConcurrecy());
                        break;
                    case "gettests":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\gettests", dss.GetLocal());
                        Assert.AreEqual(AppConfigTest.sRemoteTestPath, dss.GetRemote());
                        Assert.AreEqual(DeltaStepCautionLevel.Die, dss.GetCautionLevel());
                        Assert.False(dss.GetConcurrecy());
                        break;
                    case "copytests":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\copytests", dss.GetLocal());
                        Assert.AreEqual(AppConfigTest.sRemoteTestPath, dss.GetRemote());
                        Assert.AreEqual(DeltaStepCautionLevel.Die, dss.GetCautionLevel());
                        Assert.False(dss.GetConcurrecy());
                        break;
                    case "deltests":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\deltests", dss.GetLocal());
                        Assert.AreEqual(AppConfigTest.sRemoteTestPath, dss.GetRemote());
                        Assert.AreEqual(DeltaStepCautionLevel.Skip, dss.GetCautionLevel());
                        Assert.False(dss.GetConcurrecy());
                        break;
                    case "mkdirtests":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath, dss.GetLocal());
                        Assert.AreEqual(AppConfigTest.sRemoteTestPath, dss.GetRemote());
                        Assert.AreEqual(DeltaStepCautionLevel.Force, dss.GetCautionLevel());
                        Assert.False(dss.GetConcurrecy());
                        break;
                    case "killtests":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath, dss.GetLocal());
                        Assert.AreEqual(AppConfigTest.sRemoteTestPath, dss.GetRemote());
                        Assert.AreEqual(DeltaStepCautionLevel.Die, dss.GetCautionLevel());
                        Assert.False(dss.GetConcurrecy());
                        break;
                    case "startuptests":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath, dss.GetLocal());
                        Assert.AreEqual(AppConfigTest.sRemoteTestPath, dss.GetRemote());
                        Assert.AreEqual(DeltaStepCautionLevel.Die, dss.GetCautionLevel());
                        Assert.False(dss.GetConcurrecy());
                        break;
                    default:
                        Assert.Fail("Unnamed <set> step at " + ds.GetID());
                        break;
                }
            }
            //dsdDelta.Run();
        }
    }
}
