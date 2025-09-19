using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.Serialization;


public class LessonItem : BaseCode
{
    [SerializeField] private TextMeshProUGUI lessonName;
    [SerializeField] private Image lessonImage;
    [SerializeField] private TextMeshProUGUI lessonStart;

    public void setData(AnimalCategorySO animalCategorySo)
    {
        setImage(animalCategorySo.categoryName);
        setName(animalCategorySo.categoryName);
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
                name = "animal_asset_1";
                break;

            case "Sea Animal":
                name = "animal_asset_28";
                break;

            case "Wild Animal":
                name = "animal_asset_8";
                break;
            case "Farm Animal":
                name = "animal_asset_7";
                break;
            default:
                name = "animal_asset_0";
                break;
        }
        lessonImage.sprite = assetManager.getSprite(name);
        lessonImage.SetNativeSize();
    }
}