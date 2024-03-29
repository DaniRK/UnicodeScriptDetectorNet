﻿# UnicodeScriptDetectorNet
This solution consists of two parts: 
* UnicodeScriptDetector: the actual .net library. 
* ImportDataToCSharp: Produces a C# code file (Initialize.cs) based on newest Unicode consortium data, that is then used in the UnicodeScriptDetector project. 


### NuGet:
UnicodeScriptDetectorNet

### Usage:

There are various detection methods and data extraction methods.
##### [See here](UnicodeScriptDetectorNet/Examples.md) for a complete list of methods and more examples.


As an example, the UnicodeScriptDetector.GetUsedScripts method returns a list of possible Unicode "writing scripts" (like 'Latin', 'Arabic', 'Cyrillic') that might have been used to write the specified text, together with a probablity for each.

The list may contain 0, 1 or multiple results:  
* 0 result: there is no identication of any script (for example for a string "123 +")  
* 1 result: only 1 script was detected  
* multiple results: this usually happens if the text was written using mixed scripts (e.g.: "Hello Привет", using Latin and Cyrillic script)

The probablity for each script (>0, <=1) is calculated as the number of characters in the text belonging to that script devided by the number of all characters that could be related to any script.

Returned script identifiers are the official Unicode short and long names.

### Example:  
    results = UnicodeScriptDetector.GetUsedScripts("Hello translates in Hebrew to: שלום");  
    foreach(var r in results)  
    {  
         Console.WriteLine($"Script short code: {r.scriptNameShort}, long code {r.scriptNameLong}, probablity:{r.propabilty}");  
    }  

    Output:  
    Script short code: Latn, long code Latin, probablity:0.862069  
    Script short code: Hebr, long code Hebrew, probablity:0.137931  

##### [See here](UnicodeScriptDetectorNet/Examples.md) for explanations and more examples.

This library is based on data from official data files version 12.0 from the Unicode consortium.

For more details of theory and logic: http://www.unicode.org/reports/tr24/  

