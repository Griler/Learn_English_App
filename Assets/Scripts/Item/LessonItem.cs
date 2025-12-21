using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.Serialization;
using UnityEngine.SceneManagement;


public class LessonItem : BaseCode
{
    [SerializeField] private TextMeshProUGUI lessonName;
    [SerializeField] private Image lessonImage;
    [SerializeField] private Image startIcon;
    [SerializeField] private TextMeshProUGUI lessonStar;
    [SerializeField] private Button playLessonButton;
    private string lessonPlay;
    public void setData(string topicName)
    {
        setName(topicName);
        setImage(topicName);
    }

    private void setName(string name)
    {
        lessonName.text = name;
    }

    private void setImage(string name)
    {
        switch (name)
        {
            case "Pet":
                name = "dog";
                break;

            case "Sea Animal":
                name = "dolphin";
                break;

            case "Wild Animal":
                name = "tiger";
                break;
            case "Farm Animal":
                name = "cow";
                break;
            default:
                name = "dog";
                break;
        }
        lessonImage.sprite = assetManager.getSpriteAnimal(name);
        lessonImage.SetNativeSize();
    }

    public void setHightLightStart()
    {
        startIcon.color = Color.white;
    }    
    public void setDisableStart()
    {
        startIcon.color = Color.gray;
    }
}