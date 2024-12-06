using UnityEngine;

public class GPUInstancedSpawner : MonoBehaviour
{
    [Header("Mesh and Material")]
    public Mesh mesh; 
    public Material instancedMaterial;
    public int instanceCount = 1000;
    public Vector3 spawnAreaSize = new Vector3(100f, 0f, 100f);
    public Vector3 spawnAreaCenter = Vector3.zero;

    private Matrix4x4[] matrices;

    void Start()
    {
        SetupMatrices();
    }

    void SetupMatrices()
    {
        int count = instanceCount;

        matrices = new Matrix4x4[count];

        for (int i = 0; i < count; i++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(spawnAreaCenter.x - spawnAreaSize.x/2, spawnAreaCenter.x + spawnAreaSize.x/2),
                spawnAreaCenter.y,
                Random.Range(spawnAreaCenter.z - spawnAreaSize.z/2, spawnAreaCenter.z + spawnAreaSize.z/2)
            );
            Quaternion rot = Quaternion.Euler(0, Random.Range(0,360f), 0);
            Vector3 scale = Vector3.one;

            matrices[i] = Matrix4x4.TRS(randomPos, rot, scale);
        }
    }

    void Update()
    {
        if (matrices != null && matrices.Length > 0)
        {
            RenderParams rp = new RenderParams(instancedMaterial)
            {
                shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                receiveShadows = true                                             
            };
            Graphics.RenderMeshInstanced(rp, mesh, 0, matrices, matrices.Length);
        }
    }
}