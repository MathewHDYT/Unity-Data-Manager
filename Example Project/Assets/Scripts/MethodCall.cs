using UnityEngine;

public class MethodCall : MonoBehaviour {
    private DataManager dm;

    private const string saveFile = "save";

    private void Start() {
        dm = DataManager.instance;
    }
}