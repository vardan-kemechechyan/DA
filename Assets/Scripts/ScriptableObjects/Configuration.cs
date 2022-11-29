using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[CreateAssetMenu]
public class Configuration : ScriptableObject
{
    public bool poisonEnabled;
    public bool endLevelWithElevator;
    public bool slowmotion;
    public bool allowJumpOutOfRange;

    [Tooltip("0-100% swipe helper")][Range(0, 100)]
    public int correctSwipeChance;

    public string termsUrl;
    public string privacyPolicyUrl;

    public BonusCase[] bonusCases;
    public Sprite[] bonusCaseIcons;

    [Tooltip("Distance between spawned props")]
    public float sceneryRange = 1.0f;

    [Tooltip("Spawned props count")]
    public int sceneryPerLevel = 10;

    [Tooltip("Gap between obstacles and scenery")]
    public float sceneryGap = 2.0f;

    public PlayerMove[] playerMoves;
    public RelatedDirection[] relatedDirections;

    public Vector3 mainAnchor;

    public Color[] randomLaserColors;

    public Skin burnedSkin;
    public Skin[] skins;
    public Level[] tutorials;
    public Location[] locations;
    public Level giftLevel;

    public int shopAdReward = 300;

    public LaserColorMode laserColorMode;
    public CharactersAnimationMode characterMotionMode;

    public string failedClueMessage;
    public string[] floatingMessages;

    public AnimationCurve obstacleRewardByLevel;

    [HideInInspector]
    public float gameSpeed = 20.0f;

    public AudioClip menuMusic;

    public GameObject projectorPrefab;

    public GameObject elevatorPrefab;
    public GameObject jackpotPrefab;
    public GameObject giftPrefab;
    public GameObject cluePrefab;
    public GameObject keyPrefab;
    public GameObject poisonPrefab;
    public GameObject skinRenderer;

    public int locationKeysCount = 3;
    public int moreKeysCount = 1;

    public string importFilePath;

    public int giftLevelRate = 2;
    public int giftFinalReward = 200;

    public int unlockSkinRate = 5;

    [Range(0, 100)]
    public int giftSkinChance = 20;

    [Serializable]
    public class Skin
    {
        public string id;
        public Rarity rarity;
        [SerializeField]
        Availability[] availability;
        public int cost;
        public string storeId;
        public GameObject prefab;

        public int levelsToUnlock;
        public int adsToUnlock;

        public bool IsAvailable(Availability[] availability) 
        {
            foreach (var a in availability) 
            {
                foreach (var av in this.availability)
                    if (a == av)
                        return true;
            }

            return false;
        }

        public enum Rarity 
        {
            Usual,
            Rare,
            Epic,
            Legend,
            Event
        }

        public enum Availability 
        {
            Default,
            Gift,
            Case,
            Purchase
        }

        public Skin(GameObject prefab, int cost = 0)
        {
            id = prefab.name;
            this.cost = cost;
            this.prefab = prefab;
            availability = new Availability[] { Availability.Default };
        }
    }

    [Serializable]
    public class Location
    {
        public string id;
        public string title;

        [Range(0, 100)][Tooltip("0-100%")]
        public int gameSpeedMultiplier;
        [Range(0, 100)][Tooltip("0-100%")]
        public int slowMotionMultiplier;

        public AudioClip music;
        public Level[] levels;
        public GameObject lane;
        public string clue;
        public Reward reward;
        public int obstacleReward;
        public int clueReward;
        public bool hasPoison;
        public Sprite[] icons;
        public enum Reward
        {
            Location,
            Level
        }

        [Serializable]
        public class LaneSettings 
        {
            public Color floorColor, ceilingColor, wallsColor;
            public Texture floorTexture, ceilingTexture, wallsTexture;
            public Vector2 floorOffset, ceilingOffset, wallsOffset;
            public Vector2 floorTiling, ceilingTiling, wallsTiling;
        }
    }

    [Serializable]
    public class BonusCase
    {
        public Type type;
        public int money;

        [HideInInspector]
        public Skin skin;

        public enum Type
        {
            Money,
            Skin
        }
    }

    [Serializable]
    public class Level
    {
        public string data;
        public int obstacleReward;
        public int clueReward;
        public bool hasPoison;

        [Tooltip("Icon will be displayed on roadmap")]
        public LevelEvent levelEvent;

        public Obstacle[] obstacles = new Obstacle[0];
    }

    [Serializable]
    public class PlayerMove 
    {
        public SwipeDirection direction;
        public string animation;
        public float moveSpeed;
        public float returnSpeed;
        public float animationSpeed = 1.0f;
        public float duration = 1.0f;
        public float delay;
        public bool slowMotion;
        public bool tutorial;
    }

    [Serializable]
    public class Obstacle
    {
        public GameObject prefab;
        public bool tutorial;
        public SwipeDirection tutorialMove;
        public TimingZoneHint timingZoneHint;
        public string tutorialText;
        public string clue;
        public bool key;
        public SwipeDirection clueAnchor;

        public Color startColor = Color.white;
        public Color endColor = Color.white;

        [Serializable]
        public class Settings
        {
            public float distance = 75.0f;
            public float enableFlamersDistance = 1.0f;
            public float selectDistance = 1.0f;
            public float tutorialDistance = 1.0f;
            public float minDistance = 1.0f;
            public float criticalDistance = 1.0f;
            public float enterDistance = 1.0f;
            public float exitDistance = 1.0f;
            public float slowMotionDistance = 15.0f;
        }
    }

    [Serializable]
    public class RelatedDirection
    {
        public SwipeDirection direction;
        public List<SwipeDirection> related;
    }

    [Header("Obstacle settings")]
    public Obstacle.Settings sharedObstacleSettings;

    public GameObject[] alphabet;

    private void GenerateLevels() 
    {
        foreach (var location in locations)
        {
            if (location != locations[0])
            {
                foreach (var level in location.levels)
                {
                    level.obstacleReward = 10;
                    level.clueReward = 5;
        
                    //foreach (var obstacle in level.obstacles)
                    //{
                    //    obstacle.distance = 50;
                    //}
                }
            }
        }

        //var obstacles = Resources.LoadAll("Obstacles", typeof(GameObject)).Cast<GameObject>().ToArray();
        //
        //foreach (var location in locations) 
        //{
        //    if (location != locations[0]) 
        //    {
        //        foreach (var level in location.levels)
        //        {
        //            level.levelSpeed = 20;
        //            level.obstacleReward = 10;
        //            level.clueReward = 5;
        //
        //            foreach (var obstacle in level.obstacles)
        //            {
        //                obstacle.distance = 50;
        //                obstacle.prefab = obstacles[UnityEngine.Random.Range(0, obstacles.Length)];
        //            }
        //        }
        //    }
        //}
    }

    public void LoadSkins()
    {
        var s = Resources.LoadAll("Skins", typeof(GameObject)).Cast<GameObject>().ToArray();

        skins = new Skin[s.Length];

        for (int i = 0; i < skins.Length; i++)
            skins[i] = new Skin(s[i], 2000);
    }

    public void Fill() 
    {
        //alphabet = Resources.LoadAll($"Alphabet", typeof(GameObject)).Cast<GameObject>().ToArray();

        LoadSkins();
    }

    public void Validate() 
    {
        FixLinks();
        SetLocationKeys();
        
        foreach (var location in locations)
            foreach (var level in location.levels) 
            {
                level.hasPoison = true;
            }

        foreach (var location in locations)
        {
            var locationNumber = Array.IndexOf(locations, location) + 1;
        
            if (location.levels.Length <= 0)
                Debug.LogWarning($"Location: {location.id}({locationNumber}) levels empty!");
        
            foreach (var level in location.levels)
            {
                var levelNumber = Array.IndexOf(location.levels, level);
        
                foreach (var obstacle in level.obstacles) 
                {
                    if (!obstacle.prefab)
                        Debug.LogWarning($"Location: {location.id}({locationNumber}) " +
                            $"level: {levelNumber} obstacle: {Array.IndexOf(level.obstacles, obstacle)} not found!");
                }
            }
        }
    }

