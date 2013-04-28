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
    class DSMkDir : DeltaStep {
        string _sPath;

        public DSMkDir(XElement xe, DeltaStep dsParent, AppConfig acApp) {
            Initialize(xe, dsParent, "mkdir"); 
            _sPath = xe.RequiredAttr("path");
            _sPath = acApp.Interpret(_sPath);
            _sPath = FullLocalPath(_sPath);
            _dssStatus = DeltaStepStatus.Waiting;
        }

        public override void Attempt() {
            if (Directory.Exists(_sPath)) {
                throw new IOException("Directory already exists");
            }
            Directory.CreateDirectory(_sPath);
        }
        public override void Safe(Exception e) {
            Directory.CreateDirectory(DeltaStep.SafePath(_sPath));
        }

        public string GetPath() {
            return _sPath;
        }
    }

    [TestFixture]
    public class DSMkDirTest { 

        [Test]
        public void Test() {
            DSDelta dsdDelta = DeltaStepTest.GetTestDelta();
            Assert.AreEqual(3, dsdDelta.Select("mkdir").Count());
            foreach (DeltaStep ds in dsdDelta.Select("mkdir")) { 
                DSMkDir dsm = (DSMkDir)(ds);
                switch (dsm.GetID()) {
                    case "mkdirnormal":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\madedir", dsm.GetPath());
                        Assert.AreEqual(DeltaStepCautionLevel.Force, dsm.GetCautionLevel());
                        break;
                    case "mkdirsafe":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\madedir", dsm.GetPath());
                        Assert.AreEqual(DeltaStepCautionLevel.Safe, dsm.GetCautionLevel());
                        break;
                    case "mkdirnew":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\v2.0", dsm.GetPath());
                        Assert.AreEqual(DeltaStepCautionLevel.Die, dsm.GetCautionLevel());
                        break;
                    default:
                        Assert.Fail("Unnamed <mkdir> step"); 
                        break;
                }
                //dsm.Run();
            }
            //DeltaStep dt = dsdDelta.Select("#mkdirtests").ElementAt(0);
            //dt.Run();
            //dsdDelta.Run();
        }
    }
}
