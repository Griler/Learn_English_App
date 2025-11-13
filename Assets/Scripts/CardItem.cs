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
    
    public void setUpCard(WordData word = null)
    {
        string nameSprite = config.formatSpriteName(word.name_en);
        if(nameSprite != "")
            petImage.sprite = assetManager.getSpriteAnimal(nameSprite);
        petImage.SetNativeSize();
        nameEN.text = word.name_en;
        nameVI.text = word.name_vi;
        wordToVoice = word.name_en;
    }

    public void playVoice()
    {
        audioManager.playVoiceWord(wordToVoice);
    }
}