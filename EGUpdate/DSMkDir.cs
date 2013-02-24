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
        private string _sPath;

        public DSMkDir(XElement xe, DeltaStep dsParent, AppConfig acApp) {
            Initialize(xe, dsParent, "mkdir");
            _sPath = xe.RequiredAttr("path");
            _sPath = acApp.Interpret(_sPath);
            _sPath = FullLocalPath(_sPath);
            _dssStatus = DeltaStepStatus.Waiting;
        }

        public override void Attempt() {
            if (_dsclCaution == DeltaStepCautionLevel.Safe) {
                _sPath = DeltaStep.SafePath(_sPath);
            }
            Directory.CreateDirectory(_sPath);
        }
    }

    [TestFixture]
    public class DSMkDirTest {

        [Test]
        public void Test() {
        }
    }
}
