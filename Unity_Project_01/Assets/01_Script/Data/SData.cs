using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SData : SingletonBehabiour<SData>
{
    public void Initialize()
    {
        // ※ 베이스 코드 ※
        // if (TestData == null) TestData = testData.dataArray.ToDictionary(data => data.ID);

        if (LoadingData == null) LoadingData = loadingData.dataArray.ToDictionary(data => data.ID);
        if (LocalizeData == null) LocalizeData = localizeData.dataArray.ToDictionary(data => data.ID);
        if (ResourceData == null) ResourceData = resourceData.dataArray.ToDictionary(data => data.ID);
    }

    // ※ 베이스 코드 ※
    //[SerializeField] Text testData;
    //public static Dictionary<int, TestData> TestData;
    //public static TestData GetStoryData(int id)
    //{
    //    if (TestData.TryGetValue(id, out var data)) return data;
    //    return null;
    //}

    [SerializeField] Loading loadingData;
    public static Dictionary<int, LoadingData> LoadingData;
    public static LoadingData GetLoadingData(int id)
    {
        if (LoadingData.TryGetValue(id, out var data)) return data;
        return null;
    }

    public static LoadingData GetRandomData(int _world)
    {
        List<int> worldDatas = new List<int>();

        foreach (var data in LoadingData)
        {
            if (data.Value.World == _world)
            {
                worldDatas.Add(data.Value.ID);
            }
        }

        if (worldDatas.Count > 0)
        {
            int randomValue = UnityEngine.Random.Range(0, worldDatas.Count);
            return GetLoadingData(worldDatas[randomValue]);
        }

        return null;
    }

    [SerializeField] Localize localizeData;
    public static Dictionary<int, LocalizeData> LocalizeData;
    public static string GetLocalizeData(int _id)
    {
        if (LocalizeData.TryGetValue(_id, out var data))
        {
            if (GameManager.PData.Option.Language == LANGUAGE.KOREAN)
            {
                return data.Kor;
            }
            else if (GameManager.PData.Option.Language == LANGUAGE.ENGLISH)
            {
                return data.En;
            }
        }
        return null;
    }

    [SerializeField] Resource resourceData;
    public static Dictionary<int, ResourceData> ResourceData;
    public static ResourceData GetResourceData(int id)
    {
        if (ResourceData.TryGetValue(id, out var data)) return data;
        return null;
    }
}
