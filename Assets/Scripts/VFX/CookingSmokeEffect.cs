using System.Collections;
using UnityEngine;

public class CookingSmokeEffect : MonoBehaviour
{
    [Header("Smoke Sprites (6 frames)")]
    [SerializeField] private Sprite[] smokeFrames;

    [Header("Settings")]
    [SerializeField] private float frameRate = 0.15f;
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private int maxSmokes = 3;
    [SerializeField] private float smokeScale = 2f;
    [SerializeField] private Vector2 offset = new Vector2(0f, 0.4f);
    [SerializeField] private float randomSpread = 0.15f;

    private bool isPlaying;
    private Coroutine spawnCoroutine;

    public void Play()
    {
        if (isPlaying) return;
        Debug.Log($"[CookingSmoke] Play on {gameObject.name}, frames: {smokeFrames?.Length}");
        isPlaying = true;
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public void Stop()
    {
        isPlaying = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (isPlaying)
        {
            SpawnSmoke();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnSmoke()
    {
        if (smokeFrames == null || smokeFrames.Length == 0) return;

        Vector3 pos = transform.position + (Vector3)offset;
        pos.x += Random.Range(-randomSpread, randomSpread);

        GameObject smokeObj = new GameObject("CookingSmoke");
        smokeObj.transform.position = pos;
        smokeObj.transform.localScale = Vector3.one * smokeScale;

        SpriteRenderer sr = smokeObj.AddComponent<SpriteRenderer>();
        sr.sprite = smokeFrames[0];
        sr.sortingLayerName = "Main";
        sr.sortingOrder = 10;

        StartCoroutine(AnimateSmoke(smokeObj, sr));
    }

    private IEnumerator AnimateSmoke(GameObject smokeObj, SpriteRenderer sr)
    {
        Vector3 startPos = smokeObj.transform.position;

        // Jouer chaque frame une par une
        for (int i = 0; i < smokeFrames.Length; i++)
        {
            sr.sprite = smokeFrames[i];

            float progress = (float)i / smokeFrames.Length;

            // Monter doucement
            smokeObj.transform.position = startPos + Vector3.up * (progress * 0.3f);

            // Fade out sur la deuxième moitié
            Color c = sr.color;
            c.a = 1f - (progress * 0.8f);
            sr.color = c;

            yield return new WaitForSeconds(frameRate);
        }

        Destroy(smokeObj);
    }

    private void OnDestroy()
    {
        Stop();
    }
}
