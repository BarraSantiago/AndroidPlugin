package com.barra.loggerplugin;

import android.app.Activity;
import android.util.Log;

import androidx.annotation.Keep;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.FileWriter;
import java.io.IOException;

@Keep
public class FileManager
{
    public static final String fileName = "logs.txt";

    public static final FileManager _instance = new FileManager();

    @Keep
    public static FileManager GetInstance()
    {
        return _instance;
    }

    public static Activity mainAct;

    @Keep
    private void WriteFile(String data)
    {
        File path = mainAct.getFilesDir();
        File file = new File(path, fileName);

        try
        {
            if (file.exists())
            {
                FileWriter file2 = new FileWriter(file, true);
                file2.write(data);
                file2.close();
            }
            else
            {
                FileOutputStream stream = new FileOutputStream(file);

                try
                {
                    stream.write(data.getBytes());
                }
                finally
                {
                    stream.close();
                }
            }
        }
        catch (IOException e)
        {
            Log.e("Exception", "File write failed" + e.toString());
        }
    }

    @Keep
    private String ReadFile()
    {
        File path = mainAct.getFilesDir();

        File file = new File(path, fileName);

        if (!file.exists())
        {
            return "";
        }

        int length = (int) file.length();
        byte[] bytes = new byte[length];

        try
        {
            FileInputStream stream = new FileInputStream(file);

            try
            {
                stream.read(bytes);
            }
            finally
            {
                stream.close();
            }
        }
        catch (IOException e)
        {
            Log.e("Exception", "File write failed" + e.toString());
        }

        return new String(bytes);
    }

    @Keep
    private void DeleteFiles()
    {
        File path = mainAct.getFilesDir();

        File file = new File(path, fileName);

        boolean deleted = file.delete();

        if (deleted)
        {
            Log.i("DeleteFile", "File deleted");
        }
        else
        {
            Log.i("DeleteFile", "Can't delete file");
        }
    }
}
