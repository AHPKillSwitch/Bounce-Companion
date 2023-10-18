using System;
using System.Collections.Generic;
using System.Linq;
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
                    var latestVersion = latestRelease.TagName;

                    // Compare versions
                    if (new Version(latestVersion) > new Version(currentVersion))
                    {
                        return true; // A new version is available
                    }
                }

                return false; // No new version available
            }
        }
    }
}
