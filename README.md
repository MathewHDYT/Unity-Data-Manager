![Unity Data Manager](https://github.com/MathewHDYT/Unity-Data-Manager-UDM/blob/main/logo.png/)

[![MIT license](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://lbesson.mit-license.org/)
[![Unity](https://img.shields.io/badge/Unity-5.3%2B-green.svg?style=flat-square)](https://docs.unity3d.com/530/)
[![GitHub release](https://img.shields.io/github/release/MathewHDYT/Unity-Data-Manager-UDM/all.svg?style=flat-square)](https://github.com/MathewHDYT/Unity-Data-Manager-UDM/releases/)
[![GitHub downloads](https://img.shields.io/github/downloads/MathewHDYT/Unity-Data-Manager-UDM/all.svg?style=flat-square)](https://github.com/MathewHDYT/Unity-Data-Manager-UDM/releases/)

# Unity Data Manager (UDM)

## Contents
- [Unity Data Manager (UDM)](#unity-data-manager-udm)
  - [Contents](#contents)
  - [Introduction](#introduction)
  - [Installation](#installation)
- [Documentation](#documentation)
  - [Reference to Data Manager Script](#reference-to-data-manager-script)
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
To use the Data Manager to start playing sounds outside of itself you need to reference it. As the Data Manager is a [Singelton](https://stackoverflow.com/questions/2155688/what-is-a-singleton-in-c) this can be done easily when we get the instance and save it as a private variable in the script that uses the Data Manager.

```csharp
private DataManager dm;

private const string saveFile = "save";

private void Start() {
    dm = DataManager.instance;
    // Calling Function in DataManager
    dm.CreateNewFile(saveFile);
}
```

Alternatively you can directly call the methods this is not advised tough, if it is executed multiple times or you're going to need the instance multiple times in the same file.

```csharp
private const string saveFile = "save";

private void Start() {
    // Calling Function in DataManager
    DataManager.CreateNewFile(saveFile);
}
```

## Public accesible methods
This section explains all public accesible methods, especially what they do, how to call them and when using them might be advantageous instead of other methods. We always assume DataManager instance has been already referenced in the script. If you haven't done that already see [Reference to Data Manager Script](#reference-to-data-manager-script).

### Create New File method
**What it does:**
Creates and registers a new file with the possible options and content you want to write into that file.

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
dm.CreateNewFile(fileName, content, directoryPath, fileEnding, encryption, hashing, compression);
```

Alternatively you can call the methods with less paramters as some of them have default arguments.

```csharp
string fileName = "save";
dm.CreateNewFile(fileName);
```

**When to use it:**
When you want to register and create a new file with the system so it can be used later.

### Try Read From File method
**What it does:**
Reads the content of a registered file and returns it as plain text, as well as a boolean deciding wheter the hash was changed outside the environment, if it was enabled for the given file in the [Create New File method](#create-new-file-method).

**How to call it:**
- ```FileName``` is the name without extension we have given the registered file and want to read now
- ```Content``` is the variable that the plain text that we read will be copied into

```csharp
string fileName = "save";
string content = "";
if (dm.TryReadFromFile(fileName, content)) {
    Debug.Log("Reading the file was succesfull, it has not been modified outside of the environment, with the read data being: " + content);
}
else {
    Debug.Log("Reading the file failed, as the file has been modified outside of the environment");
}
```

**When to use it:**
When you want to read the content of a registered file and additionaly if enabled check and return if the file was modified outside of the environment or not.

### Change File Path method
**What it does:**
Moves the file location of a registered file to the new directory and returns true if it succeded.

**How to call it:**
- ```FileName``` is the name without extension we have given the registered file and want to move now
- ```Directory``` is the new directory the file should be saved into

```csharp
string fileName = "save";
string directory = Application.persistentDataPath;
if (dm.ChangeFilePath(fileName, directory)) {
    Debug.Log("Moving the file to the new directory: " + directory + " was succesfull");
}
else {
    Debug.Log("Moving the file to the new directory: " + directory + " failed");
}
```

**When to use it:**
WHen you want to move the file location of a registered file.

### Update File Content method
**What it does:**
Replaces all the current content in the registered file with the new given content and returns true if it succeded.

**How to call it:**
- ```FileName``` is the name without extension we have given the registered file and want to rewrite the content of
- ```Content``` is the new content that should replace the current content

```csharp
string fileName = "save";
string content = "Example";
if (dm.UpdateFileContent(fileName, content)) {
    Debug.Log("Updating the file to the new content: " + content + " was succesfull");
}
else {
    Debug.Log("Updating the file to the new content: " + content + " failed");
}
```

**When to use it:**
WHen you want to replace the current content of a registered file with the newly given content, so that the old content is overwritten.

### Append File Content method
**What it does:**
Appends the given content to the current content in the registered file and returns true if it succeded.

**How to call it:**
- ```FileName``` is the name without extension we have given the registered file and want to rewrite the content of
- ```Content``` is the new content that should replace the current content

```csharp
string fileName = "save";
string content = "Example";
if (dm.AppendFileContent(fileName, content)) {
    Debug.Log("Appending the content: " + content + " to the file was succesfull");
}
else {
    Debug.Log("Appending the content: " + content + " to the file failed"));
}
```

**When to use it:**
WHen you want to append the given content the current content of a registered file, so that the old content still stays in the file.

### Check File Hash method
**What it does:**
Compares the current hash of the file with the expected hash we have saved in our environment, should be the hash for the last internal changes (write access). Returns false if hashing was not enabled for the registered file.

**How to call it:**
- ```FileName``` is the name without extension we have given the registered file and want to check the hash now

```csharp
string fileName = "save";
if (!dm.CheckFileHash(fileName)) {
    Debug.Log("Given file was changedd outside of the environment since last accesing it");
}
```

**When to use it:**
WHen you want to check if the file was changed outisde of the environment by for example the user editing the file from the file explorer.

### Delete File method
**What it does:**
Deletes the registered file and unregisters it from the environment.

**How to call it:**
- ```FileName``` is the name without extension we have given the registered file and want to delete now

```csharp
string fileName = "save";
if (dm.DeleteFile(fileName)) {
    Debug.Log("Deleting the file " + fileName + " was succesfull");
}
else {
    Debug.Log("Deleting the file " + fileName + " failed");
}
```

**When to use it:**
WHen you want to delete a file and unregister it from the environment, if it is for example no longer needed.
