using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StarrailControl : ControlBase<StarrailControl>
{
    [SerializeField] Button btn_Click;
    [SerializeField] Button btn_World;
    [SerializeField] Button btn_Localize;

    protected override void Awake()
    {
        base.Awake();

        btn_Click.onClick.AddListener(Click);
        btn_World.onClick.AddListener(ChangeWorld);
        btn_Localize.onClick.AddListener(ChangeLocalize);
    }

    protected override void Start()
    {
        base.Start();
    }

    public override void Open(PlayerData _pData)
    {
        base.Open(_pData);
    }

    public override void Initialize()
    {
        base.Initialize();
        //Debug.Log(PData.NowWorldID);
        nowData = SData.GetRandomData(PData.NowWorldID);
        RefreshUI();
    }

    LoadingData nowData;

    [SerializeField] Image[] img_BG;
    [SerializeField] Image img_Pom;

    [SerializeField] TextMeshProUGUI txt_Title;
    [SerializeField] TextMeshProUGUI txt_Text;
    [SerializeField] Image img_Icon;

    public void ChangeWorld()
    {
        Debug.Log(PData.NowWorldID);
        PData.NowWorldID = PData.NowWorldID == 2001 ? 2002 : 2001; // 삼항연산자

        Click();
    }

    public void ChangeLocalize()
    {
        if (PData.Option.Language == LANGUAGE.KOREAN)
        {
            PData.Option.Language = LANGUAGE.ENGLISH;
        }
        else
        {
            PData.Option.Language = LANGUAGE.KOREAN;
        }

        RefreshTextUI();
    }

    public void Click()
    {
        nowData = SData.GetRandomData(PData.NowWorldID);
        RefreshUI();
    }

    public void RefreshUI()
    {
        for (int i = 0; i < img_BG.Length; i++)
        {
            img_BG[i].sprite = Resources.Load<Sprite>(GetResourcePath(nowData.World)); // 배경 이미지 변경
        }

        img_Pom.sprite = Resources.Load<Sprite>(GetResourcePath(nowData.World + 100)); // 폼폼 이미지 변경

        img_Icon.sprite = Resources.Load<Sprite>(GetResourcePath(nowData.Icon));

        RefreshTextUI();
    }

    void RefreshTextUI()
    {
        txt_Title.text = SData.GetLocalizeData(nowData.Titletext);
        txt_Text.text = SData.GetLocalizeData(nowData.Text);
    }

    string GetResourcePath(int _id)
    {
        return SData.GetResourceData(_id).Path;
    }
}
