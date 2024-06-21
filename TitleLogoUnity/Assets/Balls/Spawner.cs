using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject ballPrefab;

    private bool isSpawning = false;
    private void Start()
    {

    }
    
    private void SpawnBall()
    {
        // このオブジェクトの位置、回転、スケールで求まる範囲内にランダムな位置を求める
        Vector3 position = transform.position + new Vector3(
            UnityEngine.Random.Range(-transform.localScale.x / 2, transform.localScale.x / 2),
            UnityEngine.Random.Range(-transform.localScale.y / 2, transform.localScale.y / 2),
            UnityEngine.Random.Range(-transform.localScale.z / 2, transform.localScale.z / 2)
        );
        // プレハブを生成する
        GameObject ball = Instantiate(ballPrefab, position, Quaternion.identity);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isSpawning = !isSpawning;
        }

        if (isSpawning)
        {
            SpawnBall();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
