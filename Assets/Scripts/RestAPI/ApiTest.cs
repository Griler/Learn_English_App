using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApiTest : MonoBehaviour
{
    public ApiController apiController;

    void Start()
    {
        if (apiController == null)
        {
            Debug.LogError("ApiController is not assigned!");
            return;
        }

        // Example: Test getting all categories
        Debug.Log("Attempting to fetch categories...");
        StartCoroutine(apiController.GetCategoriesByParent(1,OnCategoriesReceived));
        StartCoroutine(apiController.GetCategoriesByParent(null,OnCategoriesReceived));

        // Example: Test getting all vocabularies
        // Debug.Log("Attempting to fetch vocabularies...");
        // StartCoroutine(apiController.GetVocabularies(OnVocabulariesReceived));
    }

    private void OnCategoriesReceived(List<Category> categories)
    {
        if (categories != null)
        {
            Debug.Log($"Successfully fetched {categories.Count} categories.");
            foreach (var category in categories)
            {
                Debug.Log($"Category ID: {category.id}, Name: {category.name}");
            }
        }
        else
        {
            Debug.LogError("Failed to fetch categories.");
        }
    }

    private void OnVocabulariesReceived(List<Vocabulary> vocabularies)
    {
        if (vocabularies != null)
        {
            Debug.Log($"Successfully fetched {vocabularies.Count} vocabularies.");
            foreach (var vocab in vocabularies)
            {
                Debug.Log($"Vocab ID: {vocab.id}, EN: {vocab.nameEn}, VI: {vocab.nameVi}");
            }
        }
        else
        {
            Debug.LogError("Failed to fetch vocabularies.");
        }
    }
}
