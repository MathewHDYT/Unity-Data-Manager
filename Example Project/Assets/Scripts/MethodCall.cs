using UnityEngine;
using UnityEngine.UI;

public class MethodCall : MonoBehaviour {

    [Header("Input Fields:")]
    [SerializeField]
    private InputField fileNameInputField;
    [SerializeField]
    private InputField contentInputField;
    [SerializeField]
    private InputField directoryPathInputField;
    [SerializeField]
    private InputField fileEndingInputField;
    
    [Header("Input Toggles:")]
    [SerializeField]
    private Toggle encryptionToggle;
    [SerializeField]
    private Toggle hashingToggle;
    [SerializeField]
    private Toggle compressionToggle;
    
    [Header("Output:")]
    [SerializeField]
    private Text outputText;

    private DataManager dm;

    private void Start() {
        dm = DataManager.instance;
    }

    public void CreateNewFileClicked() {
        DataManager.DataError err = dm.CreateNewFile(fileNameInputField.text, contentInputField.text, directoryPathInputField.text, fileEndingInputField.text, encryptionToggle.isOn, hashingToggle.isOn, compressionToggle.isOn);
        if (err != DataManager.DataError.OK) {
            SetTextAndColor("Creating file failed with error message: " + ErrorToMessage(err), Color.red);
        }
        else {
            SetTextAndColor("Creating file succesfull", Color.green);
        }
    }

    public void TryReadFromFileClicked() {
        DataManager.DataError err = dm.TryReadFromFile(fileNameInputField.text, out string content);
        if (err != DataManager.DataError.OK) {
            SetTextAndColor("Reading file failed with error message: " + ErrorToMessage(err), Color.red);
        }
        else {
            SetTextAndColor("Reading file succesfull with the content being: " + content, Color.green);
        }
    }

    public void ChangeFilePathClicked() {
        DataManager.DataError err = dm.ChangeFilePath(fileNameInputField.text, directoryPathInputField.text);
        if (err != DataManager.DataError.OK) {
            SetTextAndColor("Changing file path failed with error message: " + ErrorToMessage(err), Color.red);
        }
        else {
            SetTextAndColor("Changing file path succesfull to new directory: " + directoryPathInputField.text, Color.green);
        }
    }

    public void UpdateFileContentClicked() {
        DataManager.DataError err = dm.UpdateFileContent(fileNameInputField.text, contentInputField.text);
        if (err != DataManager.DataError.OK) {
            SetTextAndColor("Updating file content failed with error message: " + ErrorToMessage(err), Color.red);
        }
        else {
            SetTextAndColor("Updating file content succesfull to the new content: " + contentInputField.text, Color.green);
        }
    }

    public void AppendFileContentClicked() {
        DataManager.DataError err = dm.AppendFileContent(fileNameInputField.text, contentInputField.text);
        if (err != DataManager.DataError.OK) {
            SetTextAndColor("Appending file content failed with error message: " + ErrorToMessage(err), Color.red);
        }
        else {
            SetTextAndColor("Appending file content succesfull, added content being: " + contentInputField.text, Color.green);
        }
    }

    public void CheckFileHashClicked() {
        DataManager.DataError err = dm.CheckFileHash(fileNameInputField.text);
        if (err == DataManager.DataError.OK) {
            SetTextAndColor("Hash is as expected, file has not been changed outside of the environment", Color.green);
        }
        else if (err == DataManager.DataError.FILE_CORRUPTED) {
            SetTextAndColor("Hash is different than expected, accessing might not be save anymore", Color.red);
        }
        else {
            SetTextAndColor("Checking file hash failed with error error message: " + ErrorToMessage(err), Color.red);
        }
    }

    public void DeleteFileClicked() {
        DataManager.DataError err = dm.DeleteFile(fileNameInputField.text);
        if (err != DataManager.DataError.OK) {
            SetTextAndColor("Deleting file failed with error message: ", Color.red);
        }
        else {
            SetTextAndColor("Deleting file succesfull", Color.green);
        }
    }

    private string ErrorToMessage(DataManager.DataError err) {
        string message = "";

        switch (err) {
            case DataManager.DataError.OK:
                message = "Method succesfully executed";
                break;
            case DataManager.DataError.INVALID_ARGUMENT:
                message = "Can not both encrypt and compress a file";
                break;
            case DataManager.DataError.INVALID_PATH:
                message = "Given path does not exists in the local system";
                break;
            case DataManager.DataError.NOT_REGISTERED:
                message = "File has not been registered with the create file function yet";
                break;
            case DataManager.DataError.FILE_CORRUPTED:
                message = "File has been changed outside of the environment, accessing might not be save anymore";
                break;
            case DataManager.DataError.FILE_ALREADY_EXISTS:
                message = "A file already exists at the same path, choose a different name or directory";
                break;
            case DataManager.DataError.FILE_DOES_NOT_EXIST:
                message = "There is no file with the given name in the given directory, ensure it wasn't moved or deleted";
                break;
            case DataManager.DataError.FILE_MISSING_DEPENDENCIES:
                message = "Tried to compare hash, but hasing has not been enabled for the registered file";
                break;
            default:
                // Invalid DataManager.DataError argument.
                break;
        }

        return message;
    }

    private void SetTextAndColor(string text, Color color) {
        outputText.text = text;
        outputText.color = color;
    }
}
