using CommandLine;
using CommandLine.Text;

namespace UploadAttachmentsToJira
{
    internal class Options
    {
        [Option('s', "server", Required = true, HelpText = "JIRA Server URL")]
        public string JiraServerUrl { get; set; }

        [Option('u', "username", Required = true, HelpText = "JIRA Username")]
        public string Username { get; set; }

        [Option('p', "password", Required = true, HelpText = "JIRA Password")]
        public string Password { get; set; }

        [Option('r', "project", Required = true, HelpText = "Project Key")]
        public string ProjectKey { get; set; }

        [Option('i', "inputpath", Required = true, HelpText = "Path where attachments are stored. Attachment filenames should be formatted '{ISSUE-ID} - AttachmentName.Ext' where ISSUE-ID is just the number of the JIRA item to use")]
        public string InputPath { get; set; }

        [Option('o', "overwrite", Required = false, HelpText = "Overwrites existing attachments")]
        public bool Overwrite {  get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
