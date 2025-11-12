using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

[System.Serializable]
public class PlayerStat
{
    public string Name;
    public int Value;
    public int Vasic;
    // ...
}


public class Stats : MonoBehaviour
{
    public int PlayerLevel = 1;
    public int Status;
    // 총스탯은 4개로 이루어져 있음 (str,dex,int,luck)
    public List<PlayerStat> PlayerStats = new List<PlayerStat>();


    public void Update()
    {

        //테스트용으로 일단 이렇게 만듬
        if (Input.GetKeyDown(KeyCode.Space))
        {
            NextStage();
        }
        // 이건 창을 켰다고 가정
        if (Input.GetKeyDown(KeyCode.U))
        {
        }

    }
    // 다음 스테이지로 갈때 실행, 레벨업,스탯 5획득
    public void NextStage()
    {
        print("레벨업!");
        PlayerLevel++;
        Status += 5;
    }

    //***** 스탯 증가 함수 *****//
    public void Stats_Upgrade(string statName)
    {
        // 1. 찍을 스탯이 부족할시 메세지 출력
        if (Status <= 0)
        {
            Debug.Log("스탯 포인트가 부족합니다!");
            return;
        }

        // 2. for문을 통해서 누른 버튼이 어떤 스탯인지 확인
        for (int i = 0; i < PlayerStats.Count; i++)
        {
            if (PlayerStats[i].Name == statName)
            {
                PlayerStats[i].Value++;

                Status--; // 스탯 포인트 1 소모
                Debug.Log($"{statName}이 1 증가했습니다. 남은 포인트: {Status}");
                return;
            }
        }
    }

    //***** 스탯 초기화 함수 *****//
    public void Stats_Reset()
    {
        for (int i = 0; i < PlayerStats.Count; i++)
        {
            Status += PlayerStats[i].Value; // 스탯 포인트 복구

            PlayerStats[i].Value = 0;

        }

        print($"스탯 초기화 남은 포인트 {Status}");

    }

}


