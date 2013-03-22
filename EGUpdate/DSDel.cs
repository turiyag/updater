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
    class DSDel : DeltaStep {
        string _sPath;


        public DSDel(XElement xe, DeltaStep dsParent, AppConfig acApp) {
            Initialize(xe, dsParent, "del"); 
            _sPath = xe.RequiredAttr("path");
            _sPath = acApp.Interpret(_sPath);
            _sPath = FullLocalPath(_sPath);
            _dssStatus = DeltaStepStatus.Waiting;
        }

        public override void Attempt() {
            if (Directory.Exists(_sPath)) {
                Directory.Delete(_sPath, true);
            } else {
                if (File.Exists(_sPath)) {
                    File.Delete(_sPath);
                } else {
                    throw new FileNotFoundException("File/Directory to delete not found");
                }
            }
        }

        public string GetPath() {
            return _sPath;
        }
    }

    [TestFixture]
    public class DSDelTest { 

        [Test]
        public void Test() {
            DSDelta dsdDelta = DeltaStepTest.GetTestDelta();
            IEnumerable<DeltaStep> dsl = dsdDelta.Select("#deltests del");
            Assert.AreEqual(2, dsl.Count());
            foreach (DeltaStep ds in dsl) { 
                DSDel dsd = (DSDel)(ds);
                switch (dsd.GetID()) {
                    case "delfile":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\deltests\file.txt", dsd.GetPath());
                        Assert.AreEqual(DeltaStepCautionLevel.Skip, dsd.GetCautionLevel());
                        break;
                    case "deldir":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\deltests\asdf", dsd.GetPath());
                        Assert.AreEqual(DeltaStepCautionLevel.Skip, dsd.GetCautionLevel());
                        break;
                    default:
                        Assert.Fail("Unnamed <del> step"); 
                        break;
                }
            }
            //DeltaStep dt = dsdDelta.Select("#deltests").ElementAt(0);
            //dt.Run();
            //dsdDelta.Run();
        }
    }
}
