using System;
using System.IO;
using System.Text.Json;
using Donaldows_Vista_SP1_hotfix091016_Csharp.State;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Save
{
    public sealed class SaveManager
    {
        private readonly string _path = Path.Combine(AppContext.BaseDirectory, "Save", "save.json");

        public SaveData Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    var json = File.ReadAllText(_path);
                    var data = JsonSerializer.Deserialize(json, SaveJsonContext.Default.SaveData);
                    if (data is not null)
                    {
                        return data;
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (JsonException)
            {
            }

            return new SaveData();
        }

        public void Save(SaveData data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var json = JsonSerializer.Serialize(data, SaveJsonContext.Default.SaveData);
            File.WriteAllText(_path, json);
        }
    }
}
