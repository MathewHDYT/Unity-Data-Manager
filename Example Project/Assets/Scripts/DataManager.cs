using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

public class DataManager : MonoBehaviour {

    #region Singelton
    public static DataManager instance;

    private void Awake() {
        // Check if instance is already defined and if this gameObject is not the current instance.
        if (instance != null) {
            Debug.LogWarning("Multiple Instances of DataManager found. Current instance was destroyed.");
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        string fullPath = Path.Combine(Application.persistentDataPath, FILENAME_FILE);
        // Check if there are already saved files in our system,
        // therefore the file was created last game session.
        if (!File.Exists(fullPath)) {
            File.Create(fullPath).Close();
        }
        else {
            // If it is we can just load all it's contents
            // and save them into our dictionary.
            LoadFileNames();
        }
    }
    #endregion

    private const string FILENAME_FILE = "fileNames.save";
    private Dictionary<string, FileData> fileDictionary = new Dictionary<string, FileData>();

    /// <summary>
    /// Creates a new file and writes the given text to it.
    /// </summary>
    /// <param name="fileName">Name the given file should have (is used as the ID so make sure it's unique).</param>
    /// <param name="content">Inital data that should be saved into the file.</param>
    /// <param name="directoryPath">Directory the file shoukd be saved into.</param>
    /// <param name="fileEnding">Ending the given file should have.</param>
    /// <param name="encryption">Wether the given file should be encrypted.</param>
    /// <param name="hashing">Wheter the given file should be checked for unexpected changes before using it.</param>
    public void CreateNewFile(string fileName, string content = "", string directoryPath = "", string fileEnding = ".txt", bool encryption = false, bool hashing = false) {
        // Set the given directory to save into to the persisentDataPath if no different directoryPath was given.
        if (directoryPath == string.Empty) {
            directoryPath = Application.persistentDataPath;
        }

        // Get the filePath from the given values and save it into our FileData.
        string filePath = Path.Combine(directoryPath, fileName + fileEnding);
        byte[] fileKey = null;
        string fileHash = string.Empty;

        // Check if the file exists already at the given path.
        if (File.Exists(filePath)) {
            Debug.LogWarning("There already exists a file at the given path: " + filePath);
            return;
        }

        // Check if the file should be encrypted.
        if (encryption) {
            fileKey = WriteToEncryptedFile(filePath, content, FileMode.CreateNew);
        }
        else {
            WriteToFile(filePath, content, FileMode.CreateNew);
        }

        // Check if the file should be hashed.
        if (hashing) {
            fileHash = GetFileHash(filePath);
        }

        // Add data of the newly created file to the dictionary.
        FileData fileData = new FileData(filePath, fileHash, fileKey);
        AddToDictionary(fileName, fileData);
    }

    /// <summary>
    /// Reads all the content from the given file and returns it as plain text.
    /// </summary>
    /// <param name="fileName">Name of the given file that should be read from.</param>
    /// <returns>Wheter the file has been changed outside of the DataManager class or not.</returns>
    public bool TryReadFromFile(string fileName, out string content) {
        content = string.Empty;
        bool sameHash = false;

        // Get fileData from the dictionary and return an empty string and a warning if it wasn't created yet.
        bool result = fileDictionary.TryGetValue(fileName, out FileData fileData);
        if (!result) {
            Debug.LogWarning("File has not been created yet with the given name: " + fileName);
            return sameHash;
        }

        // Check if hashing is enabled and we therefore saved the latest hash in fileData.
        if (fileData.FileHash != string.Empty) {
            sameHash = CompareFileHash(fileData);
        }

        // Check if encryption is enabled and we therefore saved a key in fileData.
        if (fileData.FileKey != null && fileData.FileKey.Length > 0) {
            content = ReadFromEncryptedFile(fileData);
        }
        else {
            content = File.ReadAllText(fileData.FilePath);
        }

        return sameHash;
    }

    /// <summary>
    /// Moves the given file to another directory.
    /// </summary>
    /// <param name="fileName">Name of the given file that should be moved.</param>
    /// <param name="directory">New directory the given file should be moved too.</param>
    /// <returns>Wheter changing the file path was succesfull or not.</returns>
    public bool ChangeFilePath(string fileName, string directory) {
        bool success = false;

        // Get fileData from the dictionary and return an empty string and a warning if it wasn't created yet.
        bool result = fileDictionary.TryGetValue(fileName, out FileData fileData);
        if (!result) {
            Debug.LogWarning("File has not been created yet with the given name: " + fileName);
            return success;
        }

        // Check if the given path exists.
        if (!Directory.Exists(directory)) {
            Debug.LogWarning("Given path: " + directory + " does not exist");
            return success;
        }

        string name = Path.GetFileName(fileData.FilePath);
        string fullPath = Path.Combine(directory, name);

        // Check if the file exists already at the given path.
        if (File.Exists(fullPath)) {
            Debug.LogWarning("There already exists a file with the name: " + name + " at the given path: " + directory);
            return success;
        }

        // Move the file to its new location and adjust the FilePath to the new value.
        File.Move(fileData.FilePath, fullPath);
        fileData.FilePath = fullPath;
        success = true;
        return success;
    }

    /// <summary>
    /// Updates content of the given file.
    /// </summary>
    /// <param name="fileName">Name of the given file that should have its content replaced.</param>
    /// <param name="content">Data that should be saved into the file.</param>
    /// <returns>Wheter updating the content of the file was succesfull or not.</returns>
    public bool UpdateFileContent(string fileName, string content) {
        bool success = false;

        // Get fileData from the dictionary and return an empty string and a warning if it wasn't created yet.
        bool result = fileDictionary.TryGetValue(fileName, out FileData fileData);
        if (!result) {
            Debug.LogWarning("File has not been created yet with the given name: " + fileName);
            return success;
        }

        // Check if encryption is enabled and we therefore saved a key in fileData.
        if (fileData.FileKey != null && fileData.FileKey.Length > 0) {
            byte[] fileKey = WriteToEncryptedFile(fileData.FilePath, content, FileMode.Create);
            fileData.FileKey = fileKey;
        }
        else {
            WriteToFile(fileData.FilePath, content, FileMode.Create);
        }

        // Check if hashing is enabled and we therefore saved the latest hash in fileData.
        if (fileData.FileHash != string.Empty) {
            fileData.FileHash = GetFileHash(fileData.FilePath);
        }

        success = true;
        return success;
    }

    /// <summary>
    /// Appends content to the given file.
    /// </summary>
    /// <param name="fileName">Name of the given file that should have the content appended.</param>
    /// <param name="content">Data that should be appended to the file.</param>
    /// <returns>Wheter appending the content to the file was succesfull or not.</returns>
    public bool AppendFileContent(string fileName, string content) {
        bool success = false;

        // Get fileData from the dictionary and return an empty string and a warning if it wasn't created yet.
        bool result = fileDictionary.TryGetValue(fileName, out FileData fileData);
        if (!result) {
            Debug.LogWarning("File has not been created yet with the given name: " + fileName);
            return success;
        }

        // Check if encryption is enabled and we therefore saved a key in fileData.
        if (fileData.FileKey != null && fileData.FileKey.Length > 0) {
            byte[] fileKey = WriteToEncryptedFile(fileData.FilePath, content, FileMode.Append);
            fileData.FileKey = fileKey;
        }
        else {
            WriteToFile(fileData.FilePath, content, FileMode.Append);
        }

        // Check if hashing is enabled and we therefore saved the latest hash in fileData.
        if (fileData.FileHash != string.Empty) {
            fileData.FileHash = GetFileHash(fileData.FilePath);
        }

        success = true;
        return success;
    }

    /// <summary>
    /// Compares hash of the given file with the last hash it had to ensure it's the same.
    /// </summary>
    /// <param name="fileName">Name of the given file that should have its hash checked.</param>
    /// <returns>Wheter the file hash is still the same as expected or if it has changed.</returns>
    public bool CheckFileHash(string fileName) {
        bool sameHash = false;

        // Get fileData from the dictionary and return an empty string and a warning if it wasn't created yet.
        bool result = fileDictionary.TryGetValue(fileName, out FileData fileData);
        if (!result) {
            Debug.LogWarning("File has not been created yet with the given name: " + fileName);
            return sameHash;
        }

        return CompareFileHash(fileData);
    }

    private void AddToDictionary(string fileName, FileData fileData) {
        // Add the data to the dictionary.
        fileDictionary.Add(fileName, fileData);

        string fullPath = Path.Combine(Application.persistentDataPath, FILENAME_FILE);
        // Write all dictionary keys into the file.
        File.WriteAllLines(fullPath, fileDictionary.Keys);
    }

    private void LoadFileNames() {
        string fullPath = Path.Combine(Application.persistentDataPath, FILENAME_FILE);
        // Read all dictionary keys from the file.
        string[] dictKeys = File.ReadAllLines(fullPath);

        // Get the serialized FileData for each saved dictionary key
        // and load it into a FileData object to then save it into our dictionary.
        foreach (string key in dictKeys) {
            string json = PlayerPrefs.GetString(key);
            FileData fileData = JsonUtility.FromJson<FileData>(json);
            fileDictionary.Add(key, fileData);
        }
    }

    private bool CompareFileHash(FileData fileData) {
        string currentHash = GetFileHash(fileData.FilePath);
        return string.Equals(currentHash, fileData.FileHash);
    }

    private byte[] WriteToEncryptedFile(string filePath, string content, FileMode fileMode) {
        byte[] fileKey = null;

        // Create a FileStream for creating files.
        using (var fileStream = new FileStream(filePath, fileMode)) {
            // Create new AES instance.
            Aes aes = Aes.Create();
            // Update the internal key.
            fileKey = aes.Key;
            // Save the new generated IV.
            byte[] input = aes.IV;
            // Write the IV to the FileStream unencrypted.
            fileStream.Write(input, 0, input.Length);

            // Check if we even need to write something to the currently open FileStream.
            if (content == string.Empty) {
                return fileKey;
            }

            // Create CryptoStream, wrapping FileStream.
            using (var iStream = new CryptoStream(fileStream, aes.CreateEncryptor(fileKey, input), CryptoStreamMode.Write)) {
                // Create a StreamReader, wrapping CryptoStream.
                using (var streamWriter = new StreamWriter(iStream)) {
                    // Write to the innermost stream (which will encrypt).
                    streamWriter.Write(content);
                }
            }
        }

        return fileKey;
    }

    private string ReadFromEncryptedFile(FileData fileData) {
        string content = string.Empty;

        // Create FileStream for opening files.
        using (var fileStream = new FileStream(fileData.FilePath, FileMode.Open)) {
            // Create new AES instance.
            Aes aes = Aes.Create();
            // Create an array of correct size based on AES IV.
            byte[] output = new byte[aes.IV.Length];
            // Read the IV from the file.
            fileStream.Read(output, 0, output.Length);
            // Create CryptoStream, wrapping FileStream.
            using (var oStream = new CryptoStream(fileStream, aes.CreateDecryptor(fileData.FileKey, output), CryptoStreamMode.Read)) {
                // Create a StreamReader, wrapping CryptoStream.
                using (var streamReader = new StreamReader(oStream)) {
                    // Read the entire file into a string value.
                    content = streamReader.ReadToEnd();
                }
            }
        }

        return content;
    }

    private void WriteToFile(string filePath, string content, FileMode fileMode) {
        switch (fileMode) {
            case FileMode.Append:
                File.AppendAllText(filePath, content);
                break;
            case FileMode.Create:
                break;
            case FileMode.CreateNew:
                File.Create(filePath).Close();
                File.WriteAllText(filePath, content);
                break;
            default:
                // Nothing to do.
                break;
        }
    }

    private string GetFileHash(string filePath) {
        byte[] buffer = File.ReadAllBytes(filePath);
        using (var sha1 = new SHA1CryptoServiceProvider()) {
            return string.Concat(sha1.ComputeHash(buffer).Select(x => x.ToString("X2")));
        }
    }
}
