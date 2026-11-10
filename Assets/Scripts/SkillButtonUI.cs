using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// On the HUDCanvas SkillButton. Three jobs, all UI-only:
//  - finger DOWN raises GameEvents.SkillPressed (the canvas never references the Player, rule 1),
//  - shows / hides itself for the current class (no skill = hidden),
//  - draws the cooldown: a dark radial overlay that unwinds until the skill is ready.
// Subscribes in OnEnable, initial pull in Start (README rule).
public class SkillButtonUI : MonoBehaviour, IPointerDownHandler
{
    [Tooltip("Child Image, Image Type = Filled / Radial 360. Fill 1 = full cooldown, 0 = ready.")]
    [SerializeField] private Image cooldownOverlay;

    [Tooltip("Label; shows the skill's displayName (or SKILL).")]
    [SerializeField] private TMP_Text label;

    [Tooltip("Hides the whole button (alpha + raycasts) for classes without a skill.")]
    [SerializeField] private CanvasGroup group;

    private float cooldownEnd;
    private float cooldownLength;
    private bool hasSkill;

    void OnEnable()
    {
        GameEvents.OnPlayerClassChanged += OnClassChanged;
        GameEvents.OnSkillUsed += OnSkillUsed;
    }

    void OnDisable()
    {
        GameEvents.OnPlayerClassChanged -= OnClassChanged;
        GameEvents.OnSkillUsed -= OnSkillUsed;
    }

    void Start()
    {
        // Initial pull, in case the class was set before this button started listening.
        PlayerRefs player = GameManager.Instance != null ? GameManager.Instance.Player : null;
        PlayerClass pc = player != null ? player.Class : null;
        OnClassChanged(pc != null ? pc.Definition : null);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (hasSkill) GameEvents.SkillPressed();
    }

    private void OnClassChanged(CharacterClassDefinition c)
    {
        AbilityDefinition skill = c != null ? c.skill : null;
        hasSkill = skill != null;

        if (group != null)
        {
            group.alpha = hasSkill ? 1f : 0f;
            group.blocksRaycasts = hasSkill;
            group.interactable = hasSkill;
        }
        if (label != null)
            label.text = hasSkill && !string.IsNullOrEmpty(skill.displayName) ? skill.displayName.ToUpper() : "SKILL";

        // New class = new skill, ready immediately (PlayerAttack resets its timer too).
        cooldownEnd = 0f;
        cooldownLength = 0f;
        if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
    }

    private void OnSkillUsed(float cooldown)
    {
        cooldownLength = cooldown;
        cooldownEnd = Time.time + cooldown;
    }

    void Update()
    {
        if (cooldownOverlay == null || cooldownLength <= 0f) return;

        float remaining = cooldownEnd - Time.time;
        cooldownOverlay.fillAmount = Mathf.Clamp01(remaining / cooldownLength);
        if (remaining <= 0f) cooldownLength = 0f;   // done, stop updating
    }
}
