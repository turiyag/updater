using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace EGUpdate {
    class EGUConfig {
        public static List<AppConfig> aclAppConfigs = new List<AppConfig>();
        public static void ParseEGUConfigFile(string sPath) {
            if (!File.Exists(sPath)) {
                Globals.FailAndDie("XML file not found");
            }
            if (sPath.Substring(sPath.Length - 3) != "xml") {
                Globals.FailAndDie("File extension must be xml");
            }
            try {

                XDocument xd = XDocument.Load(sPath);
                XElement xeCatalog = xd.Elements().ElementAt(0);
                if (xeCatalog.Name.ToString() != "catalog") throw new FormatException("File is not properly formatted");
                foreach (XElement xe in xeCatalog.Select("app")) {
                    AppConfig ac = new AppConfig(xe);
                    EGUConfig.aclAppConfigs.Add(ac);
                    System.Console.Write(ac);
                    if (ac.GetAppStatus() == AppStatus.Error) System.Console.Write("!");
                    System.Console.WriteLine();
                }
            } catch (Exception e) {
                System.Console.WriteLine(e.Message);
            }
            System.Console.ReadLine();
        }
    }
}
