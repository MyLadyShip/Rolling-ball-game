using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; // For cross-platform input

public class NameInputSubmitter : MonoBehaviour
{
    public TMP_InputField inputField;
    public GameObject nextPanel;
    public float minNameLength = 2;

    private void Start()
    {
        inputField.Select();
        inputField.ActivateInputField();
        inputField.onSubmit.AddListener(HandleSubmit); // Changed from onEndEdit
    }

    private void HandleSubmit(string text)
    {
        // Validate name length
        if (text.Length < minNameLength)
        {
            inputField.ActivateInputField(); // Keep focus if invalid
            return;
        }

        // Save using new Input System's keyboard check (works on mobile too)
        if (Keyboard.current.enterKey.wasPressedThisFrame || 
            Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            PlayerPrefs.SetString("PlayerName", text.Trim());
            
            if (nextPanel != null)
            {
                nextPanel.SetActive(true);
                gameObject.SetActive(false);
            }
        }
    }
}