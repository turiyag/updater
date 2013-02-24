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
    class DSDelta : DeltaStep {
        private bool _bConcurrent;
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
                } catch (Exception e) {
                    Program.FailAndDie("Could not retrieve xml files: " + e.Message);
                    return;
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

        public override List<DeltaStep> GetSubSteps() {
            return _dslSubSteps;
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
        }
    }
}
