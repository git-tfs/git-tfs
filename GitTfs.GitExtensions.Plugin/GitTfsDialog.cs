using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GitUIPluginInterfaces;

namespace GitTfs.GitExtensions.Plugin
{
    public partial class GitTfsDialog : Form
    {
        private readonly IGitUICommands _commands;
        private readonly SettingsContainer _settings;

        public GitTfsDialog(IGitUICommands commands, SettingsContainer settings, IEnumerable<string> tfsRemotes)
        {
            _commands = commands;
            _settings = settings;

            InitializeComponent();
            TfsRemoteComboBox.DataSource = tfsRemotes.ToList();
            InitializeFromSettings();
        }

        private void InitializeFromSettings()
        {
            InitializeTfsRemotes();
            InitializePull();
            InitializePush();
        }

        private void InitializeTfsRemotes()
        {
            TfsRemoteComboBox.Text = _settings.TfsRemote;
        }

        private void InitializePull()
        {
            switch(_settings.PullSetting)
            {
                case PullSetting.Pull:
                    PullRadioButton.Checked = true;
                    break;
                case PullSetting.Rebase:
                    RebaseRadioButton.Checked = true;
                    break;
                case PullSetting.Fetch:
                    FetchRadioButton.Checked = true;
                    break;
            }

            SetPullButtonEnabledState();
        }

        private void MergeOptionCheckedChanged(object sender, EventArgs e)
        {
            SetPullButtonEnabledState();
        }

        private void SetPullButtonEnabledState()
        {
            PullButton.Enabled = PullRadioButton.Checked || RebaseRadioButton.Checked || FetchRadioButton.Checked;
        }

        private void PullButtonClick(object sender, EventArgs e)
        {
            if (PullRadioButton.Checked)
            {
                _settings.PullSetting = PullSetting.Pull;
                if (!_commands.StartGitTfsCommandProcessDialog("pull"))
                {
                    _commands.StartResolveConflictsDialog();
                }
            }
            else if (RebaseRadioButton.Checked)
            {
                _settings.PullSetting = PullSetting.Rebase;
                _commands.StartGitTfsCommandProcessDialog("fetch", TfsRemoteComboBox.Text);
                _commands.StartRebaseDialog("tfs/" + TfsRemoteComboBox.Text);
            }
            else if (FetchRadioButton.Checked)
            {
                _settings.PullSetting = PullSetting.Fetch;
                _commands.StartGitTfsCommandProcessDialog("fetch", TfsRemoteComboBox.Text);
            }
        }

        private void InitializePush()
        {
            switch (_settings.PushSetting)
            {
                case PushSetting.Checkin:
                    CheckinRadioButton.Checked = true;
                    break;
                case PushSetting.Shelve:
                    ShelveRadioButton.Checked = true;
                    break;
            }

            SetPushButtonEnabledState();
        }

        private void PushOptionCheckedChanged(object sender, EventArgs e)
        {
            SetPushButtonEnabledState();
        }

        private void SetPushButtonEnabledState()
        {
            PushButton.Enabled = CheckinRadioButton.Checked || ShelveRadioButton.Checked;
        }

        private void PushButtonClick(object sender, EventArgs e)
        {
            if (CheckinRadioButton.Checked)
            {
                _settings.PushSetting = PushSetting.Checkin;
                _commands.StartGitTfsCommandProcessDialog("checkintool");
            }
            else if (ShelveRadioButton.Checked)
            {
                _settings.PushSetting = PushSetting.Shelve;
                new ShelveDialog(_commands, _settings.ShelveSettings).ShowDialog();
            }
        }

        private void TfsRemoteComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _settings.TfsRemote = TfsRemoteComboBox.Text;
        }
    }
}
