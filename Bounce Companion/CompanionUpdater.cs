using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Octokit;

namespace Bounce_Companion
{
    class CompanionUpdater
    {
        public class UpdateChecker
        {
            public static async Task<bool> IsNewVersionAvailable(string owner, string repo, string currentVersion)
            {
                var githubClient = new GitHubClient(new Octokit.ProductHeaderValue("Bounce-Companion"));
                var releases = await githubClient.Repository.Release.GetAll(owner, repo);

                if (releases.Any())
                {
                    // Get the latest release
                    var latestRelease = releases.First();
                    var latestVersion = latestRelease.TagName.Split('v')[1];

                    // Compare versions
                    if (new Version(latestVersion) > new Version(currentVersion))
                    {
                        return true; // A new version is available
                    }
                }

                return false; // No new version available
            }

            public static void DownloadLatestZipRelease(string owner, string repo)
            {
                var githubClient = new GitHubClient(new Octokit.ProductHeaderValue("Bounce-Companion"));
                var releases = githubClient.Repository.Release.GetAll(owner, repo).Result;

                if (releases.Any())
                {
                    // Get the latest release
                    var latestRelease = releases.First();
                    string latestVersionname = latestRelease.TagName;

                    // Ensure the release is a zip file by checking the name or other criteria
                    var downloadUrl = latestRelease.ZipballUrl;

                    if (downloadUrl != null)
                    {
                        using (WebClient webClient = new WebClient())
                        {
                            webClient.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");
                            webClient.DownloadFile(downloadUrl, "Bounce Companion "+ latestVersionname + ".zip"); // Save to a local file
                        }
                    }
                    else
                    {
                        // Handle the case where there are no zip assets
                    }
                }

            }
        }
    }
}
