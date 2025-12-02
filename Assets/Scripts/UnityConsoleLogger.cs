using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class UnityConsoleLogger : MonoBehaviour
{
    private static UnityConsoleLogger instance;

    [Header("UI References")]
    [SerializeField] private Button readLogsButton;
    [SerializeField] private Button clearLogsButton;
    [SerializeField] private TMP_Text logsDisplayText;
    [SerializeField] private ScrollRect logsScrollRect;

    [Header("Logging Settings")]
    [SerializeField] private bool logDebugMessages = true;
    [SerializeField] private bool logWarnings = true;
    [SerializeField] private bool logErrors = true;
    [SerializeField] private bool logExceptions = true;

    private AndroidJavaObject loggerInstance;
    private bool isInitialized = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLogger();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        SetupUI();
    }

    private void InitializeLogger()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                using (AndroidJavaClass loggerClass = new AndroidJavaClass("com.barra.loggerplugin.Logger"))
                {
                    loggerClass.CallStatic("Initialize", activity);
                    loggerInstance = loggerClass.CallStatic<AndroidJavaObject>("getInstance");
                }

                isInitialized = true;
                Debug.Log("UnityConsoleLogger initialized successfully");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize UnityConsoleLogger: {e.Message}");
        }
#else
        Debug.LogWarning("Logger plugin only works on Android devices");
#endif
    }

    private void SetupUI()
    {
        if (readLogsButton != null)
            readLogsButton.onClick.AddListener(OnReadLogsClicked);

        if (clearLogsButton != null)
            clearLogsButton.onClick.AddListener(OnClearLogsClicked);

#if !UNITY_ANDROID || UNITY_EDITOR
        if (logsDisplayText != null)
            logsDisplayText.text = "Plugin only available on Android";
#endif
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!isInitialized || loggerInstance == null)
            return;

        bool shouldLog = false;
        string logPrefix = "";

        switch (type)
        {
            case LogType.Error:
                shouldLog = logErrors;
                logPrefix = "[ERROR] ";
                break;
            case LogType.Assert:
                shouldLog = logErrors;
                logPrefix = "[ASSERT] ";
                break;
            case LogType.Warning:
                shouldLog = logWarnings;
                logPrefix = "[WARNING] ";
                break;
            case LogType.Log:
                shouldLog = logDebugMessages;
                logPrefix = "[LOG] ";
                break;
            case LogType.Exception:
                shouldLog = logExceptions;
                logPrefix = "[EXCEPTION] ";
                break;
        }

        if (shouldLog)
        {
            try
            {
                string fullLog = logPrefix + logString;

                if ((type == LogType.Error || type == LogType.Exception) && !string.IsNullOrEmpty(stackTrace))
                {
                    fullLog += "\n" + stackTrace;
                }

                loggerInstance.Call("SendLog", fullLog);
            }
            catch (Exception e)
            {
                // Avoid recursive logging by not using Debug.LogError here
            }
        }
#endif
    }

    private void OnReadLogsClicked()
    {
        string logs = GetAllLogs();
        if (logsDisplayText != null)
        {
            logsDisplayText.text = logs;
            Canvas.ForceUpdateCanvases();
            if (logsScrollRect != null)
                logsScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void OnClearLogsClicked()
    {
        ShowClearLogsAlert();
    }

    public string GetAllLogs()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!isInitialized || loggerInstance == null)
            return "Logger not initialized";

        try
        {
            return loggerInstance.Call<string>("GetLogs");
        }
        catch (Exception e)
        {
            return $"Error retrieving logs: {e.Message}";
        }
#else
        return "Android only";
#endif
    }

    private void ShowClearLogsAlert()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!isInitialized || loggerInstance == null)
            return;

        try
        {
            loggerInstance.Call("ShowClearLogsAlert", new ClearLogsCallback(this));
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to show clear logs alert: {e.Message}");
        }
#endif
    }

    private void OnLogsCleared(bool success)
    {
        if (success && logsDisplayText != null)
        {
            logsDisplayText.text = "Logs cleared successfully";
        }
    }

    private class ClearLogsCallback : AndroidJavaProxy
    {
        private UnityConsoleLogger logger;

        public ClearLogsCallback(UnityConsoleLogger logger) : base("com.barra.loggerplugin.Logger$OnClearLogsListener")
        {
            this.logger = logger;
        }

        public void onClearLogsResult(bool success)
        {
            logger.OnLogsCleared(success);
        }
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;

        if (readLogsButton != null)
            readLogsButton.onClick.RemoveAllListeners();
        if (clearLogsButton != null)
            clearLogsButton.onClick.RemoveAllListeners();
    }
}