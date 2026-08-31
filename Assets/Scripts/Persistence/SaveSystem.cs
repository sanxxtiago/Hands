using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class SaveSystem
{
    private const string AppDirectoryName = "app";
    private const string ProfilesDirectoryName = "profiles";

    private static readonly JsonSerializerSettings Settings =
    new()
    {
        Formatting = Formatting.Indented
    };

    public static void Save<T>(string fileName, T data)
    {
        SaveToPath(GetPath(fileName), data);
    }

    public static void Save<T>(string userId, string fileName, T data)
    {
        SaveToPath(GetPath(userId, fileName), data);
    }

    public static T Load<T>(string fileName)
    {
        return LoadFromPath<T>(GetPath(fileName));
    }

    public static T Load<T>(string userId, string fileName)
    {
        return LoadFromPath<T>(GetPath(userId, fileName));
    }

    public static bool Exists(string fileName)
    {
        return File.Exists(GetPath(fileName));
    }

    public static bool Exists(string userId, string fileName)
    {
        return File.Exists(GetPath(userId, fileName));
    }

    public static void Delete(string fileName)
    {
        DeletePath(GetPath(fileName));
    }

    public static void Delete(string userId, string fileName)
    {
        DeletePath(GetPath(userId, fileName));
    }

    private static void SaveToPath<T>(string path, T data)
    {
        string json = JsonConvert.SerializeObject(data, Settings);
        string directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

        try
        {
            using (FileStream stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.Write(json);
                writer.Flush();
                stream.Flush(true);
            }

            if (File.Exists(path))
                File.Replace(temporaryPath, path, null);
            else
                File.Move(temporaryPath, path);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static T LoadFromPath<T>(string path)
    {

        if (!File.Exists(path))
            return default;

        string json = File.ReadAllText(path);

        return JsonConvert.DeserializeObject<T>(json);
    }

    private static void DeletePath(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private static string GetPath(string fileName)
    {
        return Path.Combine(
            Application.persistentDataPath,
            AppDirectoryName,
            $"{fileName}.json");
    }

    private static string GetPath(string userId, string fileName)
    {
        return Path.Combine(
            Application.persistentDataPath,
            AppDirectoryName,
            ProfilesDirectoryName,
            userId,
            $"{fileName}.json");
    }
}
