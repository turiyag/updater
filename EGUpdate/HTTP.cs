using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using NUnit.Framework;

namespace EGUpdate {
    class HTTP {
        public static string GetString(string sURL) {
            HttpWebRequest hwreqRequest;
            HttpWebResponse hwrspResponse;
            hwreqRequest = (HttpWebRequest)WebRequest.Create(sURL);
            hwreqRequest.Method = "GET";
            hwreqRequest.ContentLength = 0;
            hwrspResponse = (HttpWebResponse)hwreqRequest.GetResponse();
            using (StreamReader reader = new StreamReader(hwrspResponse.GetResponseStream())) {
                return reader.ReadToEnd();
            }
        }
        public static byte[] GetBytes(string sURL) {
            HttpWebRequest hwreqRequest;
            HttpWebResponse hwrspResponse;
            hwreqRequest = (HttpWebRequest)WebRequest.Create(sURL);
            hwreqRequest.Method = "GET";
            hwreqRequest.ContentLength = 0;
            hwrspResponse = (HttpWebResponse)hwreqRequest.GetResponse();
            using (BinaryReader reader = new BinaryReader(hwrspResponse.GetResponseStream())) {
                return reader.ReadBytes(int.MaxValue);
            }
        }
        public static void DownloadFileTo(string sURL, string sPath) {
            HttpWebRequest hwreqRequest;
            HttpWebResponse hwrspResponse;
            hwreqRequest = (HttpWebRequest)WebRequest.Create(sURL);
            hwreqRequest.Method = "GET";
            hwreqRequest.ContentLength = 0;
            hwrspResponse = (HttpWebResponse)hwreqRequest.GetResponse();
            using (BinaryReader reader = new BinaryReader(hwrspResponse.GetResponseStream())) {
                using (FileStream fs = File.Create(sPath)) {
                    reader.BaseStream.CopyTo(fs);
                }
            }
        }

        public static bool Is404(WebException we) {
            try {
                return GetHTTPErrorStatusCode(we) == HttpStatusCode.NotFound;
            } catch (ArgumentNullException) {
                return false;
            }
        }

        public static bool IsDown(WebException we) {
            return we.Status == WebExceptionStatus.NameResolutionFailure;
        }

        public static HttpStatusCode GetHTTPErrorStatusCode(WebException we) {
            if (we.Response != null) {
                return ((HttpWebResponse)we.Response).StatusCode;
            }
            throw new ArgumentNullException("WebException Response is null");
        }
    }

    [TestFixture]
    public class HTTPTest {
        [Test]
        public void Test() {
            string sFail;
            try {
                sFail = HTTP.GetString(AppConfigTest.sRemoteTestPath + "/404notfound");
                Assert.Fail("Code should 404");
            } catch (WebException we) {
                Assert.IsTrue(HTTP.Is404(we));
            } catch (Exception e) {
                Assert.Fail("Bad error, code should 404:" + e.Message);
            }
            try {
                sFail = HTTP.GetString("http://merp.edgemontgeek.com/");
                Assert.Fail("Code should error out");
            } catch (Exception e) {
                Console.WriteLine("Good error:" + e.Message);
            }
            Assert.AreEqual(HTTP.GetString(AppConfigTest.sRemoteTestPath + "/test"), "EGUpdater Test");
        }

        [Test]
        public void TestDownload() {
            HTTP.DownloadFileTo("http://home.edgemontgeek.com/dev/updater/testapp/test", "httptest.txt");
            Assert.AreEqual("EGUpdater Test", File.ReadAllText("httptest.txt"));
            File.Delete("httptest.txt");
        }
    }
}
