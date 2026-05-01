using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnemyWaveManager : MonoBehaviour
{
    [Header("enemies")]
    [SerializeField] private GameObject normalEnemyPrefab;
    [SerializeField] private GameObject specialEnemyPrefab;

    [Header("boss")]
    [SerializeField] private GameObject bossEnemyPrefab;



    [Header("fx")]
    [SerializeField] private GameObject spawnParticlePrefab;
    [SerializeField] private float particleDestroyTime = 2f;



    [Header("ui")]
    [SerializeField] private TextMeshProUGUI waveText;



    [Header("intro")]
    [SerializeField] private GameObject startScreen;
    [SerializeField] private float firstWaveDelay = 1f;



    [Header("settings")]
    [SerializeField] private float timeBetweenWaves = 2f;



    [Header("death settings")]
    [SerializeField] private float deathDelay = 2f;



    // runtime tracking
    private List<GameObject> aliveEnemies = new List<GameObject>();

    private bool playerDied;
    private PlayerStats playerStats;

    private Coroutine deathRoutine;



    // wave spawn sets

    [Header("wave 1 spawns")]
    [SerializeField] private Transform[] wave1NormalSpawns;
    [SerializeField] private Transform[] wave1SpecialSpawns;

    [Header("wave 2 spawns")]
    [SerializeField] private Transform[] wave2NormalSpawns;
    [SerializeField] private Transform[] wave2SpecialSpawns;

    [Header("wave 3 spawns")]
    [SerializeField] private Transform[] wave3NormalSpawns;
    [SerializeField] private Transform[] wave3SpecialSpawns;



    [Header("boss wave spawns")]
    [SerializeField] private Transform[] bossWaveNormalSpawns;
    [SerializeField] private Transform[] bossWaveSpecialSpawns;

    [Header("boss wave 1 spawns")]
    [SerializeField] private Transform[] wave1BossSpawns;

    [Header("boss wave 2 spawns")]
    [SerializeField] private Transform[] wave2BossSpawns;

    [Header("boss wave 3 spawns")]
    [SerializeField] private Transform[] wave3BossSpawns;

    [Header("boss final wave spawns")]
    [SerializeField] private Transform[] bossWaveBossSpawns;



    private void Start()
    {
        if (waveText != null)
        {
            waveText.gameObject.SetActive(false);
        }

        playerStats = FindFirstObjectByType<PlayerStats>();
    }



    public void OnPlayerDied()
    {
        playerDied = true;
    }



    public void BeginWaves()
    {
        if (startScreen != null)
        {
            // startScreen.SetActive(false);
        }

        StartCoroutine(WaveLoop());
    }



    // handles external death trigger (during waves)
    private bool CheckDeathEarly()
    {
        if (!playerDied)
        {
            return false;
        }

        if (deathRoutine == null)
        {
            deathRoutine = StartCoroutine(HandleDeathSequence());
        }

        return true;
    }



    private IEnumerator HandleDeathSequence()
    {
        yield return StartCoroutine(ShowWaveText("You Died"));

        yield return new WaitForSeconds(deathDelay);

        ClearAllEnemies();

        if (playerStats != null)
        {
            playerStats.Revive();
            playerDied = false;
        }

        CameraIntroSequence intro = FindFirstObjectByType<CameraIntroSequence>();

        if (intro != null)
        {
            intro.ReturnToIntro();
        }

        playerDied = false;
        deathRoutine = null;
    }



    private IEnumerator WaveLoop()
    {
        yield return new WaitForSeconds(firstWaveDelay);

        yield return StartCoroutine(ShowWaveText("Wave 1"));

        SpawnWave(wave1NormalSpawns, wave1SpecialSpawns, wave1BossSpawns);
        yield return StartCoroutine(WaitForWaveClear());

        if (playerDied)
        {
            yield return StartCoroutine(ShowWaveText("You Died"));

            CameraIntroSequence intro = FindFirstObjectByType<CameraIntroSequence>();

            if (intro != null)
            {
                intro.ReturnToIntro();
            }

            ClearAllEnemies();
            playerStats.Revive();
            playerDied = false;

            yield break;
        }



        yield return StartCoroutine(ShowWaveText("Wave 2"));

        SpawnWave(wave2NormalSpawns, wave2SpecialSpawns, wave2BossSpawns);
        yield return StartCoroutine(WaitForWaveClear());

        if (playerDied)
        {
            yield return StartCoroutine(ShowWaveText("You Died"));

            CameraIntroSequence intro = FindFirstObjectByType<CameraIntroSequence>();

            if (intro != null)
            {
                intro.ReturnToIntro();
            }

            ClearAllEnemies();
            playerStats.Revive();
            playerDied = false;

            yield break;
        }



        yield return StartCoroutine(ShowWaveText("Wave 3"));

        SpawnWave(wave3NormalSpawns, wave3SpecialSpawns, wave3BossSpawns);
        yield return StartCoroutine(WaitForWaveClear());

        if (playerDied)
        {
            yield return StartCoroutine(ShowWaveText("You Died"));

            CameraIntroSequence intro = FindFirstObjectByType<CameraIntroSequence>();

            if (intro != null)
            {
                intro.ReturnToIntro();
            }

            ClearAllEnemies();
            playerStats.Revive();
            playerDied = false;

            yield break;
        }



        yield return StartCoroutine(ShowWaveText("Boss Battle"));

        SpawnWave(bossWaveNormalSpawns, bossWaveSpecialSpawns, bossWaveBossSpawns);
        yield return StartCoroutine(WaitForWaveClear());

        if (playerDied)
        {
            yield return StartCoroutine(ShowWaveText("You Died"));

            CameraIntroSequence intro = FindFirstObjectByType<CameraIntroSequence>();

            if (intro != null)
            {
                intro.ReturnToIntro();
            }

            ClearAllEnemies();
            playerStats.Revive();
            playerDied = false;

            yield break;
        }



        yield return StartCoroutine(ShowWaveText("You Win"));

        ClearAllEnemies();

        if (playerStats != null)
        {
            playerStats.Revive();
        }

        CameraIntroSequence finalIntro = FindFirstObjectByType<CameraIntroSequence>();

        if (finalIntro != null)
        {
            finalIntro.ReturnToIntro();
        }
    }



    private void SpawnWave(Transform[] normalSpawns, Transform[] specialSpawns, Transform[] bossSpawns = null)
    {
        for (int i = 0; i < normalSpawns.Length; i++)
        {
            if (normalSpawns[i] == null)
            {
                continue;
            }

            SpawnEnemy(normalEnemyPrefab, normalSpawns[i]);
        }

        for (int i = 0; i < specialSpawns.Length; i++)
        {
            if (specialSpawns[i] == null)
            {
                continue;
            }

            SpawnEnemy(specialEnemyPrefab, specialSpawns[i]);
        }

        if (bossSpawns != null)
        {
            for (int i = 0; i < bossSpawns.Length; i++)
            {
                if (bossSpawns[i] == null)
                {
                    continue;
                }

                SpawnEnemy(bossEnemyPrefab, bossSpawns[i]);
            }
        }
    }



    private void SpawnEnemy(GameObject prefab, Transform spawnPoint)
    {
        if (prefab == null)
        {
            Debug.Log("enemy prefab missing");
            return;
        }

        if (spawnPoint == null)
        {
            return;
        }

        if (spawnParticlePrefab != null)
        {
            GameObject fx = Instantiate(
                spawnParticlePrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            Destroy(fx, particleDestroyTime);
        }

        GameObject enemy = Instantiate(
            prefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        aliveEnemies.Add(enemy);
    }



    private IEnumerator WaitForWaveClear()
    {
        while (true)
        {
            if (playerDied)
            {
                yield break;
            }

            aliveEnemies.RemoveAll(item => item == null);

            if (aliveEnemies.Count == 0)
            {
                break;
            }

            yield return null;
        }

        yield return new WaitForSeconds(timeBetweenWaves);
    }



    private IEnumerator ShowWaveText(string message)
    {
        if (waveText == null)
        {
            yield break;
        }

        waveText.text = message;
        waveText.gameObject.SetActive(true);

        RectTransform rect = waveText.GetComponent<RectTransform>();

        rect.localScale = Vector3.zero;

        Vector3 originalScale = Vector3.one;
        Vector3 bounceScale = Vector3.one * 1.35f;



        float t = 0f;

        while (t < 0.15f)
        {
            t += Time.deltaTime;
            rect.localScale = Vector3.Lerp(Vector3.zero, bounceScale, t / 0.15f);
            yield return null;
        }



        t = 0f;

        while (t < 0.12f)
        {
            t += Time.deltaTime;
            rect.localScale = Vector3.Lerp(bounceScale, originalScale, t / 0.12f);
            yield return null;
        }

        rect.localScale = originalScale;

        yield return new WaitForSeconds(1.7f);

        waveText.gameObject.SetActive(false);
    }



    private void ClearAllEnemies()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (aliveEnemies[i] != null)
            {
                Destroy(aliveEnemies[i]);
            }
        }

        aliveEnemies.Clear();
    }
}