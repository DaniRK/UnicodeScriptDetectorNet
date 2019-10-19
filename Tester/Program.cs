using System;
using UnicodeScriptDetectorNet;
using System.Linq;

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



            // using default ignoreInherited parameter (true)
            // 1 latin characters, 1 hebrew, and a COMBINING GRAVE ACCENT (draws a ` above the preceeding character),
            // which inherits the script of the preceeding char
            // since the accent is ignored, total relevant chars = 2
            // should yield Latin 0.5, Hebrew 0.5

            results = UnicodeScriptDetector.GetUsedScripts("אa\u0300"); // אà


            // same but using parameter ignoreInherited=false
            // 1 latin characters, 1 hebrew, and a COMBINING GRAVE ACCENT (draws a ` above the preceeding character),
            // which inherits the script of the preceeding char
            // since the accent follows an latin char, it is counted as latin, too. Total relevant chars = 3
            // should yield Latin 0.666, Hebrew 0.33

            results = UnicodeScriptDetector.GetUsedScripts("אa\u0300", ignoreInherited: false); // אà

            results = UnicodeScriptDetector.GetUsedScripts("Hello translates in Hebrew to: שלום");
            foreach(var r in results)
            {
                Console.WriteLine($"Script short code: {r.scriptNameShort}, long code {r.scriptNameLong}, probablity:{r.probabilty}");
            }


            float p;
            
            // common only, should yield 1
            p = UnicodeScriptDetector.ProbablyInScript("123 +", "Latn");

            // common only, but one of the commons is restricted to the geogian and armenic scripts 
            // should yield 0.8
            p = UnicodeScriptDetector.ProbablyInScript("123 \u0589", "Latn");
            

            // only latins and commons, should yield 1
            p = UnicodeScriptDetector.ProbablyInScript(" Abö Abö", "Latin");

            // 6 latins and 2 commons , should yield 2/8 = 0.25
            p = UnicodeScriptDetector.ProbablyInScript(" Abö Abö", "HEBREW");

            bool b;

            // common only, should yield true
            b = UnicodeScriptDetector.IsInScript("123 +", "Latn");

            // common only, should yield false
            b = UnicodeScriptDetector.IsInScript("123 +", "Latn", strict:true);

            // only latins and commons, should yield true
            b = UnicodeScriptDetector.IsInScript(" Abö Abö", "Latin");

            // 6 latins and 2 commons , should yield false
            b = UnicodeScriptDetector.IsInScript(" Abö Abö", "hebrew");


            // should yield
            /*
                Short: 'Adlm', Long: 'Adlam'
                Short: 'Aghb', Long: 'Caucasian_Albanian'
                Short: 'Ahom', Long: 'Ahom'
                Short: 'Arab', Long: 'Arabic'          
                ...
            */
            foreach (var sn in UnicodeScriptDetector.GetScripts())
            {
                Console.WriteLine($"Short: '{sn.shortName}', Long: '{sn.longName}'");
            }


            // count number of code points with with explicit extended properties that 
            // include (also) the latin script.
            // should yield: latinextended = 21,  nonLatinExtended = 446

            int latinExtended = 0, nonLatinExtended = 0;

            foreach(var ep in UnicodeScriptDetector.GetExtendedProperties())
            {
                int inRange = ep.rangeEnd - ep.rangeStart + 1;

                if (ep.scriptNamesShort.Contains("Latn"))
                    latinExtended += inRange;
                else
                    nonLatinExtended += inRange;
            }

            // count number of code points that inherit from preceeding character
            int inheritedCount = 0;
            foreach (var ep in UnicodeScriptDetector.GetCodepointScripts())
            {

                if (ep.script.shortName == UnicodeScriptDetector.ScriptShortInherited)
                {
                    int inRange = ep.rangeEnd - ep.rangeStart + 1;
                    if(inRange < 0)
                    {

                    }
                    inheritedCount += inRange;
                }
             }


            // dump contigous ranges of code points that pass as latin script
            // count number of code points that pass as latin script
            // Note: ALL inherited codepoints will NOT pass, since we test only single code points, so there 
            // is no preceeding code point to inherit from (which should never occur in real text)


            int start = -1, end = -1;           
            bool lastWasInScript = false;
            int rangesCount = 0;
            int matches = 0;

            for(int cp= 0 ; cp< 0x10FFFF; cp++) // unicode space 0-0x10FFFF, .net (without surrogates): 0-xffff (UTF16)
            {

                if (cp >= 0x00d800 && cp <= 0x00dfff) //exclude surrogate code points that are never assigned in unicode to a real character and cannot be converted to utf-32
                    continue;

                string s = char.ConvertFromUtf32(cp);
                if (UnicodeScriptDetector.IsInScript(s, "Latn", applyExtendedProperties:true))
                {
                    if (!lastWasInScript)
                       
                        start = cp;  // start new range

                    end = cp;
                    matches++;
                    lastWasInScript = true;
                }
                else
                {
                    if(lastWasInScript)
                    {
                        // range of in script has finished, print it
                        Console.WriteLine("{0} - {1}", start.ToString("X"), end.ToString("X")) ;
                        rangesCount++;
                    }
                    lastWasInScript = false;
                }
            }

            // This should yield:
            // applyExtendedProperties = true: matches 4786
            // applyExtendedProperties = false: matches 5003 
            //
            Console.WriteLine("{0} ranges  {1} matches", rangesCount, matches);
        }
    }
}
