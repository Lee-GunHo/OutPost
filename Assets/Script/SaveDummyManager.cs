using UnityEngine;
using System.IO;

public class SaveDummyCreator : MonoBehaviour
{
    [SerializeField] private string saveFileName = "SaveFile.json";

    private string SavePath =>
        Path.Combine(Application.persistentDataPath, saveFileName);

    [ContextMenu("Create Dummy Save")]
    public void CreateDummySave()
    {
        if (File.Exists(SavePath))
        {
            Debug.Log("이미 세이브 파일이 존재함");
            return;
        }

        string dummyJson = "{ \"isDummy\": true }";
        File.WriteAllText(SavePath, dummyJson);

        Debug.Log($"더미 세이브 생성 완료\nPath: {SavePath}");
    }

    [ContextMenu("Delete Save")]
    public void DeleteSave()
    {
        if (!File.Exists(SavePath)) return;
        File.Delete(SavePath);
        Debug.Log("세이브 파일 삭제 완료");
    }
}