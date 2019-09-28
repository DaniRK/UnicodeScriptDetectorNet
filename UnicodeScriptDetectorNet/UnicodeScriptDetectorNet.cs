using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable 1591 // disable  "is never assigned to"


namespace UnicodeScriptDetectorNet
{
    static public partial class UnicodeScriptDetector
    {
        internal class ScriptName
        {
            public string shortName;
            public string longName;
            public int tempIndex;   // our internal index. Can change when importing newer version of Unicode data!!! Don't use this value in persisten data such as DB!!!
        }
        internal class ScriptNames : List<ScriptName> { }

        internal class CodepointScript
        {
            public int rangeStart, rangeEnd;
            public string scriptNameShort;
            public int tempIndex;   // our internal index. Can change when importing newer version of Unicode data!!! Don't use this value in persisten data such as DB!!!
        }
        internal class CodepointScripts : List<CodepointScript> { }

        public class Result
        {
            public string scriptNameShort;
            public string scriptNameLong;
            public float propabilty;
        }

        public class Results : List<Result> { }

        // short names for special Script names 

        internal const string ScriptShortInherited = "Zinh"; // Inherited
        internal const string ScriptShortCommon = "Zyyy";    // Common
        internal const string ScriptShortUnknown = "Zzzz";   // Unknown

        static internal ScriptNames scriptNames;
        static internal CodepointScripts codepointScripts;


        static UnicodeScriptDetector()
        {
            InitializeData();
        }

        /// <summary>
        /// Returns a list of possible "writing scripts" (like 'Latin', 'Arabic') that might have been used to write the specified text, together with a probablity for each
        /// Multiple scripts may be returned if a text either is composed of mixed scripts OR if only codePoints where used that belong
        /// to multiple scripts.
        /// An empty list will be returned if no script at all could be detected (such as "123," which only contains 'common' codepoints)
        /// 
        /// </summary>
        /// <param name="testText">Text to evaluate</param>
        /// <param name="ignoreInherited">If true: special characters that inherit their script from the preceeding character are not counted</param>
        /// <returns>Result list</returns>
        static public Results GetUsedScripts(string testText, bool ignoreInherited = true /*, bool useExtendedProperties = false*/ )
        {
            // for logic and technical details, see http://www.unicode.org/reports/tr24/

            int[] buckets = new int[scriptNames.Count];
            int totalRelevantCharacters = 0;

            CodepointScript lastCps = null; // for inheritance

            foreach (char c in testText)
            {
                var codePoint = Convert.ToInt32(c);

                var cps = codepointScripts.Where(cs => cs.rangeStart <= codePoint && cs.rangeEnd >= codePoint).FirstOrDefault();

                if (cps == null) // not in table, ignore
                    continue;

                if (cps.scriptNameShort == ScriptShortUnknown)
                    continue;

                if (cps.scriptNameShort == ScriptShortCommon)
                    continue;

                if (cps.scriptNameShort == ScriptShortInherited)
                {
                    if (ignoreInherited)
                        continue;

                    if (lastCps == null) // should not happen in real written text
                        continue;
                    else
                        cps = lastCps;
                }
                    
                lastCps = cps;
                totalRelevantCharacters++;

                buckets[cps.tempIndex]++;

            }

            Results results = new Results();

            for (int i = 0; i < buckets.Length; i++)
            {
                var bucket = buckets[i];
                if (bucket > 0)
                {

                    float p = (float)bucket / totalRelevantCharacters;
                    var scriptName = scriptNames.Where(sn => sn.tempIndex == i).First();

                    Console.WriteLine($"script {scriptName.longName}: {p}%");

                    results.Add(new Result
                    {
                        scriptNameShort = scriptName.shortName,
                        scriptNameLong = scriptName.longName,
                        propabilty = p
                    });
                }
            }

            results.Sort((item1, item2) => item2.propabilty.CompareTo(item1.propabilty)); // highest probability first

            return results;
        }
    }

}