using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImportDataToCSharp
{

    class Program
    {
        public class ScriptName
        {
            public string shortName;
            public string longName;
            public int tempIndex;   // our internal index during build, do not export. Can change when importing newer version of Unicode data!!! Don't use this value in persisten data such as DB!!!
        }
        public class ScriptNames : List<ScriptName> { }

        public class CodepointScript
        {
            public int rangeStart, rangeEnd;
            public string scriptNameShort;
            public int tempIndex;   // our internal index. Can change when importing newer version of Unicode data!!! Don't use this value in persisten data such as DB!!!
        }
        public class CodepointScripts : List<CodepointScript> { }


        static ScriptNames scriptNames = new ScriptNames();
        static CodepointScripts codepointScripts = new CodepointScripts();


        static void Main(string[] args)
        {
            LoadFromText();
            Test();
            WriteCSharpFile();

        }


        static private void LoadFromText()
        {


            var lines = File.ReadAllLines("sourceData/PropertyValueAliases.txt");
            var index = 0;
            foreach (var line in lines)
            {
                //sc ; Aghb                             ; Caucasian_Albanian
                if (line.StartsWith("sc"))
                {
                    string[] parts = line.Split(';');

                    if (parts[0].Trim() != "sc") // check again, other thing could start with 'sc' in the future and diff white space
                        continue;

                    var shortName = parts[1].Trim();
                    var longName = parts[2].Trim();

                    scriptNames.Add(new ScriptName { shortName = parts[1].Trim(), longName = parts[2].Trim(), tempIndex = index++ });
                }
            }

            lines = File.ReadAllLines("sourceData/Scripts.txt");
            foreach (var line in lines)
            {
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                //0061..007A; Latin # L&  [26] LATIN SMALL LETTER A..LATIN SMALL LETTER Z
                //00AA; Latin # Lo       FEMININE ORDINAL INDICATOR

                string[] parts = line.Split(';');
                var r = parts[0];

                int rangeStart, rangeEnd;

                if (r.Contains('.'))
                {
                    string[] rparts = r.Split("..");
                    rangeStart = Convert.ToInt32(rparts[0].Trim(), 16);// = int.Parse("x" + rparts[0]);
                    rangeEnd = Convert.ToInt32(rparts[1].Trim(), 16);
                }
                else
                {
                    rangeStart = rangeEnd = Convert.ToInt32(r.Trim(), 16);
                }

                var scriptNameLong = parts[1].Split('#')[0].Trim();
                var scriptName = scriptNames.Where(sn => sn.longName == scriptNameLong).First();
                codepointScripts.Add(new CodepointScript { rangeStart = rangeStart, rangeEnd = rangeEnd, scriptNameShort = scriptName.shortName, tempIndex = scriptName.tempIndex });
            }

        }

        private static void Test()
        {
            // short names for special 
            const string ScriptInheritedShort = "Zinh"; // Inherited
            const string ScriptCommonShort = "Zyyy";    // Common
            const string ScriptUnknownShort = "Zzzz";   // Unknown

            string test = "jkjsad j1 21%^& dsatט";


            // buckets
            int[] buckets = new int[scriptNames.Count];

            foreach (char c in test)
            {
                var codePoint = Convert.ToInt32(c);
                var cps = codepointScripts.Where(cs => cs.rangeStart <= codePoint && cs.rangeEnd >= codePoint).FirstOrDefault();
                if (cps == null) // not in table, ignore
                    continue;

                if (cps.scriptNameShort == ScriptCommonShort)
                    continue;
                if (cps.scriptNameShort == ScriptInheritedShort)
                    continue;
                if (cps.scriptNameShort == ScriptUnknownShort)
                    continue;

                buckets[cps.tempIndex]++;

            }

            for (int i = 0; i < buckets.Length; i++)
            {
                var bucket = buckets[i];
                if (bucket > 0)
                {
                    float p = (float)bucket / (float)test.Length * 100;
                    var s = scriptNames.Where(sn => sn.tempIndex == i).First();

                    Console.WriteLine($"script {s.longName}: {p}%");
                }
            }
        }

        static private void WriteCSharpFile()
        {

            StringBuilder sb = new StringBuilder();

         

            sb.AppendLine(@"
/* This file is generated by running UnicodeScriptDetectorNet.ImportDataToCSharp project
Changes made here would be overwritten if running ImportDataToCSharp again to import a newer Unicode version */

namespace UnicodeScriptDetectorNet
{
    public static partial class UnicodeScriptDetector
    {
        static void InitializeData()
        {
            scriptNames = new ScriptNames
            {
");

            foreach(var s in scriptNames)
            {
                // like:  new ScriptName {shortName = "Latn", longName = "Latin"},

                sb.AppendLine($"\t\t\t\tnew ScriptName {{shortName = \"{s.shortName}\", longName = \"{s.longName}\", tempIndex = {s.tempIndex}}},");
            }

            sb.AppendLine(@"
            };

            codepointScripts = new CodepointScripts
            {
            ");

            foreach(var cp in codepointScripts)
            {
                // like: new CodepointScript { rangeStart = 1, rangeEnd = 2, scriptNameShort = "a"}

                string s = string.Format("\t\t\t\tnew CodepointScript {{rangeStart = 0x{0}, rangeEnd = 0x{1}, scriptNameShort = \"{2}\", tempIndex = {3}}},",
                    cp.rangeStart.ToString("X"), cp.rangeEnd.ToString("X"),  cp.scriptNameShort, cp.tempIndex );
                sb.AppendLine(s);
                
            }

            sb.AppendLine(@"
            };
        }
    }
}
            ");


            if (!Directory.Exists("Output"))
                Directory.CreateDirectory("Output");

            File.WriteAllText("Output/Initialize.cs", sb.ToString());

        }
    }

}
