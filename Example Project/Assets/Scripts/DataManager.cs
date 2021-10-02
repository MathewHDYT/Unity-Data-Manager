using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.IO.Compression;
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

        string filePath = Path.Combine(Application.persistentDataPath, FILENAME_FILE);
        // Check if there are already saved files in our system,
        // therefore the file was created last game session.
        if (!CheckFile(filePath, true)) {
            File.Create(filePath).Close();
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
    /// <param name="compression">Wheter the given file should be compressed.</param>
    public void CreateNewFile(string fileName, string content = "", string directoryPath = "", string fileEnding = ".txt", bool encryption = false, bool hashing = false, bool compression = false) {
        // Set the given directory to save into to the persisentDataPath if no different directoryPath was given.
        if (directoryPath == string.Empty) {
            directoryPath = Application.persistentDataPath;
        }

        // Get the filePath from the given values and save it into our FileData.
        string filePath = Path.Combine(directoryPath, fileName + fileEnding);

        // Add dummy data to ensure we set them if the corresponding bools are enabled.
        byte[] fileKey = encryption ? new byte[1] : null;
        string fileHash = compression ? "c" : string.Empty;

        // Check if the file exists already at the given path.
        if (CheckFile(filePath)) {
            return;
        }

        // Check if the file should be encrypted and compressed.
        if (encryption && compression) {
            Debug.LogWarning("File can't be both encrypted and compressed");
            return;
        }

        // Add data of the newly created file to the dictionary.
        FileData fileData = new FileData(filePath, fileHash, fileKey, compression);
        AddToDictionary(fileName, fileData);
        DetectWriteMode(fileData, content, FileMode.CreateNew);
    }

    /// <summary>
    /// Reads all the content from the given file and returns it as plain text.
    /// </summary>
    /// <param name="fileName">Name of the given file that should be read from.</param>
    /// <returns>Wheter the file has been changed outside of the DataManager class or not.</returns>
    public bool TryReadFromFile(string fileName, out string content) {
        content = string.Empty;
        bool sameHash = false;

        FileData fileData = GetFileData(fileName);
        if (fileData == null) {
            return sameHash;
        }

        // Check if hashing is enabled and we therefore saved the latest hash in fileData.
        if (!string.IsNullOrEmpty(fileData.FileHash)) {
            sameHash = CompareFileHash(fileData);
        }
        else {
            sameHash = true;
        }

        // Check if encryption is enabled and we therefore saved a key in fileData.
        if (!IsByteArrayNullOrEmpty(fileData.FileKey)) {
            content = ReadFromEncryptedFile(fileData);
        }
        else if (fileData.FileCompression) {
            content = ReadFromCompressedFile(fileData);
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

        FileData fileData = GetFileData(fileName);
        if (fileData == null) {
            return success;
        }

        // Check if the given path exists.
        if (!Directory.Exists(directory)) {
            Debug.LogWarning("Given path: " + directory + " does not exist");
            return success;
        }

        string name = Path.GetFileName(fileData.FilePath);
        string filePath = Path.Combine(directory, name);

        // Check if the file exists already at the given path.
        if (CheckFile(filePath)) {
            return success;
        }

        // Move the file to its new location and adjust the FilePath to the new value.
        File.Move(fileData.FilePath, filePath);
        fileData.FilePath = filePath;
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

        FileData fileData = GetFileData(fileName);
        if (fileData == null) {
            return success;
        }

        DetectWriteMode(fileData, content, FileMode.Create);
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

        FileData fileData = GetFileData(fileName);
        if (fileData == null) {
            return success;
        }

        // Check if hashing is enabled and we therefore saved the latest hash in fileData,
        // and check if the hash is still the same or if it was changed.
        if (!string.IsNullOrEmpty(fileData.FileHash) && !CompareFileHash(fileData)) {
            // If it was changed append our given content to the file.
            return success;
        }

        DetectWriteMode(fileData, content, FileMode.Append);
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

        FileData fileData = GetFileData(fileName);
        if (fileData == null) {
            return sameHash;
        }

        return CompareFileHash(fileData);
    }

    /// <summary>
    /// Deletes the given file and all its remote counterparts.
    /// </summary>
    /// <param name="fileName">Name of the given file that should be deleted.</param>
    /// <returns>Wheter deleting was succesful or not.</returns>
    public bool DeleteFile(string fileName) {
        bool success = false;
        
        FileData fileData = GetFileData(fileName);
        if (fileData == null) {
            return success;
        }
        // Check if the file exists at the given path.
        else if (!CheckFile(fileData.FilePath, true)) {
            return success;
        }

        File.Delete(fileData.FilePath);
        fileData.DeleteRemote();
        RemoveFromDictionary(fileName);

        success = true;
        return success;
    }

    /// <summary>
    /// Attempts to get the corresponding value for the given key from the file dictionary.
    /// </summary>
    /// <param name="fileName">Name of the given file that we want to get the values from.</param>
    /// <returns>The data of the given file.</returns>
    private FileData GetFileData(string fileName) {
        // Get fileData from the dictionary and return an empty string and a warning if it wasn't created yet.
        if (fileDictionary.TryGetValue(fileName, out FileData fileData)) {
            return fileData;
        }

        Debug.LogWarning("File has not been created yet with the given name: " + fileName);
        return null;
    }

    private void DetectWriteMode(FileData fileData, string content, FileMode fileMode) {
        // Check if the file should be only encrypted.
        if (!IsByteArrayNullOrEmpty(fileData.FileKey)) {
            if (fileMode == FileMode.Append) {
                // We have to read the whole current content of the file,
                // append our new content and rewrite it all,
                // this has to be done because we can't just simply append to a encrypted file,
                // as the key is generated based on the content and if we create the key
                // only for the new content it will not be equal to the actual key needed,
                // so reading the file won't succed and only return encrypted text.
                string currentContent = ReadFromEncryptedFile(fileData);
                content = currentContent + content;
            }
            fileData.FileKey = WriteToEncryptedFile(fileData.FilePath, content, fileMode);
        }
        // Check if the file should be only compressed.
        else if (fileData.FileCompression) {
            WriteToCompressedFile(fileData.FilePath, content, fileMode);
        }
        else {
            WriteToFile(fileData.FilePath, content, fileMode);
        }

        // Check if the file should be hashed.
        if (!string.IsNullOrEmpty(fileData.FileHash)) {
            fileData.FileHash = GetFileHash(fileData.FilePath);
        }
    }

    /// <summary>
    /// Checks if a given file exists and prints an error if the expected result is not equal to the actual result.
    /// </summary>
    /// <param name="fileName">Name of the given file that we want to get the values from.</param>
    /// <param name="fileExists">
    /// Defines wheter we expect the file to exist or not to exists,
    /// this influences when and what message will be printed out as a warning.
    /// </param>
    /// <returns>Wheter the file exists or not.</returns>
    private bool CheckFile(string filePath, bool fileExists = false) {
        bool result = TryGetFileState(filePath, fileExists, out string message);
        if (message != string.Empty) {
            Debug.LogWarning(message);
        }
        return result;
    }

    /// <summary>
    /// Attempts to get the current file state (exists or doesn't exist) and returns a message
    /// depeding on the expected and the actual file state.
    /// </summary>
    /// <param name="filePath">Name of the given file that we want to get the values from.</param>
    /// <param name="expected">
    /// Defines wheter we expect the file to exist or not to exists,
    /// this influences when and what message will be printed out as a warning.
    /// </param>
    /// <param name="message">Message we can print out.</param>
    /// <returns>Wheter the file exists or not.</returns>
    private bool TryGetFileState(string filePath, bool expected, out string message) {
        bool actual = File.Exists(filePath);

        // Don't log a warning when we achieved our expected FileState.
        if (expected == actual) {
            message = string.Empty;
            return actual;
        }

        message = GetMessage(filePath, actual);
        return actual;
    }

    /// <summary>
    /// Gets one of the two possible messages that we want to show,
    /// when the file state is not as expected when calling a fucntion.
    /// </summary>
    /// <param name="filePath">Name of the given file that we want to get the values from.</param>
    /// <param name="fileExists">
    /// Defines wheter we expect the file to exist or not to exists,
    /// this influences when and what message will be printed out as a warning.
    /// </param>
    /// <returns>Message we can print out.</returns>
    private string GetMessage(string filePath, bool fileExists) {
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string directory = Path.GetDirectoryName(filePath);
        string message = string.Empty;

        if (fileExists) {
            message = "There already exists a file with the name: " + fileName + " at the given folder: " + directory;
        }
        else {
            message = "There doesn't exist a file with the name: " + fileName + " at the given folder: " + directory;
        }

        return message;
    }

    /// <summary>
    /// Checks if the given byte array is null or if its length is 0 or less.
    /// </summary>
    /// <param name="arr">Array we want to check.</param>
    /// <returns>Wheter the given byte array is null or has no content.</returns>
    private bool IsByteArrayNullOrEmpty(byte[] arr) {
        return (arr == null || arr.Length <= 0);
    }

    /// <summary>
    /// Removes the item with the given key from the file dictionary.
    /// </summary>
    /// <param name="fileName">Key of the item we want to remove.</param>
    private void RemoveFromDictionary(string fileName) {
        // Remove the data from the dictionary.
        fileDictionary.Remove(fileName);
        UpdateFileNames();
    }

    /// <summary>
    /// Adds the given content to the file dictionary.
    /// </summary>
    /// <param name="fileName">Key of the content we want to add.</param>
    /// <param name="fileData">Value of the content we want to add.</param>
    private void AddToDictionary(string fileName, FileData fileData) {
        // Add the data to the dictionary.
        fileDictionary.Add(fileName, fileData);
        UpdateFileNames();
    }

    /// <summary>
    /// Updates the list of file names to ensure we keep our keys in the dictionary,
    /// even if we restart the game so that the file dictionary can be rebuild at the start.
    /// This is done because the same key as in the dictionary is also used to access the values
    /// saved in our PlayerPrefs that hold the FileData.
    /// </summary>
    private void UpdateFileNames() {
        string filePath = Path.Combine(Application.persistentDataPath, FILENAME_FILE);
        // Write all dictionary keys into the file.
        File.WriteAllLines(filePath, fileDictionary.Keys);
    }

    /// <summary>
    /// Loads the list of file names saved into the file. To rebuild the dictioanry with all values
    /// and the corresponding keys. So that we can still access the files even after restarting the game.
    /// </summary>
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

    /// <summary>
    /// Compares the current hash with the hash last produced from the internal system.
    /// To ensure it hasn't been changed since we last accesed it ourselves.
    /// </summary>
    /// <param name="fileData">File we want to check the hash from.</param>
    /// <returns>Wheter the expected and actual file hash are the same.</returns>
    private bool CompareFileHash(FileData fileData) {
        string currentHash = GetFileHash(fileData.FilePath);
        return string.Equals(currentHash, fileData.FileHash);
    }

    /// <summary>
    /// Writes the given content to the given file and encrypts it as well.
    /// </summary>
    /// <param name="filePath">File we want to write the content into.</param>
    /// <param name="content">Content we want to write into the given file.</param>
    /// <param name="fileMode">Mode the file should be accesed as.</param>
    /// <returns>Byte array that contains the key to access the file for future reads and writes.</returns>
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
            using (var iStream = new CryptoStream(fileStream, aes.CreateEncryptor(fileKey, input), CryptoStreamMode.Write))
            // Create a StreamReader, wrapping CryptoStream.
            using (var streamWriter = new StreamWriter(iStream))
            // Write to the innermost stream (which will encrypt).
            streamWriter.Write(content);
        }

        return fileKey;
    }

    /// <summary>
    /// Reads everything from the given encrypted file.
    /// </summary>
    /// <param name="fileData">File we want to read the content from.</param>
    /// <returns>Decrypted content that was contained in the file.</returns>
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
            using (var oStream = new CryptoStream(fileStream, aes.CreateDecryptor(fileData.FileKey, output), CryptoStreamMode.Read))
            // Create a StreamReader, wrapping CryptoStream.
            using (var streamReader = new StreamReader(oStream))
            // Read the entire file into a string value.
            content = streamReader.ReadToEnd();
        }

        return content;
    }

    /// <summary>
    /// Writes the given content to the given file.
    /// </summary>
    /// <param name="filePath">File we want to write the content into.</param>
    /// <param name="content">Content we want to write into the given file.</param>
    /// <param name="fileMode">Mode the file should be accesed as.</param>
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

    /// <summary>
    /// Writes the given content to the given file and compressed it as well.
    /// </summary>
    /// <param name="filePath">File we want to write the content into.</param>
    /// <param name="content">Content we want to write into the given file.</param>
    /// <param name="fileMode">Mode the file should be accesed as.</param>
    private void WriteToCompressedFile(string filePath, string content, FileMode fileMode) {
        // Create FileStream for opening and reading from the FileInfo object.
        using (var compressedFileStream = new FileStream(filePath, fileMode)) {
            // Check if we even need to write something to the currently open FileStream.
            if (content == string.Empty) {
                return;
            }

            // Create a FileStream for creating files.
            using (var compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
            // Create a StreamReader, wrapping CryptoStream.
            using (var streamWriter = new StreamWriter(compressionStream))
            // Write to the innermost stream (which will encrypt).
            streamWriter.Write(content);
        }

        return;
    }

    /// <summary>
    /// Reads everything from the given compressed file.
    /// </summary>
    /// <param name="fileData">File we want to read the content from.</param>
    /// <returns>Decompressed content that was contained in the file.</returns>
    private string ReadFromCompressedFile(FileData fileData) {
        string content = string.Empty;

        // Create FileStream for opening files.
        using (var decompressedFileStream = new FileStream(fileData.FilePath, FileMode.Open))
        // Create GZipStream, wrapping the CryptoStream.
        using (var decompressionStream = new GZipStream(decompressedFileStream, CompressionMode.Decompress))
        // Create a StreamReader, wrapping GZipStream.
        using (var streamReader = new StreamReader(decompressionStream))
        // Read the entire file into a string value.
        content = streamReader.ReadToEnd();

        return content;
    }

    /// <summary>
    /// Gets the file hash of a given files contents.
    /// </summary>
    /// <param name="filePath">File we want to create the hash for.</param>
    /// <returns>Hash representing the given files current content.</returns>
    private string GetFileHash(string filePath) {
        byte[] buffer = File.ReadAllBytes(filePath);
        using (var sha1 = new SHA1CryptoServiceProvider())
        return string.Concat(sha1.ComputeHash(buffer).Select(x => x.ToString("X2")));
    }
}
