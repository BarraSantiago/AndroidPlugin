using UnityEngine;
using UnityEngine.UI;
using System;

public class LoggerPluginManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private InputField logInputField;
    [SerializeField] private Button sendLogButton;
    [SerializeField] private Button readLogsButton;
    [SerializeField] private Button clearLogsButton;
    [SerializeField] private Text logsDisplayText;
    [SerializeField] private ScrollRect logsScrollRect;

    private AndroidJavaObject barraLoggerInstance;
    private AndroidJavaObject fileManagerInstance;
    private AndroidJavaObject popUpInstance;

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        InitializePlugin();
        SetupUI();
#else
        Debug.LogWarning("Plugin only works on Android devices");
        if (logsDisplayText != null)
            logsDisplayText.text = "Plugin only available on Android";
#endif
    }

    private void InitializePlugin()
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                // Initialize BarraLogger
                using (AndroidJavaClass barraLoggerClass = new AndroidJavaClass("com.barra.loggerplugin.BarraLogger"))
                {
                    barraLoggerClass.CallStatic("receiveUnityActivity", activity);
                    barraLoggerInstance = new AndroidJavaObject("com.barra.loggerplugin.BarraLogger");
                }

                // Initialize FileManager
                using (AndroidJavaClass fileManagerClass = new AndroidJavaClass("com.barra.loggerplugin.FileManager"))
                {
                    fileManagerInstance = fileManagerClass.CallStatic<AndroidJavaObject>("GetInstance");
                    fileManagerClass.SetStatic("mainAct", activity);
                }

                // Initialize PopUp
                using (AndroidJavaClass popUpClass = new AndroidJavaClass("com.barra.loggerplugin.PopUp"))
                {
                    popUpInstance = popUpClass.CallStatic<AndroidJavaObject>("GetInstance");
                    popUpClass.SetStatic("mainAct", activity);
                }

                Debug.Log("Plugin initialized successfully");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize plugin: {e.Message}");
        }
    }

    private void SetupUI()
    {
        if (sendLogButton != null)
            sendLogButton.onClick.AddListener(OnSendLogClicked);

        if (readLogsButton != null)
            readLogsButton.onClick.AddListener(OnReadLogsClicked);

        if (clearLogsButton != null)
            clearLogsButton.onClick.AddListener(OnClearLogsClicked);
    }

    public void SendLog(string logMessage)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            string timestampedLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {logMessage}\n";
            fileManagerInstance.Call("WriteFile", timestampedLog);
            barraLoggerInstance.Call("Toast", "Log saved!");
            Debug.Log($"Log sent: {logMessage}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send log: {e.Message}");
        }
#endif
    }

    public string ReadLogs()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            string logs = fileManagerInstance.Call<string>("ReadFile");
            return string.IsNullOrEmpty(logs) ? "No logs available" : logs;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to read logs: {e.Message}");
            return "Error reading logs";
        }
#else
        return "Android only";
#endif
    }

    public void ShowDeleteConfirmation()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            string[] alertParams = new string[]
            {
                "Delete Logs",
                "Are you sure you want to delete all logs? This action cannot be undone.",
                "Cancel",
                "Delete"
            };

            popUpInstance.Call("ShowAlertView", new object[] { alertParams, new AlertCallback(this) });
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to show alert: {e.Message}");
        }
#endif
    }

    private void DeleteLogs()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            fileManagerInstance.Call("DeleteFiles");
            barraLoggerInstance.Call("Toast", "Logs deleted!");
            if (logsDisplayText != null)
                logsDisplayText.text = "Logs cleared";
            Debug.Log("Logs deleted");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete logs: {e.Message}");
        }
#endif
    }

    private void OnSendLogClicked()
    {
        if (logInputField != null && !string.IsNullOrEmpty(logInputField.text))
        {
            SendLog(logInputField.text);
            logInputField.text = "";
        }
    }

    private void OnReadLogsClicked()
    {
        string logs = ReadLogs();
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
        ShowDeleteConfirmation();
    }

    private class AlertCallback : AndroidJavaProxy
    {
        private LoggerPluginManager manager;

        public AlertCallback(LoggerPluginManager manager) : base("com.barra.loggerplugin.PopUp$AlertViewCallBack")
        {
            this.manager = manager;
        }

        public void OnButtonTapped(int buttonId)
        {
            Debug.Log($"Button tapped: {buttonId}");
            // AlertDialog.BUTTON_NEGATIVE = -2 (Delete button)
            if (buttonId == -2)
            {
                manager.DeleteLogs();
            }
        }
    }

    void OnDestroy()
    {
        if (sendLogButton != null)
            sendLogButton.onClick.RemoveAllListeners();
        if (readLogsButton != null)
            readLogsButton.onClick.RemoveAllListeners();
        if (clearLogsButton != null)
            clearLogsButton.onClick.RemoveAllListeners();
    }
}
