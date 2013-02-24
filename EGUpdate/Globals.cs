using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EGUpdate {
    class Globals {
        public static void FailAndDie(string sMessage) {
            System.Console.WriteLine(sMessage);
            Environment.Exit(1);
        }
    }
}
