using System;
using UnicodeScriptDetectorNet;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {

            UnicodeScriptDetector.Results results;


            // 6 latin (ö is a Latin script char used in German) and 2 common (space)
            // the common characters are ignored, so % from the remaining 6 characters
            // should yield Latin 1.0

            results = UnicodeScriptDetector.GetUsedScripts(" Abö Abö");


            // 4 hebrew characters, 6 common, 2 Latin 
            // the common characters are ignored, so % from the remaining 6 characters
            // should yield hebrew 0.666, Latin 0.333

            results = UnicodeScriptDetector.GetUsedScripts("שש123456ששAü");


            // 1 latin characters, 1 hebrew, and a COMBINING GRAVE ACCENT (draws a ` above the preceeding character),
            // which inherits the script of the preceeding char
            // since the accent follows an hebrew char, it is counted as hebrew, too. Total relevant chars = 3
            // should yield Hebrew 0.666, Latin 0.33

            results = UnicodeScriptDetector.GetUsedScripts("aא\u0300", ignoreInherited:false); // aא̀


            // 1 latin characters, 1 hebrew, and a COMBINING GRAVE ACCENT (draws a ` above the preceeding character),
            // which inherits the script of the preceeding char
            // since the accent follows an latin char, it is counted as latin, too. Total relevant chars = 3
            // should yield Latin 0.666, Hebrew 0.33

            results = UnicodeScriptDetector.GetUsedScripts("אa\u0300", ignoreInherited:false); // אà

            // same but using default ignoreInherited (true)
            // 1 latin characters, 1 hebrew, and a COMBINING GRAVE ACCENT (draws a ` above the preceeding character),
            // which inherits the script of the preceeding char
            // since the accent is ignored total relevant chars = 2
            // should yield Latin 0.5, Hebrew 0.55857

            results = UnicodeScriptDetector.GetUsedScripts("אa\u0300"); // אà



            results = UnicodeScriptDetector.GetUsedScripts("Hello translates in Hebrew to: שלום");
            foreach(var r in results)
            {
                Console.WriteLine($"Script short code: {r.scriptNameShort}, long code {r.scriptNameLong}, probablity:{r.propabilty}");
            }

        }
    }
}
