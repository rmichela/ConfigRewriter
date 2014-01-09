using System.Collections.Generic;
using System.Management.Automation;

namespace ConfigRewriter
{
    [Cmdlet(VerbsCommon.Get, "WebConfigKey")]
    public class GetWebConfigKeyCommand : HttpCommandBase
    {
        [Parameter]
        public string Key { get; set; }

        protected override void ProcessRecord()
        {
            Dictionary<string, string> translations = FetchTranslationTable(JsonUrl);

            if (!string.IsNullOrEmpty(Key))
            {
                string value;
                if (translations.TryGetValue(Key, out value))
                {
                    WriteObject(value);
                }
                else
                {
                    ThrowTerminatingError(new ErrorRecord(new KeyNotFoundException(Key), "KeyNotFound", ErrorCategory.InvalidArgument, translations));
                }
            }
            else
            {
                WriteObject(translations);
            }
        }
    }
}
