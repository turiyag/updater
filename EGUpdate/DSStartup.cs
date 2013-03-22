using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Microsoft.Win32;

namespace EGUpdate {
    public enum StartupScope {
        CurrentUser,
        AllUsers
    };
    class DSStartup : DeltaStep {
        public const string STARTUP_KEY_PATH = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        string _sName;
        string _sPath;
        string _sArgs;
        string _sValue;
        StartupScope _ssScope;
        RegistryKey _rkStartup;


        public DSStartup(XElement xe, DeltaStep dsParent, AppConfig acApp) {
            string sScope;
            Initialize(xe, dsParent, "startup");
            _sName = xe.RequiredAttr("name");
            _sName = acApp.Interpret(_sName);
            _sPath = xe.RequiredAttr("path");
            _sPath = acApp.Interpret(_sPath);
            _sPath = FullLocalPath(_sPath);
            _sValue = "\"" + _sPath + "\"";
            _sArgs = xe.Attr("args", null);
            _sArgs = acApp.Interpret(_sArgs);
            if (!string.IsNullOrWhiteSpace(_sArgs)) {
                _sValue += " " + _sArgs;
            }
            sScope = xe.Attr("scope", "currentuser");
            _ssScope = (StartupScope)(Enum.Parse(typeof(StartupScope), sScope, true));
            _dssStatus = DeltaStepStatus.Waiting;
        }

        public override void Attempt() {
            switch (_ssScope) {
                case StartupScope.AllUsers:
                    _rkStartup = Registry.LocalMachine;
                    break; 
                case StartupScope.CurrentUser:
                    _rkStartup = Registry.CurrentUser;
                    break;
            }
            try {
                _rkStartup = _rkStartup.OpenSubKey(STARTUP_KEY_PATH, true);
            } catch {
                throw new InvalidOperationException("Registry key could not be opened for writing, try adding an <admin /> in your xml instruction set to force administrator priveliges");
            }
            if (_rkStartup.GetValue(_sName) == null) {
                _rkStartup.SetValue(_sName, _sValue);
            } else {
                throw new InvalidOperationException("Startup entry already set");
            }
        }
        public override void Force(Exception e) {
            _rkStartup.SetValue(_sName, _sValue);
        }
        public override void Safe(Exception e) {
            _sName = DeltaStep.SafeRegistryValueName(_rkStartup, _sName);
            _rkStartup.SetValue(_sName, _sValue);
        }
        public string GetName() {
            return _sName;
        }
        public string GetPath() {
            return _sPath ;
        }
        public string GetArgs() {
            return _sArgs;
        }
        public string GetValue() {
            return _sValue;
        }
        public StartupScope GetScope() {
            return _ssScope;
        }
    }

    [TestFixture]
    public class DSStartupTest { 

        [Test]
        public void Test() {
            List<DeltaStep> dsl = DeltaStepTest.GetTestDelta().GetDescendantSteps(DeltaStepCode.Startup);
            Assert.AreEqual(2, dsl.Count);
            foreach (DeltaStep ds in dsl) { 
                DSStartup dss = (DSStartup)(ds);
                switch (dss.GetID()) {
                    case "calcstartup":
                        Assert.AreEqual(StartupScope.CurrentUser, dss.GetScope());
                        Assert.AreEqual("calc", dss.GetName());
                        Assert.AreEqual(@"C:\Windows\System32\calc.exe", dss.GetPath());
                        Assert.AreEqual(null, dss.GetArgs());
                        Assert.AreEqual(@"""C:\Windows\System32\calc.exe""", dss.GetValue());
                        Assert.AreEqual(DeltaStepCautionLevel.Force, dss.GetCautionLevel());
                        break;
                    case "npstartup":
                        Assert.AreEqual(StartupScope.AllUsers, dss.GetScope());
                        Assert.AreEqual("notepad", dss.GetName());
                        Assert.AreEqual(@"C:\Windows\System32\notepad.exe", dss.GetPath());
                        Assert.AreEqual(@"C:\Windows\DtcInstall.log", dss.GetArgs());
                        Assert.AreEqual(@"""C:\Windows\System32\notepad.exe"" C:\Windows\DtcInstall.log", dss.GetValue());
                        Assert.AreEqual(DeltaStepCautionLevel.Safe, dss.GetCautionLevel());
                        break;
                    default:
                        Assert.Fail("Unnamed <startup> step"); 
                        break;
                }
                //dss.Run();
            }
            //dsdDelta.Run();
        }
    }
}
