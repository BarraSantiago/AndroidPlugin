package com.barra.loggerplugin;

import android.app.Activity;
import android.widget.Toast;

import androidx.annotation.Keep;

@Keep
public class BarraLogger {

    private static Activity unityActivity;

    @Keep
    public static void receiveUnityActivity(Activity tactivity)
    {
        unityActivity = tactivity;
    }

    @Keep
    public void Toast(String msg)
    {
        Toast.makeText(unityActivity, msg, Toast.LENGTH_SHORT).show();
    }
}
