[System.Serializable]
public class ValueDataError {
    public FileData Value { get; set; }
    public DataManager.DataError Error { get; set; }

    public ValueDataError(FileData value, DataManager.DataError error) {
        Value = value;
        Error = error;
    }
}