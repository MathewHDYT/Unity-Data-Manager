using UnityEngine;

public class MethodCall : MonoBehaviour {
    private DataManager dm;

    private const string SAVE_FILE = "save";

    private void Start() {
        dm = DataManager.instance;
    }
}