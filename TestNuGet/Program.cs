using System;

namespace TestNuGet
{
    class Program
    {
        static void Main(string[] args)
        {
            var results = UnicodeScriptDetectorNet.UnicodeScriptDetector.GetUsedScripts(" Abö Abö");

        }
    }
}
