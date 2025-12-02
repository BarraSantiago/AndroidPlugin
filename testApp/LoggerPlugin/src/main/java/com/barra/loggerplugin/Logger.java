package com.barra.loggerplugin;

import android.app.AlertDialog;
import android.content.Context;
import android.util.Log;

import java.io.BufferedReader;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;

public class Logger {
    private static final String TAG = "BarraLogger";
    private static final String LOG_FILE_NAME = "unity_logs.txt";
    private static Logger _instance;
    private Context context;

    private Logger(Context context) {
        this.context = context.getApplicationContext();
    }

    public static void Initialize(Context context) {
        if (_instance == null) {
            _instance = new Logger(context);
        }
    }

    public static Logger getInstance() {
        if (_instance == null) {
            throw new IllegalStateException("Logger must be initialized with a Context first");
        }
        return _instance;
    }

    // Enviar y registrar log desde Unity
    public void SendLog(String log) {
        String timestamp = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss", Locale.getDefault()).format(new Date());
        String formattedLog = "[" + timestamp + "] " + log;

        Log.i(TAG, log);
        SaveLogToFile(formattedLog);
    }

    // Guardar log en archivo
    private void SaveLogToFile(String log) {
        FileOutputStream fos = null;
        try {
            File file = new File(context.getFilesDir(), LOG_FILE_NAME);
            fos = new FileOutputStream(file, true); // append mode
            fos.write((log + "\n").getBytes());
        } catch (IOException e) {
            Log.e(TAG, "Error saving log to file", e);
        } finally {
            if (fos != null) {
                try {
                    fos.close();
                } catch (IOException e) {
                    Log.e(TAG, "Error closing file stream", e);
                }
            }
        }
    }

    // Devolver todos los logs guardados
    public String GetLogs() {
        StringBuilder logs = new StringBuilder();
        FileInputStream fis = null;
        BufferedReader reader = null;

        try {
            File file = new File(context.getFilesDir(), LOG_FILE_NAME);
            if (!file.exists()) {
                return "No logs available";
            }

            fis = new FileInputStream(file);
            reader = new BufferedReader(new InputStreamReader(fis));

            String line;
            while ((line = reader.readLine()) != null) {
                logs.append(line).append("\n");
            }
        } catch (IOException e) {
            Log.e(TAG, "Error reading logs from file", e);
            return "Error reading logs: " + e.getMessage();
        } finally {
            try {
                if (reader != null) reader.close();
                if (fis != null) fis.close();
            } catch (IOException e) {
                Log.e(TAG, "Error closing streams", e);
            }
        }

        return logs.toString();
    }

    // Mostrar alerta de verificación antes de borrar logs
    public void ShowClearLogsAlert(final OnClearLogsListener listener) {
        if (context instanceof android.app.Activity) {
            final android.app.Activity activity = (android.app.Activity) context;
            activity.runOnUiThread(new Runnable() {
                @Override
                public void run() {
                    new AlertDialog.Builder(activity)
                            .setTitle("Clear Logs")
                            .setMessage("Are you sure you want to delete all logs? This action cannot be undone.")
                            .setPositiveButton("Yes", (dialog, which) -> {
                                boolean success = ClearLogs();
                                if (listener != null) {
                                    listener.onClearLogsResult(success);
                                }
                            })
                            .setNegativeButton("No", (dialog, which) -> {
                                dialog.dismiss();
                                if (listener != null) {
                                    listener.onClearLogsResult(false);
                                }
                            })
                            .setCancelable(false)
                            .show();
                }
            });
        }
    }

    // Borrar archivo de logs
    private boolean ClearLogs() {
        try {
            File file = new File(context.getFilesDir(), LOG_FILE_NAME);
            if (file.exists()) {
                boolean deleted = file.delete();
                if (deleted) {
                    Log.i(TAG, "Logs cleared successfully");
                }
                return deleted;
            }
            return true;
        } catch (Exception e) {
            Log.e(TAG, "Error clearing logs", e);
            return false;
        }
    }

    // Interfaz para callback de limpieza de logs
    public interface OnClearLogsListener {
        void onClearLogsResult(boolean success);
    }
}