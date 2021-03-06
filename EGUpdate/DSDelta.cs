﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Threading;

namespace EGUpdate {
    public class DSDelta : DeltaStep {
        private List<DeltaStep> _dslSubSteps;

        public DSDelta(AppConfig acApp) {
            _dssStatus = DeltaStepStatus.Initial;
            _dscCode = DeltaStepCode.Delta;
            _dsclCaution = DeltaStepCautionLevel.Die;
            string sXML;
            try {
                sXML = HTTP.GetString(acApp.GetRemoteVerPath() + "update/from" + acApp.GetLocalVersion() + ".xml");
            } catch (Exception) {
                try {
                    sXML = HTTP.GetString(acApp.GetRemoteVerPath() + "update/init.xml");
                } catch (Exception) {
                    throw new Exception("Error downloading update configurations");
                }
            }
            XDocument xd = XDocument.Load(new StringReader(sXML));
            XElement xeDelta = xd.Element("delta");
            _sXML = xeDelta.Text();
            _dsParent = null;
            _acApp = acApp;
            _sRemote = acApp.GetRemoteRoot();
            _sLocal = acApp.GetLocalRoot();
            _dslSubSteps = new List<DeltaStep>();
            foreach (XElement xeChild in xeDelta.Elements()) {
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
            //Start each step and wait for each step
            foreach (DeltaStep ds in _dslSubSteps) {
                ds.Run();
            }
        }

    }

    [TestFixture]
    public class DSDeltaTest {

        [Test]
        public void Test() {
            DSDelta dsdDelta = DeltaStepTest.GetTestDelta();
        }
    }
}
