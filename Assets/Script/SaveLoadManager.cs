using System;
using System.Text;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SaveLoadManager : Singleton<SaveLoadManager>
{
    #region Data Structures
    [Serializable]
    public class HighScoreEntry
    {
        public string playerName;
        public float timeRemaining;
        public string timestamp;
    }

    [Serializable]
    private class SaveData
    {
        public List<HighScoreEntry> highScores = new List<HighScoreEntry>();
    }
    #endregion

    #region Constants
    private const int MAX_HIGH_SCORES = 10;
    private const string PLAYER_NAME_KEY = "PlayerName";
    private const string BALL_INDEX_KEY = "SelectedBallIndex";
    private const string VOLUME_KEY = "MasterVolume";
    private const string CHECKSUM_DELIMITER = "|CHECKSUM|";
    private const string ENCRYPTION_KEY_PREFIX = "SAFE_";
    #endregion

    #region UI Integration
    [Header("UI References")]
    [SerializeField] private TMP_Text highScoreTextPrefab;
    [SerializeField] private Transform highScoreContainer;
    #endregion

    #region Encryption Fields
    private byte[] _encryptionKey;
    private byte[] _encryptionIV;
    #endregion

    #region File Management
    private string _saveFilePath;
    private SaveData _currentSaveData;
    #endregion

    #region Initialization
    protected override void Awake()
    {
        base.Awake();
        InitializeEncryption();
        InitializeSaveSystem();
    }

    private void InitializeEncryption()
    {
        if (!PlayerPrefs.HasKey(ENCRYPTION_KEY_PREFIX + "Key"))
        {
            using (Aes aes = Aes.Create())
            {
                aes.GenerateKey();
                aes.GenerateIV();
                PlayerPrefs.SetString(ENCRYPTION_KEY_PREFIX + "Key", Convert.ToBase64String(aes.Key));
                PlayerPrefs.SetString(ENCRYPTION_KEY_PREFIX + "IV", Convert.ToBase64String(aes.IV));
            }
            PlayerPrefs.Save();
        }

        _encryptionKey = Convert.FromBase64String(PlayerPrefs.GetString(ENCRYPTION_KEY_PREFIX + "Key"));
        _encryptionIV = Convert.FromBase64String(PlayerPrefs.GetString(ENCRYPTION_KEY_PREFIX + "IV"));
    }

    private void InitializeSaveSystem()
    {
        _saveFilePath = Path.Combine(Application.persistentDataPath, "game_save.bin");
        LoadAllData();
        InitializeDefaults();
    }
    #endregion

    #region Player Preferences
    public int LoadSelectedBall() => PlayerPrefs.GetInt(BALL_INDEX_KEY, 0);
    
    public void SetBallIndex(int index)
    {
        PlayerPrefs.SetInt(BALL_INDEX_KEY, index);
        PlayerPrefs.Save();
    }
    
    public string LoadPlayerName() => PlayerPrefs.GetString(PLAYER_NAME_KEY, "Player");
    
    public void InitializeDefaults()
    {
        if (!PlayerPrefs.HasKey(PLAYER_NAME_KEY))
            PlayerPrefs.SetString(PLAYER_NAME_KEY, "Player");
        if (!PlayerPrefs.HasKey(BALL_INDEX_KEY))
            PlayerPrefs.SetInt(BALL_INDEX_KEY, 0);
        if (!PlayerPrefs.HasKey(VOLUME_KEY))
            PlayerPrefs.SetFloat(VOLUME_KEY, 1.0f);
        PlayerPrefs.Save();
    }
    #endregion

    #region High Score System
    public void AddHighScore(float timeRemaining)
    {
        var newEntry = new HighScoreEntry
        {
            playerName = LoadPlayerName(),
            timeRemaining = timeRemaining,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        _currentSaveData.highScores.Add(newEntry);
        SortAndTrimHighScores();
        SaveAllData();
        DisplayHighScores();
    }

    public List<HighScoreEntry> GetHighScores() => _currentSaveData.highScores;

    private void SortAndTrimHighScores()
    {
        _currentSaveData.highScores = _currentSaveData.highScores
            .OrderByDescending(entry => entry.timeRemaining)
            .Take(MAX_HIGH_SCORES)
            .ToList();
    }

    public void DisplayHighScores()
    {
        if (highScoreContainer == null || highScoreTextPrefab == null) return;

        // Clear existing
        foreach (Transform child in highScoreContainer)
            Destroy(child.gameObject);

        // Display top scores
        for (int i = 0; i < _currentSaveData.highScores.Count; i++)
        {
            var entry = _currentSaveData.highScores[i];
            TMP_Text scoreText = Instantiate(highScoreTextPrefab, highScoreContainer);
            scoreText.text = $"{i+1}. {entry.playerName}: {entry.timeRemaining:F1}s";
        }
    }
    #endregion

    #region Data Management
    private void LoadAllData()
    {
        try
        {
            if (File.Exists(_saveFilePath))
            {
                byte[] fileBytes = File.ReadAllBytes(_saveFilePath);
                string[] fileContent = Encoding.UTF8.GetString(fileBytes).Split(new[] { CHECKSUM_DELIMITER }, StringSplitOptions.None);
                
                if (fileContent.Length == 2 && GenerateChecksum(Convert.FromBase64String(fileContent[0])) == fileContent[1])
                {
                    string decryptedJson = Decrypt(Convert.FromBase64String(fileContent[0]));
                    _currentSaveData = JsonUtility.FromJson<SaveData>(decryptedJson) ?? new SaveData();
                }
                else
                {
                    Debug.LogWarning("Save file integrity check failed");
                    _currentSaveData = new SaveData();
                }
            }
            else
            {
                _currentSaveData = new SaveData();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Load error: {e.Message}");
            _currentSaveData = new SaveData();
        }
    }

    private void SaveAllData()
    {
        try
        {
            string jsonData = JsonUtility.ToJson(_currentSaveData, true);
            string encryptedData = Encrypt(jsonData);
            byte[] encryptedBytes = Convert.FromBase64String(encryptedData);
            string checksum = GenerateChecksum(encryptedBytes);
            
            File.WriteAllBytes(_saveFilePath, 
                Encoding.UTF8.GetBytes(encryptedData + CHECKSUM_DELIMITER + checksum));
        }
        catch (Exception e)
        {
            Debug.LogError($"Save failed: {e.Message}");
        }
    }
    #endregion

    #region Security Methods
    private string GenerateChecksum(byte[] data)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return Convert.ToBase64String(sha256.ComputeHash(data));
        }
    }

    private string Encrypt(string plainText)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = _encryptionKey;
            aes.IV = _encryptionIV;

            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                cs.Write(plainBytes, 0, plainBytes.Length);
                cs.FlushFinalBlock();
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    private string Decrypt(byte[] cipherText)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = _encryptionKey;
            aes.IV = _encryptionIV;

            using (MemoryStream ms = new MemoryStream(cipherText))
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
            using (StreamReader sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }
    #endregion
}