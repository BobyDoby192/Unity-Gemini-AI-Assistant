using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Text;
using System.Collections.Generic;
using System.IO;

public class GeminiAssistantWindow : EditorWindow
{
    private string apiKey = "";
    private string userPrompt = "";
    private string responseText = "Жду твоего запроса...";
    private Vector2 scrollPosition;
    private bool isRequesting = false;

    // Списки для выбора моделей (базовые значения на старте)
    private string[] models = { "gemini-1.5-flash", "gemini-1.5-pro", "gemini-pro" };
    private int selectedModelIndex = 0;

    // Настройки контекста
    private bool includeSelectedObject = true;
    private bool includeAttachedScripts = true;

    [MenuItem("Tools/AI Assistant")]
    public static void ShowWindow()
    {
        GetWindow<GeminiAssistantWindow>("Умный AI Ассистент");
    }

    private void OnGUI()
    {
        // 1. Блок авторизации и настроек ИИ
        GUILayout.Label("1. Настройки ИИ", EditorStyles.boldLabel);
        apiKey = EditorGUILayout.TextField("API Key:", apiKey);

        GUILayout.BeginHorizontal();
        selectedModelIndex = EditorGUILayout.Popup("Модель ИИ:", selectedModelIndex, models);

        if (GUILayout.Button("Загрузить с сервера", GUILayout.Width(150)))
        {
            FetchAvailableModels();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 2. Блок автоматического контекста
        GUILayout.Label("2. Автоматический контекст", EditorStyles.boldLabel);
        includeSelectedObject = EditorGUILayout.Toggle("Анализировать выделенный объект", includeSelectedObject);
        includeAttachedScripts = EditorGUILayout.Toggle("Считывать код его скриптов", includeAttachedScripts);

        GameObject activeGO = Selection.activeGameObject;
        if (activeGO != null && includeSelectedObject)
        {
            EditorGUILayout.HelpBox($"Фокус на объекте: {activeGO.name}\nКомпонентов найдено: {activeGO.GetComponents<Component>().Length}", MessageType.Info);
        }
        else if (includeSelectedObject)
        {
            EditorGUILayout.HelpBox("Выдели объект в Hierarchy, чтобы ИИ его увидел.", MessageType.None);
        }

        GUILayout.Space(10);

        // 3. Промпт игрока
        GUILayout.Label("3. Твой запрос к проекту:", EditorStyles.boldLabel);
        userPrompt = EditorGUILayout.TextArea(userPrompt, GUILayout.Height(60));

        GUILayout.Space(10);

        // Существующая кнопка отправки по API
        GUI.enabled = !isRequesting && !string.IsNullOrEmpty(apiKey);
        string buttonText = isRequesting ? "Нейросеть изучает проект..." : "Спросить ИИ внутри Unity";
        if (GUILayout.Button(buttonText, GUILayout.Height(35)))
        {
            CollectContextAndSend();
        }
        GUI.enabled = true;

        GUILayout.Space(5);

        // --- НОВАЯ КНОПКА ЭКСПОРТА ---
        if (GUILayout.Button("Скопировать контекст объекта для чата", GUILayout.Height(35)))
        {
            CopyToClipboardForChat();
        }

        // 4. Вывод ответа
        GUILayout.Label("Ответ Ассистента:", EditorStyles.boldLabel);

        // Начинаем зону прокрутки
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

        // Настраиваем кастомный стиль для текста
        GUIStyle responseStyle = new GUIStyle(EditorStyles.textArea);
        responseStyle.wordWrap = true; // Принудительный перенос слов по ширине окна
        responseStyle.richText = true; // Включаем поддержку базового форматирования

        // Выводим сам текст ответа внутри скролла
        EditorGUILayout.TextArea(responseText, responseStyle, GUILayout.ExpandHeight(true));

        // Закрываем зону прокрутки
        EditorGUILayout.EndScrollView();
    }

    private void OnSelectionChange()
    {
        Repaint();
    }

    // --- НОВЫЙ МЕТОД: АВТОМАТИЧЕСКАЯ ЗАГРУЗКА СПИСКА МОДЕЛЕЙ ---
    private void FetchAvailableModels()
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            responseText = "Сначала введи API Key для загрузки списка моделей!";
            return;
        }

        responseText = "Загружаю список доступных моделей...";
        string url = $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}";

        UnityWebRequest webRequest = UnityWebRequest.Get(url);
        var operation = webRequest.SendWebRequest();

        operation.completed += (op) =>
        {
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var modelList = JsonUtility.FromJson<GeminiModelList>(webRequest.downloadHandler.text);
                    List<string> validModels = new List<string>();

                    foreach (var m in modelList.models)
                    {
                        // Оставляем только модели семейства Gemini
                        if (m.name.Contains("gemini"))
                        {
                            validModels.Add(m.name.Replace("models/", ""));
                        }
                    }

                    if (validModels.Count > 0)
                    {
                        models = validModels.ToArray();
                        selectedModelIndex = 0; // Сбрасываем на первую модель в списке
                        responseText = $"Список моделей успешно обновлен! Найдено: {models.Length} шт.";
                    }
                }
                catch
                {
                    responseText = "Ошибка парсинга списка моделей.";
                }
            }
            else
            {
                responseText = $"Ошибка получения списка: {webRequest.error}";
            }
            Repaint();
            webRequest.Dispose();
        };
    }

    private void CollectContextAndSend()
    {
        StringBuilder fullPromptBuilder = new StringBuilder();
        GameObject activeGO = Selection.activeGameObject;

        if (includeSelectedObject && activeGO != null)
        {
            fullPromptBuilder.AppendLine("=== КОНТЕКСТ ВЫДЕЛЕННОГО ОБЪЕКТА В UNITY ===");
            fullPromptBuilder.AppendLine($"Имя объекта (GameObject): {activeGO.name}");

            fullPromptBuilder.AppendLine("\nСписок компонентов на объекте и их свойства:");
            Component[] components = activeGO.GetComponents<Component>();
            List<MonoBehaviour> customScripts = new List<MonoBehaviour>();

            foreach (var comp in components)
            {
                if (comp == null) continue;

                string compName = comp.GetType().Name;
                fullPromptBuilder.AppendLine($"- Компонент: [{compName}]");

                if (comp is MonoBehaviour mono && includeAttachedScripts)
                {
                    customScripts.Add(mono);
                }

                var fields = comp.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                foreach (var field in fields)
                {
                    object val = field.GetValue(comp);
                    fullPromptBuilder.AppendLine($"   └ Поле: {field.Name} = {val ?? "null"}");
                }
            }

            if (customScripts.Count > 0)
            {
                fullPromptBuilder.AppendLine("\n=== ИСХОДНЫЙ КОД ПРИКРЕПЛЕННЫХ СКРИПТОВ ===");
                foreach (var script in customScripts)
                {
                    string scriptName = script.GetType().Name;
                    string[] assetGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                    if (assetGuids.Length > 0)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[0]);
                        string codeText = File.ReadAllText(assetPath);
                        fullPromptBuilder.AppendLine($"--- Файл: {scriptName}.cs ---");
                        fullPromptBuilder.AppendLine(codeText);
                        fullPromptBuilder.AppendLine("--------------------------------");
                    }
                }
            }
        }

        fullPromptBuilder.AppendLine("\n=== ЗАПРОС РАЗРАБОТЧИКА ===");
        fullPromptBuilder.AppendLine(userPrompt);

        SendToGemini(fullPromptBuilder.ToString());
    }

    private void SendToGemini(string promptText)
    {
        isRequesting = true;
        responseText = "ИИ анализирует объект и код...";

        var requestData = new GeminiRequest
        {
            contents = new GeminiContent[]
            {
                new GeminiContent { parts = new GeminiPart[] { new GeminiPart { text = promptText } } }
            }
        };

        string jsonBody = JsonUtility.ToJson(requestData);
        string currentModel = models[selectedModelIndex];

        // ВАЖНО: Если модель уже содержит префикс "models/", мы его не дублируем
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{currentModel}:generateContent?key={apiKey}";

        UnityWebRequest webRequest = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/json");

        var operation = webRequest.SendWebRequest();

        operation.completed += (op) =>
        {
            isRequesting = false;

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonUtility.FromJson<GeminiResponse>(webRequest.downloadHandler.text);
                    responseText = response.candidates[0].content.parts[0].text;
                }
                catch
                {
                    responseText = "Ошибка обработки ответа от ИИ:\n" + webRequest.downloadHandler.text;
                }
            }
            else
            {
                responseText = $"Ошибка сети: {webRequest.error}\nДетали: {webRequest.downloadHandler.text}";
            }

            Repaint();
            webRequest.Dispose();
        };
    }
    private void CopyToClipboardForChat()
    {
        StringBuilder fullPromptBuilder = new StringBuilder();
        GameObject activeGO = Selection.activeGameObject;

        if (includeSelectedObject && activeGO != null)
        {
            fullPromptBuilder.AppendLine("=== СЛЕПОК ОБЪЕКТА ИЗ UNITY ===");
            fullPromptBuilder.AppendLine($"Имя: {activeGO.name}");

            Component[] components = activeGO.GetComponents<Component>();
            List<MonoBehaviour> customScripts = new List<MonoBehaviour>();

            fullPromptBuilder.AppendLine("\n--- КОМПОНЕНТЫ И ИНСПЕКТОР ---");
            foreach (var comp in components)
            {
                if (comp == null) continue;
                fullPromptBuilder.AppendLine($"- [{comp.GetType().Name}]");

                if (comp is MonoBehaviour mono && includeAttachedScripts) customScripts.Add(mono);

                var fields = comp.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                foreach (var field in fields)
                {
                    fullPromptBuilder.AppendLine($"   {field.Name}: {field.GetValue(comp) ?? "null"}");
                }
            }

            if (customScripts.Count > 0)
            {
                fullPromptBuilder.AppendLine("\n--- ИСХОДНЫЙ КОД ---");
                foreach (var script in customScripts)
                {
                    string scriptName = script.GetType().Name;
                    string[] assetGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                    if (assetGuids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(assetGuids[0]);
                        fullPromptBuilder.AppendLine($"// Файл: {scriptName}.cs");
                        fullPromptBuilder.AppendLine(File.ReadAllText(path));
                    }
                }
            }
        }

        // Копируем собранный текст в буфер обмена системы
        GUIUtility.systemCopyBuffer = fullPromptBuilder.ToString();

        responseText = "Контекст объекта успешно скопирован! Теперь нажми Ctrl+V в чате со мной.";
    }
}

// --- Структуры данных JSON ---
[System.Serializable] public class GeminiRequest { public GeminiContent[] contents; }
[System.Serializable] public class GeminiContent { public GeminiPart[] parts; }
[System.Serializable] public class GeminiPart { public string text; }
[System.Serializable] public class GeminiResponse { public GeminiCandidate[] candidates; }
[System.Serializable] public class GeminiCandidate { public GeminiContent content; }

// --- Новые структуры для парсинга списка моделей ---
[System.Serializable] public class GeminiModelList { public GeminiModelInfo[] models; }
[System.Serializable] public class GeminiModelInfo { public string name; }