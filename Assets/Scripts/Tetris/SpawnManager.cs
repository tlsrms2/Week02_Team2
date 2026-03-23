using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class SpawnManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform background;

    [Header("Spawn Settings")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool hideTargetObjectsDuringPlay = true;
    [SerializeField] private float spawnInterval = 0.2f;
    [SerializeField] private float fallSpeed = 18f;
    [SerializeField] private float spawnHeightOffset = 2f;
    [SerializeField] private float spawnedZPosition = -1f;

    private readonly List<Transform> targetSequence = new List<Transform>();
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();
    private Coroutine spawnRoutine;

    public List<GameObject> startHolder;
    private void Reset()
    {
        GameObject backgroundObject = GameObject.Find("Background");

        if (backgroundObject != null)
        {
            background = backgroundObject.transform;
        }
    }

    private void OnValidate()
    {
        spawnInterval = Mathf.Max(0f, spawnInterval);
        fallSpeed = Mathf.Max(0.01f, fallSpeed);
        spawnHeightOffset = Mathf.Max(0f, spawnHeightOffset);
    }

    private void Awake()
    {
        CacheTargets();

        if (Application.isPlaying && hideTargetObjectsDuringPlay)
        {
            SetTargetsVisible(false);
        }
    }

    private void Start()
    {
        if (!Application.isPlaying || !playOnStart)
        {
            return;
        }


    }

    public void StartGame()
    {
        spawnRoutine = StartCoroutine(SpawnSequence());
        foreach (var v in startHolder)
        {
            v.SetActive(true);
        }
    }

    public void ReStartGame()
    {
        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
        spawnRoutine = StartCoroutine(SpawnSequence());
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        if (Application.isPlaying)
        {
            CleanupSpawnedObjects();
            SetTargetsVisible(true);
        }
    }

    private IEnumerator SpawnSequence()
    {
        CleanupSpawnedObjects();

        foreach (Transform target in targetSequence)
        {
            if (target == null)
            {
                continue;
            }

            Vector3 targetPosition = target.position;
            targetPosition.z = spawnedZPosition;

            Vector3 spawnPosition = new Vector3(
                targetPosition.x,
                GetSpawnY(targetPosition.y),
                spawnedZPosition);

            GameObject spawnedObject = Instantiate(target.gameObject, spawnPosition, target.rotation, transform);
            spawnedObject.name = $"{target.name}_Runtime";
            spawnedObject.SetActive(true);
            spawnedObjects.Add(spawnedObject);

            yield return MoveToTarget(spawnedObject.transform, targetPosition);

            if (spawnInterval > 0f)
            {
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        spawnRoutine = null;
    }

    private IEnumerator MoveToTarget(Transform movingTransform, Vector3 targetPosition)
    {
        while (movingTransform != null &&
               (movingTransform.position - targetPosition).sqrMagnitude > 0.0001f)
        {
            movingTransform.position = Vector3.MoveTowards(
                movingTransform.position,
                targetPosition,
                fallSpeed * Time.deltaTime);

            yield return null;
        }

        if (movingTransform != null)
        {
            movingTransform.position = targetPosition;
        }
    }

    private void CacheTargets()
    {
        targetSequence.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            targetSequence.Add(transform.GetChild(i));
        }
    }

    private void SetTargetsVisible(bool visible)
    {
        foreach (Transform target in targetSequence)
        {
            if (target != null)
            {
                target.gameObject.SetActive(visible);
            }
        }
    }

    private void CleanupSpawnedObjects()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            GameObject spawnedObject = spawnedObjects[i];

            if (spawnedObject == null)
            {
                continue;
            }

            Destroy(spawnedObject);
        }

        spawnedObjects.Clear();
    }

    private float GetSpawnY(float targetY)
    {
        float backgroundTopY = targetY + spawnHeightOffset;

        if (background != null)
        {
            if (background.TryGetComponent(out Renderer renderer))
            {
                backgroundTopY = renderer.bounds.max.y + spawnHeightOffset;
            }
            else if (background.TryGetComponent(out Collider collider))
            {
                backgroundTopY = collider.bounds.max.y + spawnHeightOffset;
            }
            else
            {
                backgroundTopY = background.position.y + spawnHeightOffset;
            }
        }

        return Mathf.Max(backgroundTopY, targetY + spawnHeightOffset);
    }
}
