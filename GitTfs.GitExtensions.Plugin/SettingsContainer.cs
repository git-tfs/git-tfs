using System;
using System.Linq;
using GitUIPluginInterfaces;

namespace GitTfs.GitExtensions.Plugin
{
    public class SettingsContainer
    {
        private readonly IGitPluginSettingsContainer _container;

        public SettingsContainer(IGitPluginSettingsContainer container)
        {
            _container = container;
        }

        public string TfsRemote
        {
            get { return _container.GetSetting(SettingKeys.TfsRemote); }
            set { _container.SetSetting(SettingKeys.TfsRemote, value); }
        }

        public PullSetting? PullSetting
        {
            get { return GetEnumSettingValue<PullSetting>(SettingKeys.Pull); }
            set { SetEnumSettingValue(SettingKeys.Pull, value); }
        }

        public PushSetting? PushSetting
        {
            get { return GetEnumSettingValue<PushSetting>(SettingKeys.Push); }
            set { SetEnumSettingValue(SettingKeys.Push, value); }
        }

        public ShelveSettingsContainer ShelveSettings
        {
            get { return new ShelveSettingsContainer(_container); }
        }

        private T? GetEnumSettingValue<T>(string key)
            where T : struct
        {
            var type = typeof (T);
            var value = _container.GetSetting(key);

            return (from name in Enum.GetNames(type)
                    where name == value
                    select (T?) Enum.Parse(type, name)).FirstOrDefault();
        }

        private void SetEnumSettingValue<T>(string key, T? value)
            where T : struct
        {
            _container.SetSetting(key, value.HasValue ? value.ToString() : string.Empty);
        }
    }
}
