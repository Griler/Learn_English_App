using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FlashCardSceneManager : MonoBehaviour
{

    [SerializeField] private GameObject cardItem;
    private CardItem cardItemCmp;
    public void updateCard(VocabItem currentWord )
    {
        cardItemCmp = cardItem.GetComponent<CardItem>();
        cardItemCmp.setUpCard(currentWord);
    } 
    public void updateExample(VocabItem currentWord )
    {
        cardItemCmp = cardItem.GetComponent<CardItem>();
        cardItemCmp.setUpExample(currentWord);
    }

    public void setUpCard(VocabItem currentWord)
    {
        updateCard(currentWord);
        updateExample(currentWord);
        cardItemCmp = cardItem.GetComponent<CardItem>();
        cardItemCmp.playVoice();    
    }
}
