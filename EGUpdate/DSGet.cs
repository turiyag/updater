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
    class DSGet : DeltaStep {

        public DSGet(XElement xe, DeltaStep dsParent, AppConfig acApp) {
            string sTempRemote;
            string sTempLocal;
            Initialize(xe, dsParent, "get");
            sTempRemote = xe.Attr("remote", _sRemote);
            sTempRemote = acApp.Interpret(sTempRemote);
            _sRemote = FullRemotePath(sTempRemote);
            sTempLocal = xe.Attr("local", _sLocal);
            sTempLocal = acApp.Interpret(sTempLocal);
            _sLocal = FullLocalPath(sTempLocal);
            _dssStatus = DeltaStepStatus.Waiting;
        }

        public override void Attempt() {
            if (File.Exists(_sLocal)) {
                throw new IOException("File already exists");
            }
            HTTP.DownloadFileTo(_sRemote, _sLocal);
        }

        public override void Force(Exception e) {
            if (File.Exists(_sLocal)) {
                File.Delete(_sLocal);
                HTTP.DownloadFileTo(_sRemote, _sLocal);
            } else {
                throw e;
            }
        }
        public override void Safe(Exception e) {
            HTTP.DownloadFileTo(_sRemote, DeltaStep.SafePath(_sLocal));
        }
    }

    [TestFixture]
    public class DSGetTest { 

        [Test]
        public void Test() {
            List<DeltaStep> dsl = DeltaStepTest.GetTestDelta().GetDescendantSteps(DeltaStepCode.Get);
            Assert.AreEqual(5, dsl.Count); 
            foreach (DeltaStep ds in dsl) { 
                DSGet dsg = (DSGet)(ds);
                switch (dsg.GetID()) {
                    case "getdie":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\gettests\die.txt", dsg.GetLocal());
                        Assert.AreEqual(AppConfigTest.sRemoteTestPath + @"/test", dsg.GetRemote());
                        Assert.AreEqual(DeltaStepCautionLevel.Die, dsg.GetCautionLevel());
                        break;
                    case "getforce":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\gettests\force.txt", dsg.GetLocal());
                        Assert.AreEqual(AppConfigTest.sRemoteTestPath + @"/test", dsg.GetRemote());
                        Assert.AreEqual(DeltaStepCautionLevel.Force, dsg.GetCautionLevel());
                        break;
                    case "getskip":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\gettests\skip.txt", dsg.GetLocal());
                        Assert.AreEqual(AppConfigTest.sRemoteTestPath + @"/test", dsg.GetRemote());
                        Assert.AreEqual(DeltaStepCautionLevel.Skip, dsg.GetCautionLevel());
                        break;
                    case "getsafe":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\gettests\safe.txt", dsg.GetLocal());
                        Assert.AreEqual(AppConfigTest.sRemoteTestPath + @"/test", dsg.GetRemote());
                        Assert.AreEqual(DeltaStepCautionLevel.Safe, dsg.GetCautionLevel());
                        break;
                    case "getfoobar":
                        Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\foobar.exe", dsg.GetLocal());
                        Assert.AreEqual(AppConfigTest.sRemoteTestPath + @"/v2.0/foobar.exe", dsg.GetRemote());
                        Assert.AreEqual(DeltaStepCautionLevel.Skip, dsg.GetCautionLevel());
                        break;
                    default:
                        Assert.Fail("Unnamed <get> step"); 
                        break;
                }
                //dsg.Run();
            }
            //dsdDelta.Run();
        }
    }
}
