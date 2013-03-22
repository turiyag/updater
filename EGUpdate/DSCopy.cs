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
    class DSCopy : DeltaStep {
        string _sSrc;
        string _sDest;
        bool _bRecursive;

        public DSCopy(XElement xe, DeltaStep dsParent, AppConfig acApp) {
            Initialize(xe, dsParent, "copy");
            _sSrc = xe.RequiredAttr("src");
            _sSrc = acApp.Interpret(_sSrc);
            _sSrc = FullLocalPath(_sSrc);
            _sDest = xe.RequiredAttr("dest");
            _sDest = acApp.Interpret(_sDest);
            _sDest = FullLocalPath(_sDest);
            _bRecursive = (xe.Attr("recursive", "true") == "true");
            _dssStatus = DeltaStepStatus.Waiting;
        }

        public override void Attempt() {
            if (Directory.Exists(_sSrc)) {
                DirectoryCopy(_sSrc, _sDest);
            } else {
                File.Copy(_sSrc, _sDest);
            }
        }
        public override void Safe(Exception e) {
            if (Directory.Exists(_sSrc)) {
                DirectoryCopy(_sSrc, DeltaStep.SafePath(_sDest));
            } else {
                File.Copy(_sSrc, DeltaStep.SafePath(_sDest));
            }
        }
        public override void Force(Exception e) {
            if (Directory.Exists(_sSrc)) {
                DirectoryCopy(_sSrc, _sDest, true);
            } else {
                File.Delete(_sDest);
                File.Copy(_sSrc, _sDest);
            }
        }

        public string GetSource() {
            return _sSrc;
        }

        public string GetDestination() {
            return _sDest;
        }

        private void DirectoryCopy(string sSource, string sDest, bool bForce = false) {
            DirectoryInfo diSource = new DirectoryInfo(sSource);
            DirectoryInfo[] diaSubdirectories = diSource.GetDirectories();
            FileInfo[] fiaFiles;

            if (!diSource.Exists) {
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sSource);
            }

            if (Directory.Exists(sDest)) {
                if (bForce) {
                    Directory.Delete(_sDest, true);
                } else {
                    throw new IOException("Destination directory already exists");
                }
            }
            Directory.CreateDirectory(sDest);

            fiaFiles = diSource.GetFiles();
            foreach (FileInfo fiFile in fiaFiles) {
                fiFile.CopyTo(Path.Combine(sDest, fiFile.Name), bForce);
            }

            if (_bRecursive) {
                foreach (DirectoryInfo diSubdir in diaSubdirectories) {
                    DirectoryCopy(diSubdir.FullName, Path.Combine(sDest, diSubdir.Name), bForce);
                }
            }
        }
    }

    [TestFixture]
    public class DSCopyTest { 

        [Test]
        public void Test() {
            DSDelta dsdDelta = DeltaStepTest.GetTestDelta();
            List<DeltaStep> dsl = dsdDelta.GetDescendantSteps(DeltaStepCode.Copy);
            Assert.Greater(dsl.Count, 9); 
            foreach (DeltaStep ds in dsl) { 
                DSCopy dsc = (DSCopy)(ds);
                if (dsc.GetParent().GetID() == "copytests") {
                    switch (dsc.GetID()) {
                        case "copydie":
                            Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\copytests\test.txt", dsc.GetSource());
                            Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\copytests\die.txt", dsc.GetDestination());
                            Assert.AreEqual(DeltaStepCautionLevel.Die, dsc.GetCautionLevel());
                            break;
                        case "copysafe":
                            Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\copytests\test.txt", dsc.GetSource());
                            Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\copytests\safe.txt", dsc.GetDestination());
                            Assert.AreEqual(DeltaStepCautionLevel.Safe, dsc.GetCautionLevel());
                            break;
                        case "copyskip":
                            Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\copytests\test.txt", dsc.GetSource());
                            Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\copytests\skip.txt", dsc.GetDestination());
                            Assert.AreEqual(DeltaStepCautionLevel.Skip, dsc.GetCautionLevel());
                            break;
                        case "copyforce":
                            Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\copytests\test.txt", dsc.GetSource());
                            Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\copytests\force.txt", dsc.GetDestination());
                            Assert.AreEqual(DeltaStepCautionLevel.Force, dsc.GetCautionLevel());
                            break;
                        case "copydirforce":
                            Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\copytests\nothing", dsc.GetSource());
                            Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\copytests\nothing2", dsc.GetDestination());
                            Assert.AreEqual(DeltaStepCautionLevel.Force, dsc.GetCautionLevel());
                            break;
                        case "copydirforce2":
                            Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\copytests\full", dsc.GetSource());
                            Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\copytests\nothing2", dsc.GetDestination());
                            Assert.AreEqual(DeltaStepCautionLevel.Force, dsc.GetCautionLevel());
                            break;
                        case "copydirskip":
                            Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\copytests\3files", dsc.GetSource());
                            Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\copytests\nothing", dsc.GetDestination());
                            Assert.AreEqual(DeltaStepCautionLevel.Skip, dsc.GetCautionLevel());
                            break;
                        case "copydirdie":
                            Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\copytests\3files", dsc.GetSource());
                            Assert.AreEqual(AppConfigTest.sLocalTestPath + @"\copytests\3filesB", dsc.GetDestination());
                            Assert.AreEqual(DeltaStepCautionLevel.Die, dsc.GetCautionLevel());
                            break;
                        default:
                            Assert.Fail("Unnamed <copy> step");
                            break;
                    }
                    //dsc.Run();
                }
            }
            //dsdDelta.Run();
        }
    }
}
