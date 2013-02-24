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
    class DSWait : DeltaStep {
        private int _iTime;
        private string _sMsg;

        public DSWait(XElement xe, DeltaStep dsParent, AppConfig acApp) {
            Initialize(xe, dsParent, "wait");
            string sTime;
            sTime = xe.RequiredAttr("time");
            _iTime = int.Parse(sTime);
            _sMsg = xe.Attr("msg", "Waiting " + _iTime + "ms");
            _sMsg = _acApp.Interpret(_sMsg);
            _dssStatus = DeltaStepStatus.Waiting;
        }

        public override void Attempt() {
            System.Console.WriteLine(_sMsg);
            Thread.Sleep(_iTime);
            System.Console.WriteLine("Done: " + _sMsg);
        }
    }

    [TestFixture]
    public class DSWaitTest {

        [Test]
        public void Test() {
        }
    }
}
