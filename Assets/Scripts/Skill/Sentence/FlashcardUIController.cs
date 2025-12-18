using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
internal enum TypeInputField
{
    Wrong = 1,
    Correct = 0,
    Default = 2
}

public class FlashcardUIController : BaseCode
{
    public GrammarManager GrammarManager; // Kéo object chứa script GrammarManager vào đây
    
    [SerializeField] protected TMP_InputField verbInputField;
    [SerializeField] protected TextMeshProUGUI resultText;
    [SerializeField] protected GameObject flashCardContainer;

    [Header("UI FlashCard other")] 
    [SerializeField] protected TextMeshProUGUI exampleQuestionText;
    [SerializeField] protected List<Sprite> backgroundInputField;
    [SerializeField] protected Slider progressBar;
    [SerializeField] protected List<GrammarFlashcard> listCard;
    [SerializeField] protected Button nextButton;
    
    private int currentCardIndex = 0;
    private float incrementValue = 0;
    private string voiceText = "";
    
    protected virtual void Start()
    {
    }

    public virtual void OnSubmitAnswer()
    {
       
    }

    public void onVoiceButtonVoice()
    {
        audioManager.SpeakToText(voiceText);
    }
    
    protected IEnumerator setTypeInputField(Enum colorIndex)
    {
        verbInputField.GetComponent<Image>().sprite = backgroundInputField[Convert.ToInt32(colorIndex)];
        if (Convert.ToInt32(colorIndex) == 0)
        {
            resultText.text = "Correct";
            resultText.color = Color.lawnGreen;
        }
        else if(Convert.ToInt32(colorIndex) == 1)
        {
            resultText.text = "Wrong";
            resultText.color = Color.softRed;
        }
        yield return new WaitForSeconds(0.75f);
        verbInputField.GetComponent<Image>().sprite = backgroundInputField[Convert.ToInt32(TypeInputField.Default)];
        resultText.text = "";
        resultText.color = Color.white;
    }
    
    public virtual void HandleAnswer(Enum type)
    {
        StartCoroutine(setTypeInputField(type));
        if (Convert.ToInt32(type) == 0) 
        {
            updateProgressBar();
        }
    }

    protected virtual void updateProgressBar()
    {
        incrementValue = (progressBar.maxValue / listCard.Count);
        progressBar.value = progressBar.value + incrementValue;
    }
    
    protected void setActiveFlashCard(bool active = true)
    {
        flashCardContainer.SetActive(active);  
    }
}