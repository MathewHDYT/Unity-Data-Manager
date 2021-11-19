![Unity Data Manager](https://github.com/MathewHDYT/Unity-Data-Manager-UDM/blob/main/logo.png/)

[![MIT license](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://lbesson.mit-license.org/)
[![Unity](https://img.shields.io/badge/Unity-5.3%2B-green.svg?style=flat-square)](https://docs.unity3d.com/530/)
[![GitHub release](https://img.shields.io/github/release/MathewHDYT/Unity-Data-Manager-UDM/all.svg?style=flat-square)](https://github.com/MathewHDYT/Unity-Data-Manager-UDM/releases/)
[![GitHub downloads](https://img.shields.io/github/downloads/MathewHDYT/Unity-Data-Manager-UDM/all.svg?style=flat-square)](https://github.com/MathewHDYT/Unity-Data-Manager-UDM/releases/)

# Unity Data Manager (UDM)
Used to create, manage and load data via. files on the system easily and permanently over multiple game sessions. 

## Contents
- [Unity Data Manager (UDM)](#unity-data-manager-udm)
  - [Contents](#contents)
  - [Introduction](#introduction)
  - [Installation](#installation)
- [Documentation](#documentation)
  - [Reference to Data Manager Script](#reference-to-data-manager-script)
  - [Possible Errors](#possible-errors)
  - [Public accesible methods](#public-accesible-methods)
  	- [Create New File method](#create-new-file-method)
  	- [Try Read From File method](#try-read-from-file-method)
  	- [Change File Path method](#change-file-path-method)
  	- [Update File Content method](#update-file-content-method)
  	- [Append File Content method](#append-file-content-method)
  	- [Check File Hash method](#check-file-hash-method)
  	- [Delete File method](#delete-file-method)

## Introduction
A lot of games need to save data between multiple game runs, in a small scope this can be done with Unitys [```PlayerPrefs```](https://docs.unity3d.com/2021.2/Documentation/ScriptReference/PlayerPrefs.html) system, if the scope rises tough this small and easily integrated Data Manager can help you create, manage and load data via. files on the system easily and permanently over multiple game sessions.

**Unity Data Manager implements the following methods consisting of a way to:**
- Create and register a new file with the DataManager and the given settings at the given location (see [Create New File method](#create-new-file-method))
- Read the content of a registered file (see [Try Read From File method](#try-read-from-file-method))
- Change the file path of a registered file (see [Change File Path method](#change-file-path-method))
- Change all the content inside of a registered file (see [Update File Content method](#update-file-content-method))
- Append content to a registered file (see [Append File Content method](#append-file-content-method))
- Check the file hash of a registered file (see [Check File Hash method](#check-file-hash-method))
- Delete a registered file and unregister it (see [Delete File method](#delete-file-method))

For each method there is a description on how to call it and how to use it correctly for your game in the given section.

## Installation
**Required Software:**
- [Unity](https://unity3d.com/get-unity/download) Ver. 2020.3.17f1

The Data Manager itself is version independent, as long as the [```JsonUtility```](https://docs.unity3d.com/2021.2/Documentation/ScriptReference/JsonUtility.html) object already exists. Additionally the example project can be opened with Unity itself or the newest release can be downloaded and exectued to test the functionality.

If you prefer the first method, you can simply install the shown Unity version and after installing it you can download the project and open it in Unity (see [Opening a Project in Unity](https://docs.unity3d.com/2021.2/Documentation/Manual/GettingStartedOpeningProjects.html)). Then you can start the game with the play button to test the Data Managers functionality.

To simply use the Data Manager in your own project without downloading the Unity project get the two files in the **Example Project/Assets/Scritps/** called ```DataManager.CS``` and ```FileData.CS``` or alternatively get the file from the newest release (may not include the newest changes) and save them in your own project. Then create a new empty ```gameObject``` and attach the ```DataManager.CS``` script to it.

# Documentation
This documentation strives to explain how to start using the Data Manager in your project and explains how to call and how to use its publicly accesible methods correctly.

## Reference to Data Manager Script
To use the Data Manager to start creating/reading files outside of itself you need to reference it. As the Data Manager is a [Singelton](https://stackoverflow.com/questions/2155688/what-is-a-singleton-in-c) this can be done easily when we get the instance and save it as a private variable in the script that uses the Data Manager.

```csharp
private DataManager dm;

private void Start() {
    dm = DataManager.instance;
    // Calling Function in DataManager
    dm.CreateNewFile(saveFile);
}
```

Alternatively you can directly call the methods this is not advised tough, if it is executed multiple times or you're going to need the instance multiple times in the same file.

```csharp
private void Start() {
    // Calling Function in DataManager
    DataManager.CreateNewFile(saveFile);
}
```

## Possible Errors

| **ID** | **CONSTANT**                  | **MEANING**                                                                                    |
| -------| ------------------------------| -----------------------------------------------------------------------------------------------|
| 0      | OK                            | Method succesfully executed                                                                    |
| 1      | INVALID_ARGUMENT              | Can not both encrypt and compress a file                                                       |
| 2      | INVALID_PATH                  | Given path does not exists in the local system                                                 |
| 3      | NOT_REGISTERED                | File has not been registered with the create file function yet                                 |
| 4      | FILE_CORRUPTED                | File has been changed outside of the environment, accessing might not be save anymore          |
| 5      | FILE_ALREADY_EXISTS           | A file already exists at the same path, choose a different name or directory                   |
| 6      | FILE_DOES_NOT_EXIST           | There is no file with the given name in the given directory, ensure it wasn't moved or deleted |
| 7      | FILE_MISSING_DEPENDENCIES     | Tried to compare hash, but hasing has not been enabled for the registered file                 |

## Public accesible methods
This section explains all public accesible methods, especially what they do, how to call them and when using them might be advantageous instead of other methods. We always assume DataManager instance has been already referenced in the script. If you haven't done that already see [Reference to Data Manager Script](#reference-to-data-manager-script).

### Create New File method
**What it does:**
Creates and registers a new file with the given properties, writes the given text to it and returns an DataError (see [Possible Errors](#possible-errors)), showing wheter and how creating the file failed.

**How to call it:**
- ```FileName``` is the name without extension we have given the file we want to register and create
- ```Content``` is the inital data that is saved into the file
- ```DirectoryPath``` is the directory the file should be saved into
- ```FileEnding``` is the extension the file should have
- ```Encryption``` decides wether the given file should be encrypted or not
- ```Hashing``` decides wether the given file should be checked for unexpected changes before using it
- ```Compression``` decides wheter the given file should be compressed or not

```csharp
string fileName = "save";
string content = "";
string directoryPath = Application.persistentDataPath;
string fileEnding = ".txt";
bool encryption = false;
bool hashing = false;
bool compression = false;
DataManager.DataError err = dm.CreateNewFile(fileName, content, directoryPath, fileEnding, encryption, hashing, compression);
if (err != DataManager.DataError.OK) {
    Debug.Log("Creating file failed with error id: " + err);
}
else {
    Debug.Log("Creating file succesfull");
}
```

Alternatively you can call the methods with less paramters as some of them have default arguments.

```csharp
string fileName = "save";
DataManager.DataError err = dm.CreateNewFile(fileName);
if (err != DataManager.DataError.OK) {
    Debug.Log("Creating file failed with error id: " + err);
}
else {
    Debug.Log("Creating file succesfull");
}
```

**When to use it:**
When you want to register and create a new file with the system so it can be used later.

### Try Read From File method
**What it does:**
Reads the content of a registered file and returns it as plain text, as well as an DataError (see [Possible Errors](#possible-errors)), showing wheter and how creating the file failed.

**How to call it:**
- ```FileName``` is the name without extension we have given the registered file and want to read now
- ```Content``` is the variable that the plain text that we read will be copied into

```csharp
string fileName = "save";
DataManager.DataError err = dm.TryReadFromFile(fileName, out string content);
if (err != DataManager.DataError.OK) {
    Debug.Log("Reading file failed with error id: " + err);
}
else {
    Debug.Log("Reading file succesfull with the content being: " + content);
}
```

**When to use it:**
When you want to read the content of a registered file and additionaly if enabled check and return if the file was modified outside of the environment or not.

### Change File Path method
**What it does:**
Changes the file location of a registered file to the new directory and returns an DataError (see [Possible Errors](#possible-errors)), showing wheter and how changing the file path failed.

**How to call it:**
- ```FileName``` is the name without extension we have given the registered file and want to move now
- ```Directory``` is the new directory the file should be saved into

```csharp
string fileName = "save";
string directory = Application.persistentDataPath;
DataManager.DataError err = dm.ChangeFilePath(fileName, directory);
if (err != DataManager.DataError.OK) {
    Debug.Log("Changing file path failed with error id: " + err);
}
else {
    Debug.Log("Changing file path succesfull to new directory: " + directory);
}
```

**When to use it:**
When you want to move the file location of a registered file.

### Update File Content method
**What it does:**
Updates the content of a registered file, completly replacing the current content and returns an DataError (see [Possible Errors](#possible-errors)), showing wheter and how replacing the file content failed.

**How to call it:**
- ```FileName``` is the name without extension we have given the registered file and want to rewrite the content of
- ```Content``` is the new content that should replace the current content

```csharp
string fileName = "save";
string content = "Example";
DataManager.DataError err = dm.UpdateFileContent(fileName, content);
if (err != DataManager.DataError.OK) {
    Debug.Log("Updating file content failed with error id: " + err);
}
else {
    Debug.Log("Updating file content succesfull to the new content: " + content);
}
```

**When to use it:**
When you want to replace the current content of a registered file with the newly given content, so that the old content is overwritten.

### Append File Content method
**What it does:**
Appends the given content to a registered file, keeping the current content and returns an DataError (see [Possible Errors](#possible-errors)), showing wheter and how appending to the file content failed.

**How to call it:**
- ```FileName``` is the name without extension we have given the registered file and want to rewrite the content of
- ```Content``` is the new content that should replace the current content

```csharp
string fileName = "save";
string content = "Example";
DataManager.DataError err = dm.AppendFileContent(fileName, content);
if (err != DataManager.DataError.OK) {
    Debug.Log("Appending file content failed with error id: " + err);
}
else {
    Debug.Log("Appending file content succesfull, added content being: " + content);
}
```

**When to use it:**
WHen you want to append the given content the current content of a registered file, so that the old content still stays in the file.

### Check File Hash method
**What it does:**
Compares the current hash with the last expected hash and returns and returns an DataError (see [Possible Errors](#possible-errors)), showing wheter and how reading  the file hash failed, or if the file hash is different than expected.

**How to call it:**
- ```FileName``` is the name without extension we have given the registered file and want to check the hash now

```csharp
string fileName = "save";
DataManager.DataError err = dm.CheckFileHash(fileName);
if (err ==  DataManager.DataError.OK) {
    Debug.Log("Hash is as expected, file has not been changed outside of the environment");
}
if (err == DataManager.DataError.FILE_CORRUPTED) {
    Debug.Log("Hash is different than expected, accessing might not be save anymore");
}
else {
    Debug.Log("Checking file hash failed with error id: " + err);
}
```

**When to use it:**
WHen you want to check if the file was changed outisde of the environment by for example the user editing the file from the file explorer.

### Delete File method
**What it does:**
Deletes an registered file and unregisters it and returns an DataError (see [Possible Errors](#possible-errors)), showing wheter and how deleting the file failed.

**How to call it:**
- ```FileName``` is the name without extension we have given the registered file and want to delete now

```csharp
string fileName = "save";
DataManager.DataError err = dm.DeleteFile(fileName);
if (err != DataManager.DataError.OK) {
    Debug.Log("Deleting file with error id: " + err);
}
else {
    Debug.Log("Deleting file succesfull");
}
```

**When to use it:**
When you want to delete a file and unregister it from the environment, if it is for example no longer needed.
