## UnicodeScriptDetectorNet Description and Examples

For details about writing scripts, codepoint ranges and extended properties please refer to 
http://www.unicode.org/reports/tr24/

### GetUsedScripts method  

**static public Results GetUsedScripts(string testText, bool ignoreInherited = true)**

Returns a list of possible "writing scripts" (like 'Latin', 'Arabic') that might have been used to write the specified text, together with a probablity for each

Multiple scripts may be returned if a text either is composed of mixed scripts OR if only codePoints where used that belong
to multiple scripts.

An empty list will be returned if the string is null or empty or if no script at all could be detected (such as "123," which only contains 'common' codepoints)


    var results = UnicodeScriptDetector.GetUsedScripts("Hello translates in Hebrew to: שלום");  
    foreach(var r in results)  
    {  
         Console.WriteLine($"Script short code: {r.scriptNameShort}, long code {r.scriptNameLong}, probablity: {r.propabilty}");  
    }  

    Output:  
    Script short code: Latn, long code Latin, probablity: 0.862069  
    Script short code: Hebr, long code Hebrew, probablity: 0.137931   


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




### ProbablyInScript method  
**static public float ProbablyInScript(string testText, string scriptName, bool strict = false, bool applyExtendedProperties = true)**

Returns the probabilty (0.0 to 1.0) that a given text is written in a specified script.

Either short or long script names (e.g.:'Latn' or 'Latin') can be passed as parameter. Exception is thrown when an invalid scriptName is passed.

If strict is set to true, common characters, such as numbers and spaces are not counted as belonging to the script.

If applyExtendedProperties is true, a common character that has extended properties limiting it to a list of scripts and none of them matches the scriptName parm, then it does not match. The parmater is ignored if strict parm is true.

Null or empty texts will always return 1.

The main difference to GetUsedScripts is in case of a text built only with common code points:
here we would return 1.0 for ANY specified scriptName, while GetUsedScripts() would return an empty result set (since no specific script can be detected).
This method can also takes into account extended code point properties, restricting common characters
to certain scripts.

    float p;
            
    // common characters only, should yield 1.0
    p = UnicodeScriptDetector.ProbablyInScript("123 +", "Latn");

    // common characters only, but one of them (0x0589) is restricted to the geogian and armenic scripts 
    // should yield 0.8
    p = UnicodeScriptDetector.ProbablyInScript("123 \u0589", "Latn");           

    // only latins and commons, should yield 1
    p = UnicodeScriptDetector.ProbablyInScript(" Abö Abö", "Latin");

    // 6 latins and 2 commons , should yield 2/8 = 0.25
    p = UnicodeScriptDetector.ProbablyInScript(" Abö Abö", "HEBREW");



### IsInScript method  

**        static public bool IsInScript(string testText, string scriptName, bool strict = false, bool applyExtendedProperties = true)**

This is just a helper function that calls ProbablyInScript(), returning true if ProbablyInScript returns 1.0, false otherwise.
For explanation and parameters see the ProbablyInScript method above. 

    // common characters only, should yield true
    b = UnicodeScriptDetector.IsInScript("123 +", "Latn");

    // common characters only, should yield false
    b = UnicodeScriptDetector.IsInScript("123 +", "Latn", strict:true);

    // only latin and common characters, should yield true
    b = UnicodeScriptDetector.IsInScript(" Abö Abö", "Latin");

    // 6 latin and 2 common characters, should yield false
    b = UnicodeScriptDetector.IsInScript(" Abö Abö", "hebrew");


### Methods to get the Unicode data structures
These methods can be used to get the underlying Unicode data structures for usage in your application.

They return a copy of the data itself, so modifying them has no impact on the detection methods.

	/// Get defined scripts, short and long name (identifier)  (from Unicode data file PropertyValueAliases.txt)

    foreach (var sn in UnicodeScriptDetector.GetScripts())
    {
        Console.WriteLine($"Short: '{sn.shortName}', Long: '{sn.longName}'");
    }

    /* Output:
        Short: 'Adlm', Long: 'Adlam'
        Short: 'Aghb', Long: 'Caucasian_Albanian'
        Short: 'Ahom', Long: 'Ahom'
        Short: 'Arab', Long: 'Arabic'          
        ...
    */


    /// Get a copy of all defined script ranges (from Unicode data file scripts.txt)
	CodepointScript[] codePointScripts = UnicodeScriptDetector.GetCodepointScripts();


    /// Get a copy of all defined extended properties name ranges (from Unicode data file ScriptExtensions.txt)
	CodepointScriptExtended[] codePointScriptsExtended = UnicodeScriptDetector.CodepointScriptExtended();