    public void FixLinks() 
    {
        //foreach (var l in locations)
        //    l.lanes = Resources.LoadAll($"Lanes/{l.id}", typeof(GameObject)).Cast<GameObject>().ToArray();

        var levelsData = File.ReadAllLines(Path.Combine(Application.dataPath, "levels.txt"));

        int levelsCount = 0;

        foreach (var location in locations)
        {
            foreach (var level in location.levels)
            {
                level.data = levelsData[levelsCount];
                levelsCount++;
            }
        }

        foreach (var location in locations)
        {
            foreach (var level in location.levels)
            {
                if (!string.IsNullOrEmpty(level.data) && level.data.Length > 1)
                {
                    var obstacles = level.data.Split('/');

                    level.obstacles = new Obstacle[obstacles.Length];

                    for (int i = 0; i < level.obstacles.Length; i++)
                    {
                        string data = obstacles[i];
                        var item = new Obstacle();

                        string path = "";

                        if (data.Contains("FL")) path = "Obstacles/Flamers";
                        else if (data.Contains("COMB")) path = "Obstacles/CombinedLasers";
                        else if (data.Contains("MOV")) path = "Obstacles/MovableLasers";
                        else if (data.Contains("CAM")) path = "Obstacles/Cameras";
                        else if (data.Contains("S")) path = "Obstacles/Lasers";

                        data = data.Replace("FL", "");
                        data = data.Replace("COMB", "");
                        data = data.Replace("MOV", "");
                        data = data.Replace("CAM", "");
                        data = data.Replace("S", "");

                        try
                        {
                            int index = int.Parse(data);

                            item.prefab = Resources.Load<GameObject>($"{path}/{index}");
                            //item.distance = 50;

                            level.obstacles[i] = item;
                        }
                        catch 
                        {
                            Debug.LogError($"Location: {location.title} Level: {Array.IndexOf(location.levels, level) + 1} obstacle: {i} data:{data}");
                        }
                    }
                }
            }
        }

        // Fix values
        foreach (var location in locations)
        {
            if (location != locations[0])
            {
                foreach (var level in location.levels)
                {
                    foreach (var obstacle in level.obstacles)
                    {
                        obstacle.clue = "";
                    }
                }
            }
        }

        foreach (var location in locations)
        {
            if (location != locations[0])
            {
                var letters = location.clue.Replace(" ", "").ToCharArray();

                letters.Shuffle();

                int letter = 0;

                foreach (var level in location.levels)
                {
                    level.obstacles.Last().clue = letters[letter].ToString();
                    letter++;
                }
            }
        }
    }

    private void SetLocationKeys() 
    {
        foreach (var location in locations) 
        {
            int count = 0;

            foreach (var level in location.levels) 
            {
                foreach (var obstacle in level.obstacles) 
                {
                    if (obstacle.key)
                        count++;
                }
            }

            if (count != locationKeysCount)
                SetLocationKeys(location);
        }
    }

    private void SetLocationKeys(Location location)
    {
        if (location == null || location.levels == null || location.levels.Length <= 0)
            return;

        foreach (var level in location.levels)
            foreach (var obstacle in level.obstacles)
                obstacle.key = false;

        var levels = new List<Level>();

        foreach (var l in location.levels)
        {
            levels.Add(l);
        }

        if (levels.Count < locationKeysCount)
        {
            Debug.LogWarning($"Location {location.id} has not enough levels to add bonus keys.");
            return;
        }

        levels.Shuffle();

        if (levels.Count > locationKeysCount)
            levels.RemoveRange(locationKeysCount, levels.Count - locationKeysCount);

        List<Obstacle> obstaclesWithKey = new List<Obstacle>();

        foreach (var l in levels)
        {
            var obstacles = (Obstacle[])l.obstacles.Clone();
        
            obstacles = obstacles.Where(x => string.IsNullOrEmpty(x.clue)).ToArray();
            obstacles.Shuffle();
        
            obstacles[0].key = true;
        }
    }

    public List<CSVData> csvData = new List<CSVData>();

    public void ImportCSV()
    {
        LoadCSV(File.ReadAllText(Path.Combine(Application.dataPath, "levels.csv")));

        // Requires setup access to sheets
        //GetLevels($"https://docs.google.com/spreadsheets/d/1XBfvwr-p618cLXEjzKnmb91T0EHktRqPcuv4KaPZZWA/export?format=csv", 
        //    result => { LoadCSV(result); });
    }

    private void LoadCSV(string data) 
    {
        csvData.Clear();

        Debug.Log(data);

        List<Dictionary<string, object>> items = CSVReader.Read(data);

        for (int i = 0; i < items.Count; i++)
        {
            csvData.Add(new CSVData
            {
                obstacles = items[i]["obstacles"].ToString(),
                speed = float.Parse(items[i]["speed"].ToString())
            });
        }
    }

    public static async void GetLevels(string url, Action<string> result)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.timeout = 5;

            await www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                //Debug.Log($"{www.error} {Encoding.UTF8.GetString(www.downloadHandler.data)}");

                result(null);
            }
            else
            {
                if (www.isDone)
                {
                    result(www.downloadHandler.text);
                }
            }
        }
    }

    [Serializable]
    public class CSVData 
    {
        public string obstacles;
        public float speed;
    }
}
