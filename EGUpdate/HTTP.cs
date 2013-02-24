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
            try {
                hwreqRequest = (HttpWebRequest)WebRequest.Create(sURL);
                hwreqRequest.Method = "GET";
                hwreqRequest.ContentLength = 0;
                hwrspResponse = (HttpWebResponse)hwreqRequest.GetResponse();
                using (StreamReader reader = new StreamReader(hwrspResponse.GetResponseStream())) {
                    return reader.ReadToEnd();
                }
            } catch (Exception e) {
                throw e;
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
                sFail = HTTP.GetString("http://localhost/dev/updater/404notfound");
                Assert.Fail("Code should 404");
            } catch (WebException we) {
                Assert.IsTrue(HTTP.Is404(we));
            } catch (Exception e) {
                Console.WriteLine("Bad error:" + e.Message);
            }
            try {
                sFail = HTTP.GetString("http://merp.edgemontgeek.com/");
                Assert.Fail("Code should error out");
            } catch (Exception e) {
                Console.WriteLine("Good error:" + e.Message);
            }
            Assert.AreEqual(HTTP.GetString("http://localhost/dev/updater/test"), "EGUpdater Test");

        }
    }
}
