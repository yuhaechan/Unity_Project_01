using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

public class CreateStar : MonoBehaviour
{
    [SerializeField] Button btn_Phase1;
    [SerializeField] Button btn_Phase2;
    [SerializeField] Button btn_Phase3;
    [SerializeField] Button btn_Phase4;
    [SerializeField] Button btn_Phase5;

    [SerializeField] TextMeshProUGUI txt_Result;
    [SerializeField] TMP_InputField input_Count;

    void Awake()
    {
        input_Count.text = "0";
        btn_Phase1.onClick.AddListener(Phase1);
        btn_Phase2.onClick.AddListener(Phase2);
        btn_Phase3.onClick.AddListener(Phase3);
        btn_Phase4.onClick.AddListener(Phase4);
        btn_Phase5.onClick.AddListener(Phase5);
    }

    // result 변수에 출력될 결과 텍스트를 입력해주세요.
    // 텍스트 입력 방법 : result += "추가할 텍스트";
    // 줄바꿈 : \n
    // 별 : ★

    void Phase1()
    {
        string result = string.Empty; // 작성할 결과가 담길 공간입니다. 빈 텍스트를 기본으로 합니다.
        int count = int.Parse(input_Count.text); // 입력받은 수량이 담기는 int 타입의 변수입니다.

        // 예시입니다.
        // ----------------------------------------------------------------------------------------------------

        for (int i = 0; i < count; i++) // 입력된 수(count)만큼 반복합니다. 
        {
            for (int j = 0; j < i + 1; j++) // 각 줄마다 별이 추가될 수 있도록 반복합니다.
            {
                result += "★"; // 별을 추가합니다.
            }
            result += "\n"; // 줄바꿈을 추가합니다. 입력된 수 만큼 반복될 때 줄바꿈을 진행합니다.
        }

        // ----------------------------------------------------------------------------------------------------

        txt_Result.text = result == "" ? "비어 있음" : result;
    }

    void Phase2()
    {
        string result = string.Empty;
        int count = int.Parse(input_Count.text);

        // ----------------------------------------------------------------------------------------------------

        //for (int i = count; i > 0; i--) // 입력된 수(count)만큼 반복합니다. 
        //{
        //    for (int j = 0; j < i; j++) // 각 줄마다 별이 추가될 수 있도록 반복합니다.
        //    {
        //        result += "★"; // 별을 추가합니다.
        //    }
        //    result += "\n"; // 줄바꿈을 추가합니다. 입력된 수 만큼 반복될 때 줄바꿈을 진행합니다.
        //}

        for (int i = count; i > 0; i--) // 입력된 수(count)만큼 반복합니다. 
        {
            int deep = (count - 1) - i;
            //Debug.Log(deep);
            //Debug.Log(i);
            for (int j = 0; j < count; j++) // 각 줄마다 별이 추가될 수 있도록 반복합니다.
            {
                if ( j > deep )
                {
                    result += "★"; // 별을 추가합니다.
                }
                else
                {
                    result += "0";
                }
            }
            result += "\n"; // 줄바꿈을 추가합니다. 입력된 수 만큼 반복될 때 줄바꿈을 진행합니다.
        }

        // ----------------------------------------------------------------------------------------------------

        txt_Result.text = result == "" ? "비어 있음" : result;
    }

    void Phase3()
    {
        string result = string.Empty;
        int count = int.Parse(input_Count.text);

        // ----------------------------------------------------------------------------------------------------

        for (int i = 0; i < count-1; i++) // 입력된 수(count)만큼 반복합니다. 
        {
            for (int j = 0; j < i + 1; j++) // 각 줄마다 별이 추가될 수 있도록 반복합니다.
            {
                result += "★"; // 별을 추가합니다.
            }
            result += "\n"; // 줄바꿈을 추가합니다. 입력된 수 만큼 반복될 때 줄바꿈을 진행합니다.
        }

        for (int i = count; i > 0; i--) // 입력된 수(count)만큼 반복합니다. 
        {
            for (int j = 0; j < i; j++) // 각 줄마다 별이 추가될 수 있도록 반복합니다.
            {
                result += "★"; // 별을 추가합니다.
            }
            result += "\n"; // 줄바꿈을 추가합니다. 입력된 수 만큼 반복될 때 줄바꿈을 진행합니다.
        }


        // ----------------------------------------------------------------------------------------------------

        txt_Result.text = result == "" ? "비어 있음" : result;
    }

    void Phase4()
    {
        string result = string.Empty;
        int count = int.Parse(input_Count.text);

        // ----------------------------------------------------------------------------------------------------

        for (int i = 0; i < count; i++) // 입력된 수(count)만큼 반복합니다. 
        {
            int deep = (count - 1) - i;
            //Debug.Log(deep);
            for (int j = 0; j < count; j++) // 각 줄마다 별이 추가될 수 있도록 반복합니다.
            {
                if (j >= deep)
                {
                    result += "★"; // 별을 추가합니다.
                }
                else
                {
                    result += "0";
                }
            }
            result += "\n"; // 줄바꿈을 추가합니다. 입력된 수 만큼 반복될 때 줄바꿈을 진행합니다.
        }

        for (int i = count-1; i > 0; i--) // 입력된 수(count)만큼 반복합니다. 
        {
            int deep = (count - 1) - i;
            //Debug.Log(deep);
            //Debug.Log(i);
            for (int j = 0; j < count; j++) // 각 줄마다 별이 추가될 수 있도록 반복합니다.
            {
                if (j > deep)
                {
                    result += "★"; // 별을 추가합니다.
                }
                else
                {
                    result += "0";
                }
            }
            result += "\n"; // 줄바꿈을 추가합니다. 입력된 수 만큼 반복될 때 줄바꿈을 진행합니다.
        }


        // ----------------------------------------------------------------------------------------------------
        txt_Result.text = result == "" ? "비어 있음" : result;
    }

    void Phase5()
    {
        string result = string.Empty;
        int count = int.Parse(input_Count.text);

        // ----------------------------------------------------------------------------------------------------

        for (int i = 0; i < count; i++) // 입력된 수(count)만큼 반복합니다. 
        {
            float half = count / 2;
            int blacnk = (int)Mathf.Round(half) + 1 - i;

            Debug.Log(half);
            Debug.Log(blacnk);

            for (int j = 0; j < blacnk + 2; j++) // 각 줄마다 별이 추가될 수 있도록 반복합니다.
            {
                result += i; // 공백을 추가합니다.
            }

            for (int s = 0; s < i; s++)
            {
                 result += "★";
            }

            result += "\n"; // 줄바꿈을 추가합니다. 입력된 수 만큼 반복될 때 줄바꿈을 진행합니다.
        }

        for (int i = count; i >= 0; i--) // 입력된 수(count)만큼 반복합니다. 
        {
            float half = count / 2;
            int blacnk = (int)Mathf.Round(half) + 1 - i;

            Debug.Log(half);
            Debug.Log(blacnk);

            for (int j = 0; j < blacnk + 2; j++) // 각 줄마다 별이 추가될 수 있도록 반복합니다.
            {
                result += i; // 공백을 추가합니다.
            }

            for (int s = 0; s < i; s++)
            {
                result += "★";
            }

            result += "\n"; // 줄바꿈을 추가합니다. 입력된 수 만큼 반복될 때 줄바꿈을 진행합니다.
        }


        // ----------------------------------------------------------------------------------------------------

        txt_Result.text = result == "" ? "비어 있음" : result;
    }
}
