using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainSceneManager : MonoBehaviour
{
    private AnimalCategorySO animalCategorySo;
    private string path= "AnimalSO/" + GlobalSelection.selectedNameSO;
    private List<Animal> listAnimals;
    [SerializeField] private int currentAnimal = 0;
    [SerializeField] private GameObject cardItem;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button preButton;
    private CardItem cardItemCmp;
    void Start()
    {
        nextButton.onClick.AddListener(btnNextClicked);
        preButton.onClick.AddListener(btnPrevClicked);
        setUpData();
    }

    private void setUpData()
    {
        animalCategorySo = ScriptableObject
            .Instantiate(Resources.Load<AnimalCategorySO>(path));
        listAnimals = animalCategorySo.animals;
        updateCard(currentAnimal);
    }

    private void updateCard(int currentAnimal = 0)
    {
        cardItemCmp = cardItem.GetComponent<CardItem>();
        cardItemCmp.setUpCard(listAnimals[currentAnimal]);
    }

    void btnNextClicked()
    {
        if (currentAnimal < listAnimals.Count - 1 )
        {
            currentAnimal++;
            updateCard(currentAnimal);
        }
    }

    void btnPrevClicked()
    {
        if (currentAnimal > 0)
        {
            currentAnimal--;
            updateCard(currentAnimal);
        }
    }
}
