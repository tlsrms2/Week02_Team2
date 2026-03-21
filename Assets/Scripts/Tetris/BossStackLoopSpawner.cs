using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class BossStackLoopSpawner : MonoBehaviour
{
    [Serializable]
    private sealed class SpawnSettings
    {
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private Vector3 startPosition = new Vector3(0f, -0.4f, -5f);
        [SerializeField] private float yStep = 1.4f;
        [SerializeField] private float spawnInterval = 0.5f;

        public bool PlayOnStart => playOnStart;
        public Vector3 StartPosition => startPosition;
        public float YStep => Mathf.Max(0.01f, yStep);
        public float SpawnInterval => Mathf.Max(0f, spawnInterval);
    }

    private sealed class BossSequence
    {
        private readonly IReadOnlyList<GameObject> templates;
        private int nextIndex;

        public BossSequence(IReadOnlyList<GameObject> templates)
        {
            this.templates = templates;
            nextIndex = 0;
        }

        public bool IsValid => templates != null && templates.Count == 10;

        public GameObject GetNext()
        {
            if (!IsValid)
            {
                return null;
            }

            GameObject template = templates[nextIndex];
            nextIndex = (nextIndex + 1) % templates.Count;
            return template;
        }
    }

    [Header("Spawn Settings")]
    [SerializeField] private SpawnSettings spawnSettings = new SpawnSettings();

    private readonly List<GameObject> spawnedBosses = new List<GameObject>();
    private Coroutine spawnRoutine;
    private BossSequence bossSequence;

    private void Awake()
    {
        bossSequence = new BossSequence(FindBossTemplates());
    }

    private void OnValidate()
    {
        if (spawnSettings == null)
        {
            spawnSettings = new SpawnSettings();
        }
    }

    private void Start()
    {
        if (!Application.isPlaying || !spawnSettings.PlayOnStart)
        {
            return;
        }

        if (bossSequence == null || !bossSequence.IsValid)
        {
            Debug.LogError($"{nameof(BossStackLoopSpawner)} could not find BOSS1~BOSS10 templates in the scene.", this);
            enabled = false;
            return;
        }

        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnNextBoss();

            if (spawnSettings.SpawnInterval > 0f)
            {
                yield return new WaitForSeconds(spawnSettings.SpawnInterval);
            }
            else
            {
                yield return null;
            }
        }
    }

    private void SpawnNextBoss()
    {
        GameObject template = bossSequence.GetNext();

        if (template == null)
        {
            return;
        }

        Vector3 spawnPosition = spawnSettings.StartPosition + Vector3.up * (spawnedBosses.Count * spawnSettings.YStep);
        GameObject spawnedBoss = Instantiate(template, spawnPosition, template.transform.rotation, transform);
        spawnedBoss.name = $"{template.name}_Stack_{spawnedBosses.Count + 1}";
        spawnedBoss.SetActive(true);
        spawnedBosses.Add(spawnedBoss);
    }

    private List<GameObject> FindBossTemplates()
    {
        Dictionary<int, GameObject> templatesByIndex = new Dictionary<int, GameObject>();
        GameObject[] sceneObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject sceneObject in sceneObjects)
        {
            if (sceneObject == null || !sceneObject.scene.IsValid() || sceneObject.scene != gameObject.scene)
            {
                continue;
            }

            if (!TryGetBossIndex(sceneObject.name, out int bossIndex))
            {
                continue;
            }

            if (!templatesByIndex.ContainsKey(bossIndex))
            {
                templatesByIndex.Add(bossIndex, sceneObject);
            }
        }

        List<GameObject> orderedTemplates = new List<GameObject>();

        for (int bossIndex = 1; bossIndex <= 10; bossIndex++)
        {
            if (templatesByIndex.TryGetValue(bossIndex, out GameObject template))
            {
                orderedTemplates.Add(template);
            }
        }

        if (orderedTemplates.Count != 10)
        {
            Debug.LogWarning($"{nameof(BossStackLoopSpawner)} expected BOSS1~BOSS10, but found {orderedTemplates.Count} templates.", this);
        }

        return orderedTemplates;
    }

    private static bool TryGetBossIndex(string objectName, out int bossIndex)
    {
        bossIndex = 0;

        if (string.IsNullOrWhiteSpace(objectName) || !objectName.StartsWith("BOSS", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return int.TryParse(objectName.Substring(4), out bossIndex) && bossIndex >= 1 && bossIndex <= 10;
    }
}
