using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

#pragma warning disable 1591 // disable  "is never assigned to"

namespace UnicodeScriptDetectorNet
{
    static public partial class UnicodeScriptDetector
    {
        public enum ScriptType { Normal, Common, Inherited, Unknown };

        /// <summary>
        /// One of the writing scripts defined in Unicode
        /// </summary>
        [Serializable]
        public class Script
        {
            public string shortName;
            public string longName;
            public ScriptType type = ScriptType.Normal;
            public int tempIndex;   // internal index. Can change when importing newer version of Unicode data!!! Don't use this value in persistent data!
        }

        /// <summary>
        /// A range of codepoints belonging to the same script
        /// </summary>
        [Serializable]
        public class CodepointScript
        {
            public int rangeStart, rangeEnd;
            public Script script;
        }

        /// <summary>
        /// A range of codepoints with the same list of scripts found in codepoint extended properties  
        /// </summary>
        [Serializable]
        public class CodepointScriptExtended
        {
            public int rangeStart, rangeEnd;
            public string[] scriptNamesShort;
        }

        /// <summary>
        /// One of the results returned by detection methods
        /// </summary>
        public class Result
        {
            public string scriptNameShort;
            public string scriptNameLong;
            public float probabilty;
        }

        public class Results : List<Result> { }

        // short/long  names for special Script names 

        public const string ScriptShortInherited = "Zinh"; // Inherited
        public const string ScriptShortCommon = "Zyyy";    // Common
        public const string ScriptShortUnknown = "Zzzz";   // Unknown

        public const string ScriptLongInherited = "Inherited"; // Inherited
        public const string ScriptLongCommon = "Common";    // Common
        public const string ScriptLongUnknown = "Unknown";   // Unknown

        static internal Script[] scripts;
        static internal CodepointScript[] codepointScripts;
        static internal CodepointScriptExtended[] codepointScriptsExtended;

        static UnicodeScriptDetector()
        {
            // call InitializeData() in the partial class file UnicodeScriptDetectorNetInit.cs, 
            // which was created as output of the ImportDataToCSharp project
            InitializeData(); 

            // override the default script type for special types
            foreach (var s in scripts)
            {
                if (s.shortName == ScriptShortCommon)
                    s.type = ScriptType.Common;

                if (s.shortName == ScriptShortInherited)
                    s.type = ScriptType.Inherited;

                if (s.shortName == ScriptShortUnknown)
                    s.type = ScriptType.Unknown;
            }
        }

        /// <summary>
        /// Return a list of possible "writing scripts" (like 'Latin', 'Arabic') that might have been used to write the specified text, together with a probablity for each
        /// Multiple scripts may be returned if a text either is composed of mixed scripts OR if only codePoints where used that belong
        /// to multiple scripts.
        /// An empty list will be returned if the string is null or empty or if no script at all could be detected (such as "123," which only contains 'common' codepoints)
        /// 
        /// </summary>
        /// <param name="testText">Text to evaluate</param>
        /// <param name="ignoreInherited">If true: special characters that inherit their script from the preceeding character are not counted</param>
        /// <returns>Result list</returns>
        static public Results GetUsedScripts(string testText, bool ignoreInherited = true /*, bool useExtendedProperties = false*/ )
        {
            // for logic and technical details, see http://www.unicode.org/reports/tr24/

            if (testText == null || testText.Length == 1)
                return new Results();

            int[] buckets = new int[scripts.Length];
            int totalRelevantCharacters = 0;

            CodepointScript lastCps = null; // for inheritance

            //foreach (char c in testText)
            for (int charIndex = 0; charIndex < testText.Length; charIndex++)
            {
                //var codePoint = Convert.ToInt32(c);
                // .net/windows hold characters as utf16. Unicode codepoints > 0xffff are represented as 
                // two characters (using surrogates), therefor we cannot just loop through the characters and use their always 16 bit numeric value
                // (string length property grows accordingly)

                int codePoint = char.ConvertToUtf32(testText, charIndex);
                if (codePoint > 0xffff)
                    charIndex++;

                var cps = codepointScripts.Where(cs => cs.rangeStart <= codePoint && cs.rangeEnd >= codePoint).FirstOrDefault();

                if (cps == null) // not in table means implicitely ScriptShortUnknown
                    continue;

                if (cps.script.type == ScriptType.Unknown) // explicitly set to ScriptShortUnknown
                    continue;

                if (cps.script.type == ScriptType.Common)
                    continue;

                if (cps.script.type == ScriptType.Inherited)
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

                buckets[cps.script.tempIndex]++;
            }

            Results results = new Results();

            for (int i = 0; i < buckets.Length; i++)
            {
                var bucket = buckets[i];
                if (bucket > 0)
                {
                    float p = (float)bucket / totalRelevantCharacters;
                    var scriptName = scripts.Where(sn => sn.tempIndex == i).First();

                    Console.WriteLine($"script {scriptName.longName}: {p}%");

                    results.Add(new Result
                    {
                        scriptNameShort = scriptName.shortName,
                        scriptNameLong = scriptName.longName,
                        probabilty = p
                    });
                }
            }

            results.Sort((item1, item2) => item2.probabilty.CompareTo(item1.probabilty)); // reverse sort, highest probability first

            return results;
        }

