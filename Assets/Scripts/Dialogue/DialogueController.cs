using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueController : MonoBehaviour
{
    [SerializeField] private GameObject dialogueRoot;

    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private MemberAssembly memberAssembly;

    [Header("Dialogue")]
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("I Choice")]
    [SerializeField] private Button defaultChoiceButton;
    [SerializeField] private TMP_Text defaultChoiceText;

    [Header("Member Choice")]
    [SerializeField] private Button memberChoiceButton;
    [SerializeField] private TMP_Text memberChoiceText;

    [Header("Continue")]
    [SerializeField] private Button continueButton;

    private DialogueNPC currentNpc;
    private bool isOpen;
    private bool waitingForContinue;

    private bool movementWasEnabled;
    private bool lookWasEnabled;
    private bool interactionWasEnabled;

    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;
    private bool memberChoiceSelected;

    private void Awake()
    {
        defaultChoiceButton.onClick.AddListener(SelectDefaultChoice);
        memberChoiceButton.onClick.AddListener(SelectMemberChoice);
        continueButton.onClick.AddListener(CompleteDialogue);

        dialogueRoot.SetActive(false);
    }

    private void Update()
    {
        if (!isOpen || Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseDialogue();
            return;
        }

        if (waitingForContinue)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                CompleteDialogue();
            }

            return;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            SelectDefaultChoice();
            return;
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame &&
            memberChoiceButton.gameObject.activeSelf)
        {
            SelectMemberChoice();
        }
    }

    public void OpenDialogue(DialogueNPC npc)
    {
        if (isOpen || npc == null)
            return;

        currentNpc = npc;
        isOpen = true;
        waitingForContinue = false;
        memberChoiceSelected = false;

        movementWasEnabled = playerMovement.enabled;
        lookWasEnabled = playerLook.enabled;
        interactionWasEnabled = playerInteraction.enabled;

        previousCursorLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        playerMovement.enabled = false;
        playerLook.enabled = false;
        playerInteraction.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        dialogueRoot.SetActive(true);

        ShowOpeningState();
    }

    private void ShowOpeningState()
    {
        speakerNameText.text =
            currentNpc.SpeakerName;

        dialogueText.text =
            currentNpc.OpeningLine;

        defaultChoiceText.text =
            $"[1] I // {currentNpc.DefaultChoice}";

        bool hasRequiredMember =
            memberAssembly.HasMember(
                currentNpc.RequiredMember
            );

        memberChoiceButton.gameObject.SetActive(
            hasRequiredMember
        );

        if (hasRequiredMember)
        {
            MemberDefinition member =
                currentNpc.RequiredMember;

            memberChoiceText.text =
                $"[2] {member.Code} // {currentNpc.MemberChoice}";
        }

        defaultChoiceButton.gameObject.SetActive(true);
        continueButton.gameObject.SetActive(false);
    }

    private void SelectDefaultChoice()
    {
        if (!isOpen || waitingForContinue)
            return;

        memberChoiceSelected = false;

        ShowResponse(
            currentNpc.DefaultResponse
        );
    }

    private void SelectMemberChoice()
    {
        if (!isOpen || waitingForContinue)
            return;

        if (!memberAssembly.HasMember(
                currentNpc.RequiredMember))
        {
            return;
        }

        memberChoiceSelected = true;

        ShowResponse(
            currentNpc.MemberResponse
        );
    }

    private void ShowResponse(string response)
    {
        waitingForContinue = true;

        dialogueText.text = response;

        defaultChoiceButton.gameObject.SetActive(false);
        memberChoiceButton.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(true);
    }

    private void CloseDialogue()
    {
        if (!isOpen)
            return;

        isOpen = false;
        waitingForContinue = false;
        currentNpc = null;

        dialogueRoot.SetActive(false);

        playerMovement.enabled = movementWasEnabled;
        playerLook.enabled = lookWasEnabled;
        playerInteraction.enabled = interactionWasEnabled;

        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;
    }

    private void CompleteDialogue()
    {
        if (!isOpen || !waitingForContinue)
            return;

        if (memberChoiceSelected)
            currentNpc.CompleteMemberDialogue();

        CloseDialogue();
    }
}