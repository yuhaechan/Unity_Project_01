using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LANGUAGE
{
    KOREAN = 0,
    ENGLISH,
}

[System.Serializable]
public class PlayerData
{
    public PlayerData()
    {
        Option = new PlayerOption();
        NowWorldID = 2001;
    }

    public PlayerOption Option;

    public int NowWorldID;
}

[System.Serializable]
public class PlayerOption
{
    public PlayerOption()
    {
        Language = LANGUAGE.KOREAN;
        BGMVol = 1;
        SFXVol = 1;
    }

    public LANGUAGE Language;

    public float BGMVol;
    public float SFXVol;
}