using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
public class SkillNodeUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Button skillButton;
    [SerializeField] private Image skillIcon;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject unlockedIndicator;

    [Header("Visual States")]
    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color availableColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color unlockedColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color hoverColor = new Color(1.2f, 1.2f, 1.2f, 1f);

    [Header("Animation")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private bool enableHoverEffects = true;

    [Header("Audio")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip unlockSound;

    public SkillData skillData;
    public SkillState currentState;
    private Vector3 originalScale;
    private Color originalColor;
    private AudioSource audioSource;
    private bool isInitialized = false;

    public SkillData SkillData => skillData;
    public SkillState CurrentState => currentState;

    private void Awake()
    {
        // Get or create the audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.5f;
        }

        // Set the initial scale and color
        originalScale = transform.localScale;
        if (backgroundImage != null)
        {
            originalColor = backgroundImage.color;
        }

        // Ensure button is properly initialized
        if (skillButton == null)
        {
            skillButton = GetComponent<Button>();
        }
    }

    public void Initialize(SkillData skill)
    {
        if (skill == null)
        {
            Debug.LogError("SkillNodeUI: cannot initialize skill node with null skill data");
            return;
        }

        skillData = skill;
        isInitialized = true;

        // Set basic Info
        if (skillNameText != null)
        {
            skillNameText.text = skill.skillName;
        }

        if (skillIcon != null && skill.skillIcon != null)
        {
            skillIcon.sprite = skill.skillIcon;
        }

        // Update visual state
        UpdateVisualState();

        UpdateCostDisplay();
    }

    public void UpdateVisualState()
    {
        if (!isInitialized || skillData == null) return;

        // Determine current state
        if (skillData.IsUnlocked)
        {
            currentState = SkillState.Unlocked;
        }

        else if (skillData.CanUnlock)
        {
            currentState = SkillState.Available;
        }
        else
        {
            currentState = SkillState.Locked;
        }

        ApplyStateVisuals();
    }

    private void ApplyStateVisuals()
    {
        Color targetColor;
        bool interactable = false;
        bool showLocked = false;
        bool showUnlocked = false;

        switch (currentState)
        {
            case SkillState.Locked:
                targetColor = lockedColor;
                interactable = false;
                showLocked = true;
                break;

            case SkillState.Available:
                targetColor = availableColor;
                interactable = true;
                break;

            case SkillState.Unlocked:
                targetColor = unlockedColor;
                interactable = false;
                showUnlocked = true;
                break;

            default:
                targetColor = lockedColor;
                interactable = false;
                break;
        }

        // Apply background color
        if (backgroundImage != null)
        {
            backgroundImage.color = targetColor;
        }

        // Set button interactability
        if (skillButton != null)
        {
            skillButton.interactable = interactable;
        }

        // Show/hide overlays
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(showLocked);
        }

        if (unlockedIndicator != null)
        {
            unlockedIndicator.SetActive(showUnlocked);
        }

        // Update icon transparency
        if (skillIcon != null)
        {
            Color iconColor = skillIcon.color;
            iconColor.a = currentState == SkillState.Locked ? 0.5f : 1f;
            skillIcon.color = iconColor;
        }
    }

    private void UpdateCostDisplay()
    {
        if (costText == null || skillData == null) return;

        string costString = "";

        if (skillData.skillPointCost > 0)
        {
            costString = $"{skillData.skillPointCost} SP";
        }

        if (string.IsNullOrEmpty(costString))
        {
            Debug.LogError("SkillNodeUI: skill point cost is null or 0");
        }

        costText.text = costString;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInitialized || skillData == null) return;

        if (currentState == SkillState.Available)
        {
            PlaySound(clickSound);

            if (SkillManager.Instance != null)
            {
                bool success = SkillManager.Instance.UnlockSkill(skillData.skillId);
                if (success)
                {
                    PlaySound(unlockSound);
                }
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!enableHoverEffects || !isInitialized) return;

        PlaySound(hoverSound);

        // Scale effect
        if (currentState == SkillState.Available)
        {
            LeanTween.scale(gameObject, originalScale * hoverScale, animationDuration).setEaseOutBack();
        }

        // color effect
        if (backgroundImage != null && currentState == SkillState.Available)
        {
            backgroundImage.color = hoverColor;
        }

        // Show tooltip if available
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!enableHoverEffects || !isInitialized) return;

        // Reset scale
        LeanTween.scale(gameObject, originalScale, animationDuration).setEaseOutBack();

        // Reset color
        if (backgroundImage != null)
        {
            Color targetColor = currentState switch
            {
                SkillState.Locked => lockedColor,
                SkillState.Available => availableColor,
                SkillState.Unlocked => unlockedColor,
                _ => lockedColor
            };
            backgroundImage.color = targetColor;
        }

        // Hide tooltip
        HideTooltip();
    }


    private void ShowTooltip()
    {
        if (skillData == null) return;

        // TODO: implement tooltip system
        Debug.Log($"Skill: {skillData.skillName} is available");
    }

    private void HideTooltip()
    {
        // TODO: implement tooltip system
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void SetHighLighted(bool highlighted)
    {
        if (!isInitialized) return;

        float targetScale = highlighted ? hoverScale : 1f;
        LeanTween.scale(gameObject, originalScale * targetScale, animationDuration);
    }

    public bool CanInteract()
    {
        return isInitialized && currentState == SkillState.Available;
    }

    [ContextMenu("Force Update Visual State")]
    private void ForceUpdateVisualState()
    {
        UpdateVisualState();
    }

    [ContextMenu("Test Unlock Animation")]
    private void TestUnlockAnimation()
    {
        if (skillData != null)
        {
            skillData.SetUnlocked(true);
            UpdateVisualState();
            PlaySound(unlockSound);
        }
    }
}

public enum SkillState
{
    Locked,     // Prerequisites not met or insufficient resources
    Available,  // Can be unlocked
    Unlocked    // Already unlocked
}
