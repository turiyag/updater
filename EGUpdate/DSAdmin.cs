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
    class DSAdmin : DeltaStep {

        public DSAdmin(XElement xe, DeltaStep dsParent, AppConfig acApp) {
            Initialize(xe, dsParent, "admin");
            _dssStatus = DeltaStepStatus.Waiting;
        }

        public override void Attempt() {
            Message(UAC.IsProcessElevated() ? "Process Elevated" : "Process Not Elevated");
            Message(UAC.IsRunAsAdmin() ? "Process Run As Admin" : "Process Not Run As Admin");
            UAC.SelfElevate();
        }

    }

    [TestFixture]
    public class DSAdminTest { 

        [Test]
        public void Test() {
            DSDelta dsdDelta = DeltaStepTest.GetTestDelta();
            IEnumerable<DeltaStep> dsl = dsdDelta.Select("admin");
            Assert.AreEqual(1, dsl.Count());
            foreach (DeltaStep ds in dsl) {
                //ds.Run();
            }
            //DeltaStep dt = dsdDelta.Select("#admintests").ElementAt(0);
            //dsdDelta.Run();
        }
    }
}
