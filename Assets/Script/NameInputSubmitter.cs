using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

[RequireComponent(typeof(TMP_InputField))] // Auto-adds InputField if missing
public class NameInputSubmitter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private GameObject nextPanel;
    
    [Header("Validation")]
    [Tooltip("Minimum characters required")]
    [SerializeField] private int minNameLength = 2;

    private void Start()
    {
        if (inputField == null) inputField = GetComponent<TMP_InputField>();
        FocusInputField();
        
        inputField.onSubmit.AddListener(HandleSubmit);
    }

    private void FocusInputField()
    {
        inputField.Select();
        inputField.ActivateInputField();
        TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default); // Mobile support
    }

    public void HandleSubmit(string text)
    {
        if (!IsValidName(text)) return;
        
        PlayerPrefs.SetString("PlayerName", text.Trim());
        TransitionToNextPanel();
    }

    private bool IsValidName(string text)
    {
        if (text.Length >= minNameLength) return true;
        
        // Visual feedback for invalid input
        inputField.text = "";
        inputField.placeholder.GetComponent<TMP_Text>().text = $"Name too short! ({minNameLength}+ chars)";
        FocusInputField();
        return false;
    }

    private void TransitionToNextPanel()
    {
        if (nextPanel == null) return;
        
        nextPanel.SetActive(true);
        gameObject.SetActive(false);
        
        // Play confirmation sound
        AudioManager.Instance?.PlayButtonClickSound(); 
    }
}