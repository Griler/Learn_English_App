using System;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] TextMeshProUGUI textInputField;
    public Action onHanldeSumit;
    private void Start()
    {
        Instance = this;
        if(inputField)
            inputField.text = "";
    }

    public void setInputField(TMP_InputField inputField)
    {
        this.inputField = inputField;
    }

    public void DeleteLetter()
    {
        if(inputField.text.Length != 0) {
            inputField.text = inputField.text.Remove(inputField.text.Length - 1, 1);
        }

        if (textInputField && textInputField.text.Length != 0)
        {
            textInputField.text = textInputField.text.Remove(textInputField.text.Length - 1, 1);
        }

    }

    public void AddLetter(string letter)
    {
        if(inputField)
            inputField.text = inputField.text + letter;
        if (textInputField)
            textInputField.text = textInputField.text + letter;
    }

    public void SubmitWord()
    {
        if(inputField)
            inputField.text = inputField.text;  
        if(textInputField)
            textInputField.text = textInputField.text;
        // Debug.Log("Text submitted successfully!");
    }
}
