using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    public PlayerController climbingRig;
    public GameObject[] hearts = new GameObject[3];

    [Header("ÇÏÆ® ±ôºýÀÓ")]
    public float heartBlinkDuration = 0.8f;
    public float heartBlinkInterval = 0.1f;

    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float stunTime)
    {
        if (currentHealth <= 0) return;

        currentHealth--;
        Debug.Log($"ÇÇ°Ý! ³²Àº Ã¼·Â: {currentHealth}/{maxHealth}");

        if (climbingRig != null)
            climbingRig.Stun(stunTime);

        // ÀÒÀº ÇÏÆ® ±ôºýÀÓ ÈÄ ²ô±â
        if (currentHealth < hearts.Length && hearts[currentHealth] != null)
            StartCoroutine(BlinkThenDisable(hearts[currentHealth]));

        if (currentHealth <= 0)
            Debug.Log("»ç¸Á!");
    }

    IEnumerator BlinkThenDisable(GameObject heart)
    {
        float elapsed = 0f;
        while (elapsed < heartBlinkDuration)
        {
            heart.SetActive(!heart.activeSelf);
            yield return new WaitForSeconds(heartBlinkInterval);
            elapsed += heartBlinkInterval;
        }
        heart.SetActive(false);
    }
}