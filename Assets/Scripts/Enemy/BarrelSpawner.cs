using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrelSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    public GameObject barrelPrefab;      // 소환할 술통 프리팹
    public int spawnIntervalMin;
    public int spawnIntervalMax;
    private int spawnInterval;
    bool isActive = true;
    Coroutine coroutine;

    List<GameObject> currentBarrels = new List<GameObject>();
    [Header("이동 방향 설정")]
    [Tooltip("소환된 술통이 굴러갈 초기 방향 (오른쪽: 1,0,0 / 왼쪽: -1,0,0)")]
    public Vector3 spawnDirection = Vector3.right;

    private float timer = 0f;

    void Update()
    {


    }


    public void SetActive(bool value)
    {
        isActive = value;
    }
    public void StartSpawnBarrel()
    {
        coroutine = StartCoroutine(SpawnBarrelCour());
    }

    public void ReStartSpawnBarrel()
    {
        StopCoroutine(coroutine);
        coroutine = null;

        foreach (var barrel in currentBarrels)
        {
            Destroy(barrel);
        }
        currentBarrels = new List<GameObject>();
        coroutine = StartCoroutine(SpawnBarrelCour());
    }
    IEnumerator SpawnBarrelCour()
    {
        while (isActive)
        {
            SpawnBarrel();
            spawnInterval = UnityEngine.Random.Range(spawnIntervalMin, spawnIntervalMax);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnBarrel()
    {
        if (barrelPrefab == null) return;

        // 스포너의 위치와 회전값을 기준으로 술통 소환
        GameObject barrelObj = Instantiate(barrelPrefab, transform.position, transform.rotation);
        currentBarrels.Add(barrelObj);
        // 소환된 술통 스크립트의 이동 방향을 인스펙터에서 설정한 방향으로 덮어씌움
        Barrel barrel = barrelObj.GetComponent<Barrel>();
        if (barrel != null)
        {
            barrel.moveDirection = spawnDirection.normalized;
        }
    }
}
