using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using Microsoft.PowerShell.Commands;

namespace ConfigRewriter
{
    [Cmdlet(VerbsData.Merge, "WebConfig")]
    public class MergeWebConfigCommand : HttpCommandBase
    {
        private string[] _paths;
        private bool _shouldExpandWildcards;
        [Parameter(
            Position = 0,
            Mandatory = true,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Literal")
        ]
        [Alias("PSPath")]
        [ValidateNotNullOrEmpty]
        public string[] LiteralPath
        {
            get { return _paths; }
            set { _paths = value; }
        }
        [Parameter(
            Position = 0,
            Mandatory = true,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Wildcard")
        ]
        [ValidateNotNullOrEmpty]
        public string[] Path
        {
            get { return _paths; }
            set
            {
                _shouldExpandWildcards = true;
                _paths = value;
            }
        }

        protected override void ProcessRecord()
        {
            Dictionary<string, string> translations = FetchTranslationTable(JsonUrl);

            foreach (string resolvedPath in ResolvePaths(_paths, _shouldExpandWildcards))
            {
                PatchFile(resolvedPath, translations);              
            }
        }    

        private void PatchFile(string path, Dictionary<string, string> translations)
        {
            try
            {
                if (File.Exists(path))
                {
                    string fileContent = File.ReadAllText(path);
                    foreach (var kvp in translations)
                    {
                        fileContent = fileContent.Replace(string.Format("{{{{{0}}}}}", kvp.Key), kvp.Value); // Matches {{key}}
                    }
                    File.WriteAllText(path, fileContent);
                }
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "IoError", ErrorCategory.ReadError, path));
            }
        }
    }
}
