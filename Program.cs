using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UploadAttachmentsToJira
{
    class Program
    {
        private static HttpClient client;
        private static Options options;

        static void Main(string[] args)
        {
            options = new Options();
            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Console.WriteLine(options.GetUsage());
                return;
            }

            var files = Directory.GetFiles(options.InputPath);

            Console.WriteLine($"Uploading {files.Count()} attachments to Jira");

            SetupHttpClient();

            foreach (var file in files)
            {
                Task.WaitAll(UploadFileToJira(file));
            }
        }

        private static void SetupHttpClient()
        {
            client = new HttpClient();
            var usernamePassword = $"{options.Username}:{options.Password}";
            var encodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(usernamePassword));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedUsernamePassword);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("X-Atlassian-Token", "nocheck");
        }

        private static async Task UploadFileToJira(string file)
        {
            var url = IssueUrl(file);
            var response = await client.GetStringAsync(url);

            dynamic issue = JObject.Parse(response);
            var info = GetIssueInfo(file);

            foreach (var attachment in issue.fields.attachment)
            {
                if (attachment.filename != info.OriginalFilename)
                    continue;

                if (!options.Overwrite)
                {
                    Console.WriteLine($"{issue.key} already contains '{info.OriginalFilename}' skipping...");
                    return;
                }

                Console.WriteLine($"{issue.key} Replacing existing attachment '{info.OriginalFilename}'");
                await client.DeleteAsync(attachment.self.ToString());
            }

            Console.WriteLine($"{issue.key} uploading '{info.OriginalFilename}'");

            var form = new MultipartFormDataContent
            {
                { new StreamContent(File.OpenRead(file)), "\"file\"", info.OriginalFilename }
            };

            await client.PostAsync(IssueUrl(file) + "/attachments", form);
        }

        private static IssueInfo GetIssueInfo(string file)
        {
            var parts = Path.GetFileName(file)
                .Split(new[] {" - "}, StringSplitOptions.RemoveEmptyEntries);

            return new IssueInfo
            {
                IssueKey = $"{options.ProjectKey}-{parts[0]}",
                OriginalFilename = string.Join(" - ", parts.Skip(1))
            };

        }

        private static string IssueUrl(string file)
        {
            return $"{options.JiraServerUrl}/rest/api/latest/issue/{GetIssueInfo(file).IssueKey}";
        }
    }

    internal class IssueInfo
    {
        public string IssueKey { get; set; }
        public string OriginalFilename { get; set; }
    }
}
