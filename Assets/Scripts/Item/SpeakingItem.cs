using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.Serialization;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;


public class SpeakingItem : BaseCode
{
    [SerializeField] private TextMeshProUGUI nameItem;
    [SerializeField] private Image imageItem;
    [SerializeField] private TextMeshProUGUI lessonStar;
    [SerializeField] private Button playStartButton;
   
    public void setName(string name)
    {
        nameItem.text = name;
    }

    public void setImage(Sprite sprite)
    {
        imageItem.sprite = sprite;
    }

    public void setOnClickButton(Action action)
    {
        playStartButton.onClick.AddListener(() => action?.Invoke());
        
    }
}