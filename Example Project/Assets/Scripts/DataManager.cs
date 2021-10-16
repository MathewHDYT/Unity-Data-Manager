using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.IO.Compression;
using UnityEngine;
using System;

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
        var err = CheckFileNotExists(filePath);
        if (err == DataError.FILE_DOES_NOT_EXIST) {
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

    public enum DataError {
        OK,
        INVALID_ARGUMENT,
        INVALID_PATH,
        NOT_REGISTERED,
        FILE_CORRUPTED,
        FILE_ALREADY_EXISTS,
        FILE_DOES_NOT_EXIST
    }

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
    public DataError CreateNewFile(string fileName, string content = "", string directoryPath = "", string fileEnding = ".txt", bool encryption = false, bool hashing = false, bool compression = false) {
        // Set the given directory to save into to the persisentDataPath if no different directoryPath was given.
        if (directoryPath == string.Empty) {
            directoryPath = Application.persistentDataPath;
        }

        // Get the filePath from the given values and save it into our FileData.
        string filePath = Path.Combine(directoryPath, fileName + fileEnding);

        // Add dummy data to ensure we set them if the corresponding bools are enabled.
        byte[] fileKey = encryption ? new byte[1] : null;
        string fileHash = hashing ? "h" : string.Empty;

        // Check if the file exists already at the given path.
        var err = CheckFileExists(filePath);
        if (err != DataError.OK) {
            return err;
        }

        // Check if the file should be encrypted and compressed.
        if (encryption && compression) {
            Debug.LogWarning("File can't be both encrypted and compressed");
            return DataError.INVALID_ARGUMENT;
        }

        // Add data of the newly created file to the dictionary.
        FileData fileData = new FileData(filePath, fileHash, fileKey, compression);
        AddToDictionary(fileName, fileData);
        DetectWriteMode(fileData, content, FileMode.CreateNew);
        return DataError.OK;
    }

    /// <summary>
    /// Reads all the content from the given file and returns it as plain text.
    /// </summary>
    /// <param name="fileName">Name of the given file that should be read from.</param>
    /// <returns>Wheter the file has been changed outside of the DataManager class or not.</returns>
    public DataError TryReadFromFile(string fileName, out string content) {
        content = string.Empty;

        ValueDataError valueDataError = GetFileData(fileName);
        if (valueDataError.Error != DataError.OK) {
            return DataError.NOT_REGISTERED;
        }

        // Check if hashing is enabled and we therefore saved the latest hash in fileData.
        if (!string.IsNullOrEmpty(valueDataError.Value.FileHash) && CompareFileHash(valueDataError.Value) != DataError.OK) {
            return DataError.FILE_CORRUPTED;
        }

        // Check if encryption is enabled and we therefore saved a key in fileData.
        if (!IsByteArrayNullOrEmpty(valueDataError.Value.FileKey)) {
            content = ReadFromEncryptedFile(valueDataError.Value);
        }
        else if (valueDataError.Value.FileCompression) {
            content = ReadFromCompressedFile(valueDataError.Value);
        }
        else {
            content = File.ReadAllText(valueDataError.Value.FilePath);
        }

        return DataError.OK;
    }

    /// <summary>
    /// Moves the given file to another directory.
    /// </summary>
    /// <param name="fileName">Name of the given file that should be moved.</param>
    /// <param name="directory">New directory the given file should be moved too.</param>
    /// <returns>Wheter changing the file path was succesfull or not.</returns>
    public DataError ChangeFilePath(string fileName, string directory) {
        ValueDataError valueDataError = GetFileData(fileName);
        if (valueDataError.Error != DataError.OK) {
            return DataError.NOT_REGISTERED;
        }

        // Check if the given path exists.
        if (!Directory.Exists(directory)) {
            return DataError.INVALID_PATH;
        }

        string name = Path.GetFileName(valueDataError.Value.FilePath);
        string filePath = Path.Combine(directory, name);

        // Check if the file exists already at the given path.
        var err = CheckFileExists(filePath);
        if (err != DataError.OK) {
            return err;
        }

        // Move the file to its new location and adjust the FilePath to the new value.
        File.Move(valueDataError.Value.FilePath, filePath);
        valueDataError.Value.FilePath = filePath;
        return DataError.OK;
    }

    /// <summary>
    /// Updates content of the given file.
    /// </summary>
    /// <param name="fileName">Name of the given file that should have its content replaced.</param>
    /// <param name="content">Data that should be saved into the file.</param>
    /// <returns>Wheter updating the content of the file was succesfull or not.</returns>
    public DataError UpdateFileContent(string fileName, string content) {
        ValueDataError valueDataError = GetFileData(fileName);
        if (valueDataError.Error != DataError.OK) {
            return DataError.NOT_REGISTERED;
        }

        DetectWriteMode(valueDataError.Value, content, FileMode.Create);
        return DataError.OK;
    }

    /// <summary>
    /// Appends content to the given file.
    /// </summary>
    /// <param name="fileName">Name of the given file that should have the content appended.</param>
    /// <param name="content">Data that should be appended to the file.</param>
    /// <returns>Wheter appending the content to the file was succesfull or not.</returns>
    public DataError AppendFileContent(string fileName, string content) {
        ValueDataError valueDataError = GetFileData(fileName);
        if (valueDataError.Error != DataError.OK) {
            return DataError.NOT_REGISTERED;
        }

        // Check if hashing is enabled and we therefore saved the latest hash in fileData,
        // and check if the hash is still the same or if it was changed.
        if (!string.IsNullOrEmpty(valueDataError.Value.FileHash) && CompareFileHash(valueDataError.Value) != DataError.OK) {
            // If it was changed append our given content to the file.
            return DataError.FILE_CORRUPTED;
        }

        DetectWriteMode(valueDataError.Value, content, FileMode.Append);
        return DataError.OK;
    }

    /// <summary>
    /// Compares hash of the given file with the last hash it had to ensure it's the same.
    /// </summary>
    /// <param name="fileName">Name of the given file that should have its hash checked.</param>
    /// <returns>Wheter the file hash is still the same as expected or if it has changed.</returns>
    public DataError CheckFileHash(string fileName) {
        ValueDataError valueDataError = GetFileData(fileName);
        if (valueDataError.Error != DataError.OK) {
            return DataError.NOT_REGISTERED;
        }

        return CompareFileHash(valueDataError.Value);
    }

    /// <summary>
    /// Deletes the given file and all its remote counterparts.
    /// </summary>
    /// <param name="fileName">Name of the given file that should be deleted.</param>
    /// <returns>Wheter deleting was succesful or not.</returns>
    public DataError DeleteFile(string fileName) {
        ValueDataError valueDataError = GetFileData(fileName);
        if (valueDataError.Error != DataError.OK) {
            return DataError.NOT_REGISTERED;
        }
        // Check if the file exists at the given path.
        var err = CheckFileNotExists(valueDataError.Value.FilePath);
        if (err != DataError.OK) {
            return err;
        }

        File.Delete(valueDataError.Value.FilePath);
        valueDataError.Value.DeleteRemote();
        RemoveFromDictionary(fileName);
        return DataError.OK;
    }

    /// <summary>
    /// Attempts to get the corresponding value for the given key from the file dictionary.
    /// </summary>
    /// <param name="fileName">Name of the given file that we want to get the values from.</param>
    /// <returns>The data of the given file.</returns>
    private ValueDataError GetFileData(string fileName) {
        var valueDataError = new ValueDataError(null, DataError.OK);
        // Get fileData from the dictionary and return null and a warning if it wasn't created yet.
        if (fileDictionary.TryGetValue(fileName, out FileData fileData)) {
            valueDataError.Error = DataError.NOT_REGISTERED;
            return valueDataError;
        }

        valueDataError.Value = fileData;
        Debug.LogWarning("File has not been created yet with the given name: " + fileName);
        return valueDataError;
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
    /// Checks if a given file exists and prints an error if it does not.
    /// </summary>
    /// <param name="fileName">Name of the given file that we want to get the values from.</param>
    /// <returns>Wheter the file exists or not.</returns>
    private DataError CheckFileNotExists(string filePath) {
        bool exists = File.Exists(filePath);
        
        if (exists) {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string directory = Path.GetDirectoryName(filePath);
            Debug.LogWarning("There already exists a file with the name: " + fileName + " at the given folder: " + directory);
            return DataError.FILE_ALREADY_EXISTS;
        }
        
        return DataError.OK;
    }
    
    /// <summary>
    /// Checks if a given file exists and prints an error if it does.
    /// </summary>
    /// <param name="fileName">Name of the given file that we want to get the values from.</param>
    /// <returns>Wheter the file exists or not.</returns>
    private DataError CheckFileExists(string filePath) {
        bool exists = File.Exists(filePath);
        
        if (!exists) {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string directory = Path.GetDirectoryName(filePath);
            Debug.LogWarning("There doesn't exist a file with the name: " + fileName + " at the given folder: " + directory);
            return DataError.FILE_DOES_NOT_EXIST;
        }

        return DataError.OK;
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
    private DataError CompareFileHash(FileData fileData) {
        string currentHash = GetFileHash(fileData.FilePath);
        if (!string.Equals(currentHash, fileData.FileHash)) {
            return DataError.FILE_CORRUPTED;
        }
        return DataError.OK;
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
