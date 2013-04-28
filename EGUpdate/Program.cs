using System;
using System.IO;
using System.Xml.Linq;


namespace EGUpdate {
    class Program {

        static void Main(string[] args) {
            XDocument xd = null;
            //new DeltaStepTest().FullTest();
            //FailAndDie(UAC.Dir());
            if (args.Length == 0) {
                TextReader tr = new StringReader(EGUpdate.Properties.Resources.install);
                xd = XDocument.Load(tr);
            } else if (args.Length == 1) {
                if (!File.Exists(args[0])) {
                    FailAndDie("XML file does not exist\r\nUsage:\r\n\r\negu.exe xmlpath");
                } else {
                    xd = XDocument.Load(args[0]);
                }
            } else {
                FailAndDie("Incorrect usage\r\nUsage:\r\n\r\negu.exe xmlpath");
            }
            AppConfig ac = new AppConfig(xd.Element("app"));
            ac.GetDelta().Run();
            System.Console.WriteLine("All done!");
            System.Console.ReadLine();
        }

        public static void FailAndDie(string sMsg) {
            System.Console.Error.WriteLine(sMsg);
            Environment.Exit(1);
        }
    }
}
