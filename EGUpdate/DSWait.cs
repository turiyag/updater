using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace EGUpdate {
    class DSWait : DeltaStep {
        private int _iTime;
        private string _sMsg;

        public DSWait(XElement xe, DeltaStep dsParent, AppConfig acApp) {
            Initialize(xe, dsParent, "wait");
            string sTime;
            sTime = xe.RequiredAttr("time");
            _iTime = int.Parse(sTime);
            _sMsg = xe.Attr("msg", "Waiting " + _iTime + "ms");
            _sMsg = _acApp.Interpret(_sMsg);
            _dssStatus = DeltaStepStatus.Waiting;
        }

        public override void Attempt() {
            Message(_sMsg);
            Thread.Sleep(_iTime);
        }

        public int GetTime() {
            return _iTime;
        }

        public string GetMsg() {
            return _sMsg;
        }
    }

    [TestFixture]
    public class DSWaitTest {

        [Test]
        public void Test() {
            DSDelta dsdDelta = DeltaStepTest.GetTestDelta();
            Assert.AreEqual(3, dsdDelta.GetDescendantSteps(DeltaStepCode.Wait).Count);
            foreach (DeltaStep ds in dsdDelta.GetDescendantSteps(DeltaStepCode.Wait)) {
                DSWait dsw = (DSWait)(ds);
                switch (ds.GetID()) {
                    case "3sec":
                        Assert.AreEqual(3000, dsw.GetTime());
                        Assert.AreEqual(@"This is a message", dsw.GetMsg());
                        Assert.AreEqual(DeltaStepCautionLevel.Skip, dsw.GetCautionLevel());
                        break;
                    case "1sec":
                        Assert.AreEqual(1000, dsw.GetTime());
                        Assert.AreEqual("Waiting 1000ms", dsw.GetMsg());
                        Assert.AreEqual(DeltaStepCautionLevel.Force, dsw.GetCautionLevel());
                        break;
                    case "1secB":
                        Assert.AreEqual(1000, dsw.GetTime());
                        Assert.AreEqual("Waiting 1000ms", dsw.GetMsg());
                        Assert.AreEqual(DeltaStepCautionLevel.Safe, dsw.GetCautionLevel());
                        break;
                    default:
                        Assert.Fail("Unnamed <wait> step");
                        break;
                }
            }
            //dsdDelta.Run();
        }
    }
}
