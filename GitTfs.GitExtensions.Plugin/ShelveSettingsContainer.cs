using GitUIPluginInterfaces;

namespace GitTfs.GitExtensions.Plugin
{
    public class ShelveSettingsContainer
    {
        private readonly IGitPluginSettingsContainer _container;

        public ShelveSettingsContainer(IGitPluginSettingsContainer container)
        {
            _container = container;
        }

        public string Name
        {
            get { return _container.GetSetting(SettingKeys.ShelvesetName); }
            set { _container.SetSetting(SettingKeys.ShelvesetName, value); }
        }

        public bool Overwrite
        {
            get
            {
                bool result;
                return bool.TryParse(SettingKeys.OverwriteShelveset, out result) && result;
            }
            set
            {
                _container.SetSetting(SettingKeys.OverwriteShelveset, value.ToString());
            }
        }
    }
}
