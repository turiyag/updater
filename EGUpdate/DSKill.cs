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
    class DSKill : DeltaStep {
        string _sName;
        string _sPath;
        bool _bKillThemAll;

        public DSKill(XElement xe, DeltaStep dsParent, AppConfig acApp) {
            Initialize(xe, dsParent, "kill");
            _sName = xe.Attr("name", "");
            _sName = acApp.Interpret(_sName).ToLower();
            _sPath = xe.Attr("path", "");
            _sPath = acApp.Interpret(_sPath).ToLower();
            _bKillThemAll = (xe.Attr("all", "true") == "true");
            _dssStatus = DeltaStepStatus.Waiting;
        }

        public override void Attempt() {
            foreach (Process pInnocent in Process.GetProcesses()) {
                if (IsDoomed(pInnocent)) {
                    pInnocent.Kill();
                    if (!_bKillThemAll) {
                        return;
                    }
                }
            }
        }

        private bool IsDoomed(Process p) {
            try {
                if (!string.IsNullOrWhiteSpace(_sName)) {
                    if (_sName != p.ProcessName.ToLower()) {
                        return false;
                    }
                }
                if (!string.IsNullOrWhiteSpace(_sPath)) {
                    if (_sPath != p.MainModule.FileName.ToLower()) {
                        return false;
                    }
                }
            } catch (Exception) {
                return false;
            }
            return true;
        }

        public string GetProcessName() {
            return _sName;
        }

        public string GetProcessPath() {
            return _sPath;
        }

        public bool IsKillingThemAll() {
            return _bKillThemAll;
        }

    }

    [TestFixture]
    public class DSKillTest { 

        [Test]
        public void Test() {
            DSDelta dsdDelta = DeltaStepTest.GetTestDelta();
            IEnumerable<DeltaStep> dsl = dsdDelta.Select("#killtests kill");
            Assert.AreEqual(2, dsl.Count());
            foreach (DeltaStep ds in dsl) {
                DSKill dsk = (DSKill)(ds);
                switch (dsk.GetID()) {
                    case "killcalc":
                        Assert.AreEqual("calc", dsk.GetProcessName());
                        Assert.True(dsk.IsKillingThemAll());
                        break;
                    case "killnp":
                        Assert.AreEqual(@"C:\Windows\System32\notepad.exe".ToLower(), dsk.GetProcessPath());
                        Assert.False(dsk.IsKillingThemAll());
                        break;
                    default:
                        Assert.Fail("Unnamed <kill> step"); 
                        break;
                }
                //dsk.Run();
            }
            //DeltaStep dt = dsdDelta.Select("#killtests").ElementAt(0);
            //dsdDelta.Run();
        }
    }
}
