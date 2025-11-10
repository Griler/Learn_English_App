using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FlashCardSceneManager : MonoBehaviour
{

    [SerializeField] private GameObject cardItem;
    private CardItem cardItemCmp;
    public void updateCard(AnimalData currentAnimal )
    {
        cardItemCmp = cardItem.GetComponent<CardItem>();
        cardItemCmp.setUpCard(currentAnimal);
    }
    
}
