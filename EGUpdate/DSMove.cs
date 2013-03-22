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
    class DSMove : DeltaStep {
        string _sSrc;
        string _sDest;

        public DSMove(XElement xe, DeltaStep dsParent, AppConfig acApp) {
            Initialize(xe, dsParent, "move"); 
            _sSrc = xe.RequiredAttr("src");
            _sSrc = acApp.Interpret(_sSrc);
            _sSrc = FullLocalPath(_sSrc);
            _sDest = xe.RequiredAttr("dest");
            _sDest = acApp.Interpret(_sDest);
            _sDest = FullLocalPath(_sDest);
            _dssStatus = DeltaStepStatus.Waiting;
        }

        public override void Attempt() {
            if (Directory.Exists(_sSrc)) {
                Directory.Move(_sSrc, _sDest);
            } else {
                File.Move(_sSrc, _sDest);
            }
        }
        public override void Safe(Exception e) {
            if (Directory.Exists(_sSrc)) {
                Directory.Move(_sSrc, DeltaStep.SafePath(_sDest));
            } else {
                File.Move(_sSrc, DeltaStep.SafePath(_sDest));
            }
        }
        public override void Force(Exception e) {
            if (Directory.Exists(_sSrc)) {
                Directory.Delete(_sDest, true);
                Directory.Move(_sSrc, _sDest);
            } else {
                File.Delete(_sDest);
                File.Move(_sSrc, _sDest);
            }
        }

        public string GetSource() {
            return _sSrc;
        }

        public string GetDestination() {
            return _sDest;
        }
    }

    [TestFixture]
    public class DSMoveTest { 

        [Test]
        public void Test() {
            DSDelta dsdDelta = DeltaStepTest.GetTestDelta();
            Assert.AreEqual(6, dsdDelta.GetDescendantSteps(DeltaStepCode.Move).Count); 
            foreach (DeltaStep ds in dsdDelta.GetDescendantSteps(DeltaStepCode.Move)) { 
                DSMove dsm = (DSMove)(ds); 
                switch (dsm.GetID()) {
                    case "mvdietest":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\movetests\die.txt", dsm.GetSource());
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\movetests\die2.txt", dsm.GetDestination());
                        Assert.AreEqual(DeltaStepCautionLevel.Die, dsm.GetCautionLevel());
                        break;
                    case "mvsafetest":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\movetests\safe.txt", dsm.GetSource());
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\movetests\safe2.txt", dsm.GetDestination());
                        Assert.AreEqual(DeltaStepCautionLevel.Safe, dsm.GetCautionLevel());
                        break;
                    case "mvskiptest":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\movetests\skip.txt", dsm.GetSource());
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\movetests\skip2.txt", dsm.GetDestination());
                        Assert.AreEqual(DeltaStepCautionLevel.Skip, dsm.GetCautionLevel());
                        break;
                    case "mvforcetest":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\movetests\force.txt", dsm.GetSource());
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\movetests\force2.txt", dsm.GetDestination());
                        Assert.AreEqual(DeltaStepCautionLevel.Force, dsm.GetCautionLevel());
                        break;
                    case "mvnormaltest":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\movetests\normal.txt", dsm.GetSource());
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\movetests\normal2.txt", dsm.GetDestination());
                        Assert.AreEqual(DeltaStepCautionLevel.Force, dsm.GetCautionLevel());
                        break;
                    case "mvfoldertest":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\movetests\folder", dsm.GetSource());
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\movetests\folder2", dsm.GetDestination());
                        Assert.AreEqual(DeltaStepCautionLevel.Die, dsm.GetCautionLevel());
                        break;
                    default:
                        Assert.Fail("Unnamed <move> step"); 
                        break;
                }
                //dsm.Run();
            }
            //dsdDelta.Run();
        }
    }
}
