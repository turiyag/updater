using System;
using System.IO;
using System.Xml.Linq;


namespace EGUpdate {
    class Program {
        private static string sXML = @"<?xml version=""1.0""?> <app> <remote boo=""ghost"" mario=""human"">http://home.edgemontgeek.com/dev/updater/testapp/</remote> <local is=""5"" was=""7"" willbe=""9"" /> <desc>Test App</desc> </app>";

        static void Main(string[] args) {

            //loopex2();
            //system.console.readline();
            //return;
            new DSExecTest().Test();
            System.Console.WriteLine("All done!");
            System.Console.ReadLine();
            return;

            System.Console.WriteLine("Initializing...");
            foreach (string sArg in args) {
                System.Console.WriteLine("  Reading: " + sArg);
                EGUConfig.ParseEGUConfigFile(sArg);
            }
        }

        public static void FailAndDie(string sMsg) {
            System.Console.Error.WriteLine(sMsg);
            Environment.Exit(1);
        }

        static void nestedLoops(int loops, int iterationsPerLoop) {
            int iLooper = 0;
            int iOneLoop = 0;
            for (iLooper = 0; iLooper < Math.Pow(iterationsPerLoop, loops); iLooper++) {
                for (iOneLoop = 0; iOneLoop < loops; iOneLoop++) {
                    if (iLooper % Math.Pow(iterationsPerLoop, iOneLoop) == 0) {
                        System.Console.WriteLine("Loop execution of loop #" + iOneLoop);
                    }
                }
            }
        }

        static void loopEx1() {
            for (int i = 0; i < 5; i++) {
                System.Console.WriteLine("Outer");
                for (int k = 0; k < 5; k++) {
                    System.Console.WriteLine("Inner");
                }
            }
        }

        static void loopEx2() {
            for (int i = 0; i < 5*5; i++) {
                if (i % 5 == 0) {
                    System.Console.WriteLine("Outer");
                }
                if (i % 1 == 0) {
                    System.Console.WriteLine("Inner");
                }
            }
        }
    }
}
