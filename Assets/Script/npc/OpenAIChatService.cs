using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// ChatGPT(OpenAI) API를 호출해서 NPC 인사말 한 줄을 받아오는 서비스.
/// API 키는 OpenAIConfig.cs(git에 올라가지 않는 파일)에서 읽어옴.
/// </summary>
public class OpenAIChatService : MonoBehaviour
{
    public static OpenAIChatService Instance { get; private set; }

    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";

    [Header("모델 설정")]
    [Tooltip("모델명/가격은 바뀔 수 있으니 https://platform.openai.com/docs/pricing 에서 최신 확인 후 필요하면 수정")]
    [SerializeField] private string model = "gpt-5.6-luna";

    [Header("디버그")]
    [SerializeField] private bool showDebugLog = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static OpenAIChatService GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        Instance = FindFirstObjectByType<OpenAIChatService>();

        if (Instance != null)
            return Instance;

        GameObject managerObject = new GameObject("OpenAIChatService");
        Instance = managerObject.AddComponent<OpenAIChatService>();
        return Instance;
    }

    /// <summary>
    /// NPC가 되어 플레이어에게 한 줄 인사말을 생성해서 돌려줌.
    /// 실패하면 onError로 이유를 전달함 (호출한 쪽에서 기존 대사로 대체하는 걸 추천).
    /// </summary>
    public void RequestGreeting(
        string npcName,
        Action<string> onSuccess,
        Action<string> onError)
    {
        StartCoroutine(RequestGreetingRoutine(npcName, onSuccess, onError));
    }

    private IEnumerator RequestGreetingRoutine(
        string npcName,
        Action<string> onSuccess,
        Action<string> onError)
    {
        if (string.IsNullOrEmpty(OpenAIConfig.ApiKey) ||
            OpenAIConfig.ApiKey.Contains("여기에"))
        {
            Debug.LogWarning("OpenAI API 키가 설정되지 않았습니다. OpenAIConfig.cs를 확인하세요.");
            onError?.Invoke("API 키가 설정되지 않았습니다.");
            yield break;
        }

        string systemPrompt =
            "너는 게임 속 마을 NPC야. 이름은 \"" + npcName + "\"이야. " +
            "플레이어가 말을 걸면 짧고 반가운 인사말을 딱 한 문장으로만 해. " +
            "설명이나 따옴표 없이 대사만 출력해.";

        ChatRequest requestBody = new ChatRequest
        {
            model = model,
            messages = new ChatMessage[]
            {
                new ChatMessage { role = "system", content = systemPrompt },
                new ChatMessage { role = "user", content = "플레이어가 말을 걸었다." }
            }
        };

        string json = JsonUtility.ToJson(requestBody);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(ApiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + OpenAIConfig.ApiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "OpenAI 요청 실패: " + request.error +
                    "\n" + request.downloadHandler.text
                );

                onError?.Invoke(request.error);
                yield break;
            }

            ChatResponse response =
                JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);

            if (response == null ||
                response.choices == null ||
                response.choices.Length == 0 ||
                response.choices[0].message == null)
            {
                onError?.Invoke("응답이 비어 있습니다.");
                yield break;
            }

            string reply = response.choices[0].message.content;

            if (string.IsNullOrEmpty(reply))
            {
                onError?.Invoke("응답 내용이 비어 있습니다.");
                yield break;
            }

            reply = reply.Trim();

            if (showDebugLog)
            {
                Debug.Log("ChatGPT 인사말 생성: " + reply);
            }

            onSuccess?.Invoke(reply);
        }
    }

    [Serializable]
    private class ChatRequest
    {
        public string model;
        public ChatMessage[] messages;
    }

    [Serializable]
    private class ChatMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class ChatResponse
    {
        public Choice[] choices;
    }

    [Serializable]
    private class Choice
    {
        public ChatMessage message;
    }
}
