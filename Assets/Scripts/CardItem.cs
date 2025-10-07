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

    private void Start()
    {
        //setUpCard();
    }
    
    public void setUpCard(AnimalData animal = null)
    {
        string nameSprite = config.formatSpriteName(animal.name_en);
        petImage.sprite = assetManager.getSprite(nameSprite);
        petImage.SetNativeSize();
        nameEN.text = "animal_asset_0";
        nameVI.text = "animal_asset_1";
    }
}