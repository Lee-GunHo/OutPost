using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [SerializeField] private Transform player;
    [SerializeField] private string saveFileName = "SaveFile.json";

    private void Awake()
    {
        Instance = this;
    }

    public void SaveGame()
    {
        if (player == null)
        {
            Debug.LogError("[SAVE] Player가 연결되지 않았습니다.");
            return;
        }

        SaveData data = new SaveData();

        data.playerX = player.position.x;
        data.playerY = player.position.y;
        data.playerZ = player.position.z;

        data.playTime = Time.time;

        string json = JsonUtility.ToJson(data, true);

        string path = Path.Combine(Application.persistentDataPath, saveFileName);

        File.WriteAllText(path, json);

        Debug.Log("[SAVE] 저장 완료: " + path);
    }
}

[System.Serializable]
public class SaveData
{
    public float playerX;
    public float playerY;
    public float playerZ;

    public float playTime;
}