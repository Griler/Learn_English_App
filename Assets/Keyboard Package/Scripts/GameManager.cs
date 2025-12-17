using UnityEngine;
using TMPro;
using Unity.Android.Gradle.Manifest;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    [SerializeField] TMP_InputField inputField;
    public Action onHanldeSumit;
    private void Start()
    {
        Instance = this;
        inputField.text = "";
     
    }

    public void DeleteLetter()
    {
        if(inputField.text.Length != 0) {
            inputField.text = inputField.text.Remove(inputField.text.Length - 1, 1);
        }
    }

    public void AddLetter(string letter)
    {
        inputField.text = inputField.text + letter;
    }

    public void SubmitWord()
    {
        inputField.text = inputField.text;
        // Debug.Log("Text submitted successfully!");
    }
}
