using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GitUIPluginInterfaces;

namespace GitTfs.GitExtensions.Plugin
{
    public class GitTfsPlugin : IGitPlugin
    {
        public void Register(IGitUICommands gitUiCommands)
        {
            var existingKeys = Settings.GetAvailableSettings();

            var settingsToAdd = from field in typeof(SettingKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
                                let key = (string)field.GetValue(null)
                                where !existingKeys.Contains(key)
                                select key;

            foreach (var settingToAdd in settingsToAdd)
            {
                Settings.AddSetting(settingToAdd, string.Empty);
            }

        }

        public void Execute(GitUIBaseEventArgs gitUiCommands)
        {
            if (string.IsNullOrEmpty(gitUiCommands.GitWorkingDir)) return;

            var remotes = GetTfsRemotes(gitUiCommands.GitUICommands);

            if (remotes.Any())
            {
                new GitTfsDialog(gitUiCommands.GitUICommands, PluginSettings, remotes).ShowDialog();
            }
            else
            {
                MessageBox.Show("The active repository has no TFS remotes.", "git-tfs Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static IEnumerable<string> GetTfsRemotes(IGitUICommands commands)
        {
            var result = commands.GitCommand("config --get-regexp tfs-remote");
            var match = Regex.Match(result, @"tfs-remote\.([^\.]+)");
            return match.Success
                       ? match.Groups.Cast<Group>().Skip(1).Select(g => g.Value)
                       : Enumerable.Empty<string>();
        }

        public string Description
        {
            get { return "git-tfs"; }
        }

        public IGitPluginSettingsContainer Settings { get; set; }

        public SettingsContainer PluginSettings
        {
            get { return new SettingsContainer(Settings); }
        }
    }
}