        /// <summary>
        /// Returns the percentage (0.0 to 1.0) of characters in a string, that can be written in a specified script name.
        /// Common characters (e.g. digits, space) will pass the test for any script, unless strict is set to true.
        /// Null or empty strings will always return 1
        /// </summary>
        /// <param name="testText">Text to evaluate</param>
        /// <param name="scriptName">Short or long script name (e.g.'Latn', 'Latin')</param>
        /// <param name="strict">Common characters are not counted as belonging to the script</param>
        /// <param name="applyExtendedProperties">If a common character has extended properties limiting it to a list of scripts and none of them matches the scriptName parm, then the common character does not match. Ignored if strict parm is true  </param>
        /// <returns>value between 0.0 and 1.0, 1.0 for full fit</returns>
        /// <exception cref="System.ArgumentException">Thrown when an invalid scriptName is passed</exception>
        static public float ProbablyInScript(string testText, string scriptName, bool strict = false, bool applyExtendedProperties = true)
        {
            // The main difference to GetUsedScripts is in case of a text built only with common code points:
            // here we would return 1.0 for ANY specified scriptName, while GetUsedScripts() would return an empty result set (since it
            // no specific script can be detected)

            if (testText == null || testText.Length == 0)
                return 1;

            Script sn = scripts
                .Where(n => n.shortName.ToLower() == scriptName.ToLower() || n.longName.ToLower() == scriptName.ToLower()).FirstOrDefault();

            if (sn == null)
                throw new ArgumentException("Invalid short or long scriptName supplied", scriptName);

            string shortName = sn.shortName;


            // for logic and technical details, see http://www.unicode.org/reports/tr24/

            int inScriptCount = 0;

            CodepointScript lastCps = null; // for inheritance

            //foreach (char c in testText)
            for (int charIndex = 0; charIndex < testText.Length; charIndex++)
            {

                // .net/windows hold characters as utf16. Unicode codepoints > 0xffff are represented as 
                // two characters (using surrogates), therefor we cannot just loop through the characters and use their numeric value
                // (string length property grows accordingly)

                int codePoint = char.ConvertToUtf32(testText, charIndex);
                if (codePoint > 0xffff)
                    charIndex++;

                var cps = codepointScripts.Where(cs => cs.rangeStart <= codePoint && cs.rangeEnd >= codePoint).FirstOrDefault();

                if (cps == null) // not in table, implicitely Unknown and therefore not in script, this is a mismatch
                    continue;

                if (cps.script.type == ScriptType.Unknown) // implicitely Unknown, not in script, this is a mismatch
                    continue;

                if (cps.script.type == ScriptType.Common)
                {
                    // most common code points can be used in any script, so this is a match, unless strict parm is set to true
                    // but some common code point have extended scripts property, which says in which limited set of scripts the common might be used

                    if (strict)
                        continue; // not a match

                    if (applyExtendedProperties)
                    {
                        var cpsExtended = codepointScriptsExtended.Where(cs => cs.rangeStart <= codePoint && cs.rangeEnd >= codePoint).FirstOrDefault();

                        if (cpsExtended != null && !cpsExtended.scriptNamesShort.Contains(shortName))
                            continue; // not a match
                    }

                    inScriptCount++; //match
                    continue;
                }

                if (cps.script.type == ScriptType.Inherited)
                {
                    // inherit from preceeding character

                    if (lastCps == null) // inherited as first char should not happen in real written text, see this as a mismatch 
                        continue;
                    else
                    {
                        // though there are a few cases of inherited chars with extended properties, this is 
                        // meaningless in the context of this method

                        cps = lastCps;
                    }
                }

                if (cps.script.shortName == shortName)
                    inScriptCount++;

                lastCps = cps;
            }

            return (float)inScriptCount / testText.Length;
        }

        /// <summary>
        /// Returns true if all characters in a string can be written in a specified script name.
        /// Common characters (e.g. digits, space) will pass the test for any script, unless strict is set to true.
        /// Null or empty strings will always yield 0
        /// </summary>
        /// <param name="testText">Text to evaluate</param>
        /// <param name="scriptName">Short or long script name (e.g.'Latn', 'Latin')</param>
        /// <param name="strict">Common characters are not counted as belonging to the script</param>
        /// <param name="applyExtendedProperties">If a common character has extended properties limiting it to a list of scripts and none of them matches the scriptName parm, then it does not match. Ignored if strict parm is true  </param>
        static public bool IsInScript(string testText, string scriptName, bool strict = false, bool applyExtendedProperties = true)
        {
            return ProbablyInScript(testText, scriptName, strict, applyExtendedProperties) == 1.0;
        }

        /// <summary>
        /// Get a copy of all defined script names (short and long name)
        /// </summary>
        static public Script[] GetScripts()
        {
            return Clone<Script[]>(scripts);
        }

        /// <summary>
        /// Get a copy of all defined script ranges 
        /// </summary>
        static public CodepointScript[] GetCodepointScripts()
        {
            return Clone<CodepointScript[]>(codepointScripts);
        }

        /// <summary>
        /// Get a copy of all defined extended properties name ranges
        /// </summary>
        static public CodepointScriptExtended[] GetExtendedProperties()
        {
            return Clone<CodepointScriptExtended[]>(codepointScriptsExtended);
        }

        /// <summary>
        /// Perform a deep Copy of the object using a BinaryFormatter.
        /// IMPORTANT: the object class must be marked as [Serializable] and have an parameterless constructor.
        /// </summary>
        /// <typeparam name="T">The type of object being copied.</typeparam>
        /// <param name="source">The object instance to copy.</param>
        /// <returns>The copied object.</returns>
        public static T Clone<T>(this T source)
        {
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", "source");
            }

            // Don't serialize a null object, simply return the default for that object
            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            IFormatter formatter = new BinaryFormatter();
            using (Stream stream = new MemoryStream())
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }
    }
}