using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Threading;

public class GameData : MonoBehaviour
{
    [SerializeField] int version;
    [Tooltip("Preview only! All changes are code based")]
    [SerializeField] Data progress;
    public Data Progress { get; private set; }

    private string path;
    private Encoding outputEnc = new UTF8Encoding(false);

    private Configuration _config;

    static CancellationTokenSource cts = new CancellationTokenSource();
    CancellationToken ct = cts.Token;

    public void Initialize(Configuration config)
    {
        _config = config;

        Load();
    }

    private void OnApplicationFocus(bool focus)
    {
        if (!focus && Progress != null)
            Save();
    }

    private void SetPath() 
    {
        path = Path.Combine(Application.persistentDataPath, $"gameData.json");
    }

    public void Save() 
    {
        SetPath();

        try
        {
            var json = JsonConvert.SerializeObject(Progress);
            File.WriteAllText(path, json, outputEnc);

#if UNITY_EDITOR
            Extensions.Clone(Progress, progress);
#endif
        }
        catch (IOException e)
        {
            Debug.LogWarning($"Cannot save game data! {e}");
        }
    }

    public void Load()
    {
        SetPath();

        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path, outputEnc);
                Progress = JsonConvert.DeserializeObject<Data>(json);

                if (Progress.version != version)
                {
                    Debug.Log($"Clear game data!");

                    Progress = new Data();
                    Progress.version = version;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load game data! {e}");
                Progress = new Data();
                Progress.version = version;
            }
        }
        else 
        {
            Progress = new Data();
            Progress.version = version;
        }

        UpdateDataToGameContent();
        Save();
    }

    private void UpdateDataToGameContent()
    {
        try
        {
            var saved = Progress.locations.Count;
            var existing = _config.locations.Length;

            // Fit locations count
            if (saved > existing)
                Progress.locations.RemoveRange(existing, saved - existing);

            foreach (var location in _config.locations.Where(x => GetLocation(x.id) == null))
            {
                Progress.locations.Add(new Location { id = location.id });
            }

            // Fit levels count
            for (int i = 0; i < Progress.locations.Count; i++)
            {
                saved = Progress.locations[i].levels.Count;
                existing = _config.locations[i].levels.Length;

                if (saved > existing)
                    Progress.locations[i].levels.RemoveRange(existing, saved - existing);

                while (saved < existing)
                {
                    saved++;
                    Progress.locations[i].levels.Add(new Location.Level());
                }
            }

            // Reorder locations
            var locations = new List<Location>();

            foreach (var location in _config.locations)
                locations.Add(GetLocation(location.id));

            Progress.locations = locations;

            // Set current location and level
            var currentLocation = GetLocation(Progress.location);

            if (currentLocation != null)
            {
                var capacity = currentLocation.levels.Count - 1;

                if (Progress.level > capacity)
                    Progress.level = capacity;
            }
            else
            {
                var nextLocation = Progress.locations.FirstOrDefault(x => !x.IsCompleted());

                if (nextLocation == null)
                    nextLocation = Progress.locations.Last();

                Progress.level = 0;
            }

            // Update skins
            foreach (var skin in _config.skins) 
            {
                if (!Progress.skins.Any(x => x.id.Equals(skin.id)))
                    Progress.skins.Add(new Skin { id = skin.id });
            }

            Progress.skins[0].unlocked = true;
        }
        catch (Exception e)
        {
            Progress = new Data();
            Progress.version = version;
            Debug.LogWarning($"Failed to update saved game data with content! {e} {e.StackTrace}");
        }
    }

    public Location GetLocation(string id) 
    {
        return Progress.locations.FirstOrDefault(x => x.id.Equals(id));
    }

    public Location.Level GetLevel(string location, int level) 
    {
        return Progress.locations.FirstOrDefault(x => x.id.Equals(location)).levels[level];
    }

    public int CompletedLevelsCount()
    {
        int count = 0;

        foreach (var location in Progress.locations)
            if(!location.id.Equals("tutorial"))
                count += location.CompletedLevelsCount();

        return count;
    }

    public void SetSkin(string skin) 
    {
        Progress.skin = skin;
    }

    public Skin GetSkin(string id) 
    {
        return Progress.skins.FirstOrDefault(x => x.id.Equals(id));
    }

    public void ExploreSkin(string id)
    {
        var skin = GetSkin(id);

        skin.explored = true;

        Save();
    }

    public void UnlockSkin(string id) 
    {
        var skin = GetSkin(id);

        skin.explored = true;
        skin.unlocked = true;

        Save();
    }

    [Serializable]
    public class Data
    {
        public int version;
        public string location;
        public int level;
        public bool completeTutorial;
        public int bonusKeys;
        public int money;
        public string skin;
        public bool locationBonus;
        public string bestCase;
        public int bestCaseType;
        public int bestCaseValue;
        public string bestCaseItem;

        public List<Skin> skins = new List<Skin>();
        public List<Location> locations = new List<Location>();

        public bool IsCompleted() 
        {
            return !locations.Any(x => !x.IsCompleted());
        }
    }

    [Serializable]
    public class Location 
    {
        public string id;
        public List<Level> levels = new List<Level>();

        public bool IsCompleted() 
        {
            return !levels.Any(x => !x.completed);
        }

        public int CompletedLevelsCount() 
        {
            return levels.Where(x => x.completed).Count();
        }

        [Serializable]
        public class Level 
        {
            public bool completed;
        }
    }

    [Serializable]
    public class Skin
    {
        public string id;
        public bool unlocked;
        public bool explored;
        public int watchedAds;
    }
}
