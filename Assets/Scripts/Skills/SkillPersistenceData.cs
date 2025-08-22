using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class SkillPersistenceData
{
    public int skillPoints = 0;
    public string[] unlockedSkillIds = new string[0];
    public long lastSaveTimeStamp = 0;

    public SkillPersistenceData()
    {
        skillPoints = 0;
        unlockedSkillIds = new string[0];
        lastSaveTimeStamp = System.DateTime.Now.ToBinary();
    }

    public SkillPersistenceData(int skillPoints, HashSet<string> unlockedSkillIds)
    {
        this.skillPoints = skillPoints;

        this.unlockedSkillIds = new string[unlockedSkillIds.Count];
        unlockedSkillIds.CopyTo(this.unlockedSkillIds);

        this.lastSaveTimeStamp = System.DateTime.Now.ToBinary();
    }
}

public static class SkillSaveSystem
{
    private const string SAVE_KEY = "SkillTreeProgress";
    private const string SAVE_FILE_NAME = "skill_progress.json";

    public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

    public static void SaveSkillProgress(SkillPersistenceData data)
    {
        try
        {
            string jsonData = JsonUtility.ToJson(data, true);

            // Save to file ( primary method)
            File.WriteAllText(SaveFilePath, jsonData);

            // Also save to PlayerPrefs
            PlayerPrefs.SetString(SAVE_KEY, jsonData);
            PlayerPrefs.Save();

            Debug.Log($"Saved skill progress to {SaveFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving skill progress: {e.Message}");
        }
    }
    public static SkillPersistenceData LoadSkillProgress()
    {
        SkillPersistenceData data = null;

        try
        {
            // Try loading from file
            if (File.Exists(SaveFilePath))
            {
                string jsoNData = File.ReadAllText(SaveFilePath);
                data = JsonUtility.FromJson<SkillPersistenceData>(jsoNData);
                Debug.Log($"Loaded skill progress from {SaveFilePath}");

            }

            else if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string jsonData = PlayerPrefs.GetString(SAVE_KEY);
                data = JsonUtility.FromJson<SkillPersistenceData>(jsonData);
                Debug.Log($"Loaded skill progress from PlayerPrefs");


            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading skill progress: {e.Message}");
        }

        if (data == null)
        {
            Debug.Log("Creating new skill progress data");
            data = new SkillPersistenceData();

        }
        return data;

    }

    public static bool SaveExists()
    {
        return File.Exists(SaveFilePath) || PlayerPrefs.HasKey(SAVE_KEY);
    }

    public static void DeleteSave()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
            }

            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                PlayerPrefs.DeleteKey(SAVE_KEY);
                PlayerPrefs.Save();

            }
            Debug.Log("Deleted skill progress save");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error deleting skill progress save: {e.Message}");
        }
    }

    public static long GetLastSaveTime()
    {
        SkillPersistenceData data = LoadSkillProgress();
        return data.lastSaveTimeStamp;
    }
}

