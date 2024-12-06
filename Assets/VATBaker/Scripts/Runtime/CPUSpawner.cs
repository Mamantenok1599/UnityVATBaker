using UnityEngine;

public class CPUSpawner : MonoBehaviour
{
    [Header("Prefabs and Settings")]
    public GameObject prefab;
    public int instanceCount = 1000;
    public Vector3 spawnAreaSize = new Vector3(100f, 0f, 100f);
    public Vector3 spawnAreaCenter = Vector3.zero;

    void Start()
    {
        SpawnOnCPU();
    }

    void SpawnOnCPU()
    {
        for (int i = 0; i < instanceCount; i++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(spawnAreaCenter.x - spawnAreaSize.x/2, spawnAreaCenter.x + spawnAreaSize.x/2),
                spawnAreaCenter.y,
                Random.Range(spawnAreaCenter.z - spawnAreaSize.z/2, spawnAreaCenter.z + spawnAreaSize.z/2)
            );
            Quaternion rot = Quaternion.Euler(0, Random.Range(0,360f), 0);
            Instantiate(prefab, randomPos, rot, this.transform);
        }
    }
}
