using System;
using System.Windows.Forms;
using GitUIPluginInterfaces;

namespace GitTfs.GitExtensions.Plugin
{
    public partial class ShelveDialog : Form
    {
        private readonly IGitUICommands _commands;
        private readonly ShelveSettingsContainer _settings;

        public ShelveDialog(IGitUICommands commands, ShelveSettingsContainer settings)
        {
            _commands = commands;
            _settings = settings;

            InitializeComponent();
            InitializeFromSettings();
        }

        private void InitializeFromSettings()
        {
            NameTextBox.Text = _settings.Name;
            OverwriteCheckBox.Checked = _settings.Overwrite;
            SetShelveButtonEnabledState();
        }

        private void NameTextBoxTextChanged(object sender, EventArgs e)
        {
            SetShelveButtonEnabledState();
        }

        private void SetShelveButtonEnabledState()
        {
            ShelveButton.Enabled = !string.IsNullOrEmpty(NameTextBox.Text);
        }

        private void ShelveButtonClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(NameTextBox.Text)) return;

            _settings.Name = NameTextBox.Text;
            _settings.Overwrite = OverwriteCheckBox.Checked;

            _commands.StartGitTfsCommandProcessDialog("shelve", OverwriteCheckBox.Checked ? "-f " : "", NameTextBox.Text);
            Close();
        }
    }
}
