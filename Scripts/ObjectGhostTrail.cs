using UnityEngine;

/// <summary>
/// Genera copias semi-transparentes ("fantasmas") del objeto a medida que se mueve,
/// simulando el efecto de papel mantequilla / onion skin usado en animacion.
/// Requiere un material que use el shader "Custom/GhostTrail".
/// </summary>
public class ObjectGhostTrail : MonoBehaviour
{
    [Header("Fuente")]
    [Tooltip("Si lo dejas vacio, se auto-detectan los Renderers en este objeto y sus hijos")]
    [SerializeField] private Renderer[] sourceRenderers;
    [Tooltip("Material que use el shader Custom/GhostTrail")]
    [SerializeField] private Material ghostMaterial;

    [Header("Config del rastro")]
    [SerializeField] private float spawnInterval = 0.08f;
    [SerializeField] private float ghostLifetime = 0.6f;
    [SerializeField] private float startAlpha = 0.5f;
    [SerializeField] private Color ghostColor = new Color(0.3f, 0.7f, 1f, 1f);
    [Tooltip("Evita generar fantasmas si el objeto esta quieto")]
    [SerializeField] private float minDistanceToSpawn = 0.05f;

    private float _timer;
    private Vector3 _lastSpawnPos;

    private void Awake()
    {
        if (sourceRenderers == null || sourceRenderers.Length == 0)
            sourceRenderers = GetComponentsInChildren<Renderer>();

        _lastSpawnPos = transform.position;

        if (ghostMaterial == null)
            Debug.LogError("[ObjectGhostTrail] Falta asignar el Ghost Material en el inspector.");
    }

    private void Update()
    {
        if (ghostMaterial == null) return;

        _timer += Time.deltaTime;
        float movedDistance = Vector3.Distance(transform.position, _lastSpawnPos);

        if (_timer >= spawnInterval && movedDistance >= minDistanceToSpawn)
        {
            SpawnGhost();
            _timer = 0f;
            _lastSpawnPos = transform.position;
        }
    }

    private void SpawnGhost()
    {
        foreach (var sourceRenderer in sourceRenderers)
        {
            Mesh snapshotMesh = GetSnapshotMesh(sourceRenderer);
            if (snapshotMesh == null) continue;

            GameObject ghost = new GameObject("Ghost");
            ghost.transform.SetPositionAndRotation(sourceRenderer.transform.position, sourceRenderer.transform.rotation);
            ghost.transform.localScale = sourceRenderer.transform.lossyScale;

            var meshFilter = ghost.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = snapshotMesh;

            var meshRenderer = ghost.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = ghostMaterial;

            var fader = ghost.AddComponent<GhostFader>();
            fader.Init(ghostLifetime, ghostColor, startAlpha);
        }
    }

    private Mesh GetSnapshotMesh(Renderer sourceRenderer)
    {
        // Personajes / objetos con animacion de esqueleto: hay que "hornear" la pose actual
        if (sourceRenderer is SkinnedMeshRenderer skinned)
        {
            Mesh baked = new Mesh();
            skinned.BakeMesh(baked);
            return baked;
        }

        // Objetos estaticos con MeshFilter comun
        if (sourceRenderer is MeshRenderer)
        {
            var meshFilter = sourceRenderer.GetComponent<MeshFilter>();
            return meshFilter != null ? meshFilter.sharedMesh : null;
        }

        return null;
    }
}
