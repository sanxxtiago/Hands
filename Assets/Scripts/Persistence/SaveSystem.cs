using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class SaveSystem
{
    private static readonly JsonSerializerSettings Settings =
    new()
    {
        Formatting = Formatting.Indented
    };

    public static void Save<T>(string fileName, T data)
    {
        string json = JsonConvert.SerializeObject(
            data,
           Settings);

        File.WriteAllText(GetPath(fileName), json);
    }

    public static T Load<T>(string fileName)
    {
        string path = GetPath(fileName);

        if (!File.Exists(path))
            return default;

        string json = File.ReadAllText(path);

        return JsonConvert.DeserializeObject<T>(json);
    }

    public static bool Exists(string fileName)
    {
        return File.Exists(GetPath(fileName));
    }

    public static void Delete(string fileName)
    {
        string path = GetPath(fileName);

        if (File.Exists(path))
            File.Delete(path);
    }

    private static string GetPath(string fileName)
    {
        return Path.Combine(
            Application.persistentDataPath,
            $"{fileName}.json");
    }

}