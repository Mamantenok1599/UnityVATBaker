using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class LocalizationManager
{
    private static Dictionary<string, string> localizedTexts;
    private static string currentLanguage = "en";

    public static void LoadLocalization(string languageCode)
    {
        currentLanguage = languageCode;
        localizedTexts = new Dictionary<string, string>();

        string filePath = Path.Combine(Application.dataPath, "VATBaker/Localization", languageCode + ".json");

        if (File.Exists(filePath))
        {
            string jsonContent = File.ReadAllText(filePath);
            localizedTexts = JsonUtility.FromJson<LocalizationData>(jsonContent).ToDictionary();
        }
        else
        {
            Debug.LogError("Localization file not found: " + filePath);
        }
    }

    public static string Localize(string key, params object[] args)
    {
        if (localizedTexts != null && localizedTexts.ContainsKey(key))
        {
            return string.Format(localizedTexts[key], args);
        }
        else
        {
            return key;
        }
    }

    [System.Serializable]
    private class LocalizationData
    {
        public List<LocalizationItem> items;

        public Dictionary<string, string> ToDictionary()
        {
            var dict = new Dictionary<string, string>();
            foreach (var item in items)
            {
                dict[item.key] = item.value;
            }
            return dict;
        }
    }

    [System.Serializable]
    private class LocalizationItem
    {
        public string key;
        public string value;
    }
}