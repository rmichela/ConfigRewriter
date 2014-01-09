using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Text;
using Microsoft.PowerShell.Commands;
using Newtonsoft.Json;

namespace ConfigRewriter
{
    public class HttpCommandBase : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string JsonUrl { get; set; }

        protected Dictionary<string, string> FetchTranslationTable(string url)
        {
            var translations = new Dictionary<string, string>();
            try
            {
                // Build the key=>value map for translating the config file
                var webRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                using (var reader = new StreamReader(webResponse.GetResponseStream()))
                {
                    string json = reader.ReadToEnd();
                    translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                }
            }
            catch (Exception ex)
            {
                ThrowTerminatingError(new ErrorRecord(ex, "ConnectionFailure", ErrorCategory.ReadError, this));
            }
            return translations;
        }

        // Implementation borrowed from http://stackoverflow.com/questions/8505294/how-do-i-deal-with-paths-when-writing-a-powershell-cmdlet
        protected IEnumerable<string> ResolvePaths(IEnumerable<string> paths, bool expandWildcards)
        {
            var allFilePaths = new List<string>();

            foreach (string path in paths)
            {
                ProviderInfo provider;
                PSDriveInfo drive;
                var filePaths = new List<string>();

                if (expandWildcards)
                {
                    filePaths.AddRange(GetResolvedProviderPathFromPSPath(path, out provider));
                }
                else
                {
                    filePaths.Add(SessionState.Path.GetUnresolvedProviderPathFromPSPath(path, out provider, out drive));
                }
                if (IsFileSystemPath(provider, path) == false)
                {
                    // no, so skip to next path in paths.
                    continue;
                }
                allFilePaths.AddRange(filePaths);
            }

            return allFilePaths;
        }

        protected bool IsFileSystemPath(ProviderInfo provider, string path)
        {
            bool isFileSystem = true;
            // check that this provider is the filesystem
            if (provider.ImplementingType != typeof(FileSystemProvider))
            {
                // create a .NET exception wrapping our error text
                ArgumentException ex = new ArgumentException(path +
                    " does not resolve to a path on the FileSystem provider.");
                // wrap this in a powershell errorrecord
                ErrorRecord error = new ErrorRecord(ex, "InvalidProvider",
                    ErrorCategory.InvalidArgument, path);
                // write a non-terminating error to pipeline
                WriteError(error);
                // tell our caller that the item was not on the filesystem
                isFileSystem = false;
            }
            return isFileSystem;
        }
    }
}
