using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardItem : BaseCode
{
    public Image petImage;
    public TextMeshProUGUI nameEN;
    public TextMeshProUGUI nameVI;
    
    private string wordToVoice = "";
    private void Start()
    {
        //setUpCard();
    }
    
    public void setUpCard(AnimalData animal = null)
    {
        string nameSprite = config.formatSpriteName(animal.name_en);
        petImage.sprite = assetManager.getSprite(nameSprite);
        petImage.SetNativeSize();
        nameEN.text = animal.name_en;
        nameVI.text = animal.name_vi;
        wordToVoice = animal.name_en;
    }

    public void playVoice( )
    {
        audioManager.playVoiceWord(wordToVoice);
    }
}