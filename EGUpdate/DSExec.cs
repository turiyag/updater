using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace EGUpdate {
    class DSExec : DeltaStep {
        private string _sPath;
        private string _sArgs;
        private bool _bWait;

        public DSExec(XElement xe, DeltaStep dsParent, AppConfig acApp) {
            Initialize(xe, dsParent, "exec");
            _sPath = xe.RequiredAttr("path");
            _sPath = acApp.Interpret(_sPath);
            _sPath = FullLocalPath(_sPath);
            _sArgs = acApp.Interpret(xe.Attr("args", ""));
            _bWait = (xe.Attr("wait") == "true");
            _dssStatus = DeltaStepStatus.Waiting;
        }

        public override void Attempt() {
            Process p = Process.Start(_sPath, _sArgs);
            if (_bWait) {
                p.WaitForExit();
            }
        }

        public string GetPath() {
            return _sPath;
        }

        public string GetArgs() {
            return _sArgs;
        }

        public bool GetWaitCmd() {
            return _bWait;
        }
    }

    [TestFixture]
    public class DSExecTest {

        [Test]
        public void Test() {
            DSDelta dsdDelta = DeltaStepTest.GetTestDelta();
            List<DeltaStep> dsl = dsdDelta.GetDescendantSteps(DeltaStepCode.Exec);
            Assert.AreEqual(6, dsl.Count);
            foreach (DeltaStep ds in dsl) {
                DSExec dse = (DSExec)(ds);
                switch (ds.GetID()) {
                    case "ie":
                        Assert.AreEqual(@"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe", dse.GetPath());
                        Assert.AreEqual(@"http://www.stackoverflow.com", dse.GetArgs());
                        Assert.False(dse.GetWaitCmd());
                        Assert.AreEqual(DeltaStepCautionLevel.Die, dse.GetCautionLevel());
                        break;
                    case "np":
                        Assert.AreEqual(@"C:\Windows\System32\notepad.exe", dse.GetPath());
                        Assert.AreEqual(@"C:\Windows\csup.txt", dse.GetArgs());
                        Assert.False(dse.GetWaitCmd());
                        Assert.AreEqual(DeltaStepCautionLevel.Die, dse.GetCautionLevel());
                        break;
                    case "calc":
                        Assert.AreEqual(@"C:\Windows\System32\calc.exe", dse.GetPath());
                        Assert.AreEqual(@"", dse.GetArgs());
                        Assert.True(dse.GetWaitCmd());
                        Assert.AreEqual(DeltaStepCautionLevel.Die, dse.GetCautionLevel());
                        break;
                    case "update":
                    case "update2":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\update.exe", dse.GetPath());
                        Assert.AreEqual(AppConfigTest.sLocalTestPath, dse.GetArgs());
                        Assert.False(dse.GetWaitCmd());
                        Assert.AreEqual(DeltaStepCautionLevel.Skip, dse.GetCautionLevel());
                        break;
                    case "oldnew":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\v2.0\foobar.exe", dse.GetPath());
                        Assert.AreEqual(@"v1.0", dse.GetArgs());
                        Assert.True(dse.GetWaitCmd());
                        Assert.AreEqual(DeltaStepCautionLevel.Die, dse.GetCautionLevel());
                        break;
                    case "foobar":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\foobar.exe", dse.GetPath());
                        Assert.AreEqual(AppConfigTest.sLocalTestPath, dse.GetArgs());
                        Assert.False(dse.GetWaitCmd());
                        Assert.AreEqual(DeltaStepCautionLevel.Skip, dse.GetCautionLevel());
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
