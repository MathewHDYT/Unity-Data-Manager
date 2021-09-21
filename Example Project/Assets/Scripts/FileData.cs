using UnityEngine;
using System.IO;

[System.Serializable]
public class FileData {

    // Private members reflecting the value in the remote PlayerPrefs,
    // used to ensure we don't need to load the data from PlayerPrefs
    // each time we want to access the given file data.
    // Theese values should be private as we don't want to acces them directly,
    // they need to be public tough so that JsonUtility.ToJson
    // can add their values into the json string we create or update.
    public string path = string.Empty;
    public string hash = string.Empty;
    public byte[] key = null;

    /// <summary>
    /// Full path of the file on our local machine.
    /// </summary>
    public string FilePath {
        get {
            return path;
        }
        set {
            path = value;
            UpdateRemote();
        }
    }

    /// <summary>
    /// Hash of the file, used to ensure the file doesn't get changed
    /// from outside the DataManager class, if hashing is enabled.
    /// </summary>
    public string FileHash {
        get {
            return hash;
        }
        set {
            hash = value;
            UpdateRemote();
        }
    }

    /// <summary>
    /// Key of the file, used as the en -and decryption key
    /// if encryption is enabled.
    /// </summary>
    public byte[] FileKey {
        get {
            return key;
        }
        set {
            if (!value.IsNullOrEmpty()) {
                return;
            }

            key = value;
            UpdateRemote();
        }
    }

    /// <summary>
    /// Creates a new FileData instance with the given file data.
    /// </summary>
    /// <param name="filePath">Full path of the file.</param>
    /// <param name="fileHash">Hash of the file.</param>
    /// <param name="fileKey">Key of the file, used for en -or decryption.</param>
    public FileData(string filePath, string fileHash, byte[] fileKey) {
        FilePath = filePath;
        FileHash = fileHash;
        FileKey = fileKey;
    }

    private void UpdateRemote() {
        // Get the key for the PlayerPrefs which is our fileName without extension.
        string fileName = Path.GetFileNameWithoutExtension(FilePath);
        // Transform the fileData object into a json string.
        string json = JsonUtility.ToJson(this);
        // Save the json string into playerprefs
        // to keep the data persistent between different game session.
        PlayerPrefs.SetString(fileName, json);
    }

    private void DeleteRemote() {
        // Get the key for the PlayerPrefs which is our fileName without extension.
        string fileName = Path.GetFileNameWithoutExtension(FilePath);
        // Delete the PlayerPrefs with the given key.
        PlayerPrefs.DeleteKey(fileName);
    }
}
