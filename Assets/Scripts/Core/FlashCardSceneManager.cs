using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FlashCardSceneManager : MonoBehaviour
{
    private AnimalCategorySO animalCategorySo;
    private string path= "AnimalSO/" + GlobalSelection.selectedNameSO;
    private List<AnimalData> listAnimals;
    [SerializeField] private int currentAnimal = 0;
    [SerializeField] private GameObject cardItem;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button preButton;
    [SerializeField] private Button backButton;
    private CardItem cardItemCmp;
    void Start()
    {
        nextButton.onClick.AddListener(btnNextClicked);
        preButton.onClick.AddListener(btnPrevClicked);
        backButton.onClick.AddListener(btnBackClicked);
        setUpData();
    }

    private void setUpData()
    {
        path = "AnimalSO/" + GlobalSelection.selectedNameSO;
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

    void btnBackClicked()
    {
        SceneManager.LoadSceneAsync("MainScene");
    }
}
