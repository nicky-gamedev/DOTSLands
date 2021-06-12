using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class ScreenGuard
{
    private int prevWidth;
    private int prevHeight;

    private bool isInited = false;


    private void Init()
    {
        prevWidth = Screen.width;
        prevHeight = Screen.height;

        isInited = true;
    }

    private void SetResolution()
    {
        prevWidth = Screen.width;
        prevHeight = Screen.height;

        Screen.SetResolution(Screen.width, Screen.height, Screen.fullScreen);
    }

    public void Update()
    {
        if (!isInited)
        {
            Init();
        }

        if ((Screen.width != prevWidth) || (Screen.height != prevHeight))
        {
            SetResolution();
        }
    }
}
