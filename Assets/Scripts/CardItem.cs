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
        string nameSprite = config.formatSpriteName(word.nameEn);
        if(nameSprite != "")
            petImage.sprite = assetManager.getSpriteAnimal(nameSprite);
        petImage.SetNativeSize();
        nameEN.text = word.nameEn;
        nameVI.text = word.nameVi;
        wordToVoice = word.nameEn;
    }

    public void playVoice()
    {
        audioManager.playVoiceWord(wordToVoice);
    }
}