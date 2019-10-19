using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImportDataToCSharp
{
    class Program
    {
        public class Script
        {
            public string shortName;
            public string longName;
            public int tempIndex;   // our internal index during build, do not export. Can change when importing newer version of Unicode data!!! Don't use this value in persisten data such as DB!!!
        }
        public class Scripts : List<Script> { }

        public class CodepointScript
        {
            public int rangeStart, rangeEnd;
            public Script script;
        }
        public class CodepointScripts : List<CodepointScript> { }

        public class CodepointScriptExtended
        {
            public int rangeStart, rangeEnd;
            public List<string> scriptNamesExtendedShort = new List<string>(); // list of short(!) script names
        }
        public class CodepointScriptsExtended : List<CodepointScriptExtended> { }


        static Scripts scripts = new Scripts();
        static CodepointScripts codepointScripts = new CodepointScripts();
        static CodepointScriptsExtended codepointScriptsExtended = new CodepointScriptsExtended();

        static void Main(string[] args)
        {
            LoadFromText();
            WriteCSharpFile();
            WriteJSONDataDefs();
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

                    scripts.Add(new Script { shortName = parts[1].Trim(), longName = parts[2].Trim(), tempIndex = index++ });
                }
            }

            ////// scripts

            lines = File.ReadAllLines("sourceData/Scripts.txt");
            CodepointScripts codepointScriptsRaw = new CodepointScripts();

            CodepointScript lastCps = null;

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
                var script = scripts.Where(sn => sn.longName == scriptNameLong).First();

                var cps = new CodepointScript { rangeStart = rangeStart, rangeEnd = rangeEnd, script = script };

                codepointScriptsRaw.Add(cps);
            }

            codepointScriptsRaw.Sort((item1, item2) => item1.rangeStart.CompareTo(item2.rangeStart));

            lastCps = null;

            foreach (var cps in codepointScriptsRaw)
            {
                /* scripts.txt includes many ranges that could be expressed as part of a larger contigous range.
                The reason for that is that the smaller ranges have also some data attributes written in the comment, which 
                we do not use. Therefore, where possible, extend ranges instead of adding a new ones. This reduces number
                of ranges by about 50%.

                example:
                0020          ; Common # Zs       SPACE
                0021..0023    ; Common # Po   [3] EXCLAMATION MARK..NUMBER SIGN
                0024          ; Common # Sc       DOLLAR SIGN
                can be combined into a range 0020..0024, Common

                */

                if (lastCps != null && cps.rangeStart == lastCps.rangeEnd + 1 && cps.script == lastCps.script)
                {
                    lastCps.rangeEnd = cps.rangeEnd;
                }
                else
                {
                    var newCps = new CodepointScript
                    {
                        rangeStart = cps.rangeStart,
                        rangeEnd = cps.rangeEnd,
                        script = cps.script
                    };

                    codepointScripts.Add(cps);

                    lastCps = cps;
                }
            }

            ///////// extended /////////////

            lines = File.ReadAllLines("sourceData/ScriptExtensions.txt");
            CodepointScriptsExtended codepointScriptsExtendedRaw = new CodepointScriptsExtended();

            CodepointScriptExtended lastCpsExt = null;

            foreach (var line in lines)
            {
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                //102E0         ; Arab Copt # Mn       COPTIC EPACT THOUSANDS MARK
                //102E1..102FB  ; Arab Copt # No  [27] COPTIC EPACT DIGIT ONE..COPTIC EPACT NUMBER NINE HUNDRED

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

                var nameSection = parts[1].Split('#')[0];
                var scriptNamesShort = nameSection.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var scriptNamesShortList = new List<string>(scriptNamesShort);

                /* now comes a bit complex thing: for later algorithm performance we want to add the extended script names to the codepointScripts. But
                it happens that a range in extended does not match a full common range in codepointScripts. 
                Example:
                in codepointScripts:
                                rangeStart = 0x951, rangeEnd = 0x954, scriptNameShort = "Zinh"
                in codepointScriptsExtendedRaw:
                                rangeStart = 0x952, rangeEnd = 0x952, scriptNamesShort = new string[]{"Beng", "Deva", "Gran", "Gujr", "Guru", "Knda", "Latn", "Mlym", "Orya", "Taml", "Telu", "Tirh", }},


                In that case we need to split a range. in the above example:
                                rangeStart = 0x951, rangeEnd = 0x951, scriptNameShort = "Zinh",
                                rangeStart = 0x952, rangeEnd = 0x952, scriptNameShort = "Zinh", scriptNamesShort = new string[]{"Beng", "Deva", "Gran", "Gujr", "Guru", "Knda", "Latn", "Mlym", "Orya", "Taml", "Telu", "Tirh", }},
                                rangeStart = 0x953, rangeEnd = 0x954, scriptNameShort = "Zinh"


                */

                var cpsExt = new CodepointScriptExtended
                { rangeStart = rangeStart, rangeEnd = rangeEnd, scriptNamesExtendedShort = scriptNamesShortList};

                codepointScriptsExtendedRaw.Add(cpsExt);
            }

            codepointScriptsExtendedRaw.Sort((item1, item2) => item1.rangeStart.CompareTo(item2.rangeStart));

            lastCpsExt = null;

            foreach (var cpsExt in codepointScriptsExtendedRaw)
            {
                /* scripts.txt includes many ranges that could be expressed as part of a larger contigous range.
                The reason for that is that the smaller ranges have also some data attributes written in the comment, which 
                we do not use. Therefore, where possible, extend ranges instead of adding a new ones. This reduces number
                of ranges by about 50%.

                example:
                102E0         ; Arab Copt # Mn       COPTIC EPACT THOUSANDS MARK
                102E1..102FB  ; Arab Copt # No  [27] COPTIC EPACT DIGIT ONE..COPTIC EPACT NUMBER NINE HUNDRED
                can be combined into a range 102E0..102FB, Arab Copt

                */

                if (lastCpsExt != null && cpsExt.rangeStart == lastCpsExt.rangeEnd + 1 && lastCpsExt.scriptNamesExtendedShort.SequenceEqual(cpsExt.scriptNamesExtendedShort))
                {
                    lastCpsExt.rangeEnd = cpsExt.rangeEnd;
                }
                else
                {
                    var newCpsExt = new CodepointScriptExtended
                    {
                        rangeStart = cpsExt.rangeStart,
                        rangeEnd = cpsExt.rangeEnd,
                        scriptNamesExtendedShort = cpsExt.scriptNamesExtendedShort
                    };

                    codepointScriptsExtended.Add(newCpsExt);

                    lastCpsExt = newCpsExt;
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
            scripts = new Script[]
            {
");

            foreach (var s in scripts)
            {
                // like:  new ScriptName {shortName = "Latn", longName = "Latin"},

                sb.AppendLine($"\t\t\t\tnew Script {{shortName = \"{s.shortName}\", longName = \"{s.longName}\", tempIndex = {s.tempIndex}}},");
            }

            sb.AppendLine(@"
            };

            codepointScripts = new CodepointScript[]
            {
            ");

            foreach (var cp in codepointScripts)
            {
                // like: new CodepointScript { rangeStart = 1, rangeEnd = 2, scriptNameShort = "a"}

                string s = string.Format("\t\t\t\tnew CodepointScript {{rangeStart = 0x{0}, rangeEnd = 0x{1}, script = scripts[{2}]}},",
                    cp.rangeStart.ToString("X"), cp.rangeEnd.ToString("X"), cp.script.tempIndex);
                sb.AppendLine(s);

            }

            sb.AppendLine(@"
            };

            codepointScriptsExtended = new CodepointScriptExtended[]
            {
            ");

            foreach (var cp in codepointScriptsExtended)
            {
                // like: new CodepointScript { rangeStart = 1, rangeEnd = 2, scriptNameShort = "a"}

                string scriptNamesShort = "new string[]{";
                foreach (string sn in cp.scriptNamesExtendedShort)
                    scriptNamesShort += $"\"{sn}\", ";
                scriptNamesShort += "}";

                string s = string.Format("\t\t\t\tnew CodepointScriptExtended {{rangeStart = 0x{0}, rangeEnd = 0x{1}, scriptNamesShort = {2}}},",
                    cp.rangeStart.ToString("X"), cp.rangeEnd.ToString("X"), scriptNamesShort);
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

            File.WriteAllText("Output/UnicodeScriptDetectorNetInit.cs", sb.ToString());

        }


        private class CodepointScriptForJSON
        {
            public int rs, re;
            public string ss;
        }

        public class ScriptNameForJSON
        {
            public string sn;
            public string ln;
            public int ti;   // our internal index. Can change when importing newer version of Unicode data!!! Don't use this value in persisten data such as DB!!!
        }

        static private void WriteJSONDataDefs()
        {
            var i = 0;

            var snsjs = new ScriptNameForJSON[scripts.Count];

            foreach (var sn in scripts)
            {
                snsjs[i++] = new ScriptNameForJSON
                {
                    sn = sn.shortName,
                    ln = sn.longName,
                    ti = sn.tempIndex
                };
            }

            string jsonStr = JsonConvert.SerializeObject(snsjs);//, Newtonsoft.Json.Formatting.Indented);

            if (!Directory.Exists("Output"))
                Directory.CreateDirectory("Output");

            jsonStr = "var scriptNames = " + jsonStr + ";";
            File.WriteAllText("Output/UnicodeScriptDefs.js", jsonStr);


            var cpsjs = new CodepointScriptForJSON[codepointScripts.Count];
            i = 0;

            foreach (var cps in codepointScripts)
            {
                cpsjs[i++] = new CodepointScriptForJSON
                {
                    rs = cps.rangeStart,
                    re = cps.rangeEnd,
                    ss = cps.script.shortName
                };
            }

            jsonStr = JsonConvert.SerializeObject(cpsjs);//, Newtonsoft.Json.Formatting.Indented);

            if (!Directory.Exists("Output"))
                Directory.CreateDirectory("Output");

            jsonStr = "var codepointScripts = " + jsonStr + ";";
            File.AppendAllText("Output/UnicodeScriptDefs.js", jsonStr);
        }
    }

}
