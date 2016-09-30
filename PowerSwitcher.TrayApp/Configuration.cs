using System;
using System.IO;
using System.Xml.Serialization;

namespace PowerSwitcher.TrayApp
{

    public interface IConfigurationManger<T> where T : class, new()
    {
        void SerializeConfiguration(T configuration);
        T DeserializeOrDefault();
    }

    public class ConfigurationManager<T> where T : class, new()
    {
        public ConfigurationManager(string pathToFile)
        {
            pathToConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Petrroll", "PowerSwitcher", pathToFile);
        }

        private string pathToConfigFile;
        public void SerializeConfiguration(T configuration)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(pathToConfigFile));

                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (StreamWriter writer = File.CreateText(pathToConfigFile))
                {
                    serializer.Serialize(writer, configuration);
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException ||
                                            ex is PathTooLongException ||
                                            ex is DirectoryNotFoundException ||
                                            ex is FileNotFoundException ||
                                            ex is InvalidOperationException) { /* Eat exception on saving, do as if nothing happen :) (should log)*/ }

        }

        public T DeserializeOrDefault()
        {
            T configuration;
            if(tryToGetDeserialized(out configuration))
            {
                return configuration;
            }
            else
            {
                return new T();
            }
        }

        private bool tryToGetDeserialized(out T configuration)
        {
            configuration = default(T);
            if (!File.Exists(pathToConfigFile)) { return false; }


            try
            {
                configuration = deserializeConfiguration();
                return true;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException ||
                                            ex is PathTooLongException ||
                                            ex is DirectoryNotFoundException ||
                                            ex is FileNotFoundException ||
                                            ex is InvalidOperationException)
            {
                return false;
            }

        }


        private T deserializeConfiguration()
        {
            XmlSerializer xs = new XmlSerializer(typeof(T));
            T deserializedConfig = null;

            using (StreamReader reader = File.OpenText(pathToConfigFile))
            {
                deserializedConfig = (T)xs.Deserialize(reader);
            }
            return deserializedConfig;
        }
    }

    public class ConfigurationInstance<T> where T : class, new()
    {
        public T ConfigurationData { get; private set; }

        IConfigurationManger<T> configManager;
        public ConfigurationInstance(IConfigurationManger<T> configurationManager)
        {
            this.configManager = configurationManager;
            this.ConfigurationData = configurationManager.DeserializeOrDefault();
        }

        public void Save() => configManager.SerializeConfiguration(ConfigurationData);
    }


    [Serializable]
    public class ConfigurationData
    {

    }

}
