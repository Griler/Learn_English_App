using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using JetBrains.Annotations;
using TMPro;
using UnityEngine.UI;

public class InputKeyBoardCustom : MonoBehaviour
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    
    [SerializeField] private List<GameObject> listButton;
    private List<RectTransform> listButtonPositon = new List<RectTransform>();
    
    
    [SerializeField] private int offSetX = 0;
    [SerializeField] private int offSetY = 0;
    [SerializeField] private int fontSize = 85;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Stack<Button> buttonClicked = new Stack<Button>();
    private string originalWord;

    void Awake()
    {
        originalWord =  inputField.text;
        InitEventButton();
        InitListButtonPositon();
    }

    void InitEventButton()
    {
        foreach (GameObject gameObject in listButton)
        {
            Button button = gameObject.GetComponent<Button>();
            button.onClick.AddListener(() => onClickButton(button));
        }
        deleteButton.onClick.AddListener(onDeleteButton);
    }

    private void InitListButtonPositon()
    {
        foreach (GameObject go in listButton)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                listButtonPositon.Add(rt);
            }
        }
    }

    public void initButtonWord(string targetWord)
    {
        resetKeyBoard();
        List<char> alphabet = GenerateCharacterList(targetWord);
        for (int i = 0; i < alphabet.Count; i++)
        {
            int paddingX = Random.Range(-offSetX, offSetX);
            int paddingY = Random.Range(-offSetY, offSetY);
            TextMeshProUGUI charText = listButton[i].GetComponentInChildren<TextMeshProUGUI>();
            charText.text = alphabet[i].ToString().ToLower();
            charText.fontSize = fontSize;
            RectTransform rt = listButton[i].GetComponent<RectTransform>();
            float newPositionX = listButtonPositon[i].localPosition.x + paddingX;
            float newPositionY = listButtonPositon[i].localPosition.y + paddingY;
            rt.localPosition = new Vector2(newPositionX, newPositionY);
        }
    }
    
    public List<char> GenerateCharacterList(string targetWord)
    {
        int totalSize = listButton.Count;
        List<char> resultList = new List<char>(totalSize);
        string upperWord = targetWord.ToLower(); 
        
        foreach (char c in upperWord)
        {
            if (char.IsLetter(c)) 
            {
                resultList.Add(c);
            }
        }
        int lettersNeeded = totalSize - resultList.Count;
        for (int i = 0; i < lettersNeeded; i++)
        {
            int index = Random.Range(0, Alphabet.Length);
            resultList.Add(Alphabet[index]);
        }

        List<char> finalList = resultList
            .OrderBy(r => Random.value)
            .ToList();

        return finalList;
    }

    private void onClickButton(Button clickedButton)
    {
        string currentInput = inputField.text;
        string charButton = clickedButton.GetComponentInChildren<TextMeshProUGUI>().text.ToLower();
        currentInput = currentInput + charButton;
        inputField.text = currentInput.ToLower();
        clickedButton.interactable = false;
        buttonClicked.Push(clickedButton);
    }

    private void onDeleteButton()
    {
        string currentWord = inputField.text;
        if(currentWord.Length == 0)
            return;
        buttonClicked.Pop().interactable = true;
        currentWord = currentWord.Substring(0, currentWord.Length - 1);
        inputField.text = currentWord;
    }

    void resetKeyBoard()
    {
        inputField.text = originalWord;
        for (int i = 0; i < listButton.Count; i++)
        {
            TextMeshProUGUI charText = listButton[i].GetComponentInChildren<TextMeshProUGUI>();
            charText.text = "";
            charText.fontSize = fontSize;
            RectTransform rt = listButton[i].GetComponent<RectTransform>();
            float originalPosX = listButtonPositon[i].localPosition.x;
            float originalPosY = listButtonPositon[i].localPosition.y;
            rt.localPosition = new Vector2(originalPosX, originalPosY);
            listButton[i].GetComponent<Button>().interactable = true;
            buttonClicked.Clear();
        }
    }
}
