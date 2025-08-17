using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GoldDisplayUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Image coinIcon;

    [Header("Animation Settings")]
    [SerializeField] private bool animateOnChange = true;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private Color flashColorGoldEarned = Color.green;
    [SerializeField] private Color flashColorGoldSpent = Color.red;
    [SerializeField] private float goldEarnedScale = 1.2f;


    private Color originalTextColor;
    private Vector3 originalScale;

    void Awake()
    {
        // auto find components if not assigned
        if (goldText == null)
        {
            goldText = GetComponentInChildren<TextMeshProUGUI>();
        }
        if (coinIcon == null)
        {
            coinIcon = GetComponentInChildren<Image>();
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Store original values for animations
        if (goldText != null)
        {
            originalTextColor = goldText.color;
        }
        if (coinIcon != null)
        {
            originalScale = coinIcon.transform.localScale;

        }

        // Subscribe to EconomyManager events
        EconomyManager.OnGoldChanged += UpdateGoldDisplay;
        EconomyManager.OnGoldSpent += OnGoldSpent;
        EconomyManager.OnGoldEarned += OnGoldEarned;

        UpdateGoldDisplay(EconomyManager.Instance?.CurrentGold ?? 0);

    }

    void OnDestroy()
    {
        // Unsubscribe from EconomyManager events
        EconomyManager.OnGoldChanged -= UpdateGoldDisplay;
        EconomyManager.OnGoldSpent -= OnGoldSpent;
        EconomyManager.OnGoldEarned -= OnGoldEarned;

    }

    private void UpdateGoldDisplay(int goldAmount)
    {
        if (goldText != null)
        {
            goldText.text = goldAmount.ToString();
        }
    }

    private void OnGoldEarned(int amount, int newTotal)
    {

        UpdateGoldDisplay(newTotal);

        if (animateOnChange)
        {
            StartCoroutine(AnimateGoldGain());
        }
    }

    private void OnGoldSpent(int amountSpent, int remaining)
    {

        UpdateGoldDisplay(remaining);

        if (animateOnChange)
        {
            StartCoroutine(AnimateGoldSpent());
        }
    }

    private System.Collections.IEnumerator AnimateGoldGain()
    {
        if (goldText != null)
        {
            goldText.color = flashColorGoldEarned;
        }

        if (coinIcon != null)
        {
            coinIcon.transform.localScale = originalScale * goldEarnedScale;
        }

        yield return new WaitForSeconds(animationDuration);

        // reset to original color
        if (goldText != null)
        {
            goldText.color = originalTextColor;
        }
        if (coinIcon != null)
        {
            coinIcon.transform.localScale = originalScale;
        }
    }

    private System.Collections.IEnumerator AnimateGoldSpent()
    {
        // flash red briefly
        if (goldText != null)
        {
            goldText.color = flashColorGoldSpent;
        }

        yield return new WaitForSeconds(animationDuration);

        // reset to original color
        if (goldText != null)
        {
            goldText.color = originalTextColor;
        }


    }

    // Update is called once per frame
    void Update()
    {

    }
}
