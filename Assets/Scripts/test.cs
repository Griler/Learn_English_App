using System;
using System.Collections.Generic;
using UnityEngine;

public class test : BaseCode
{
    public VocabularyDatabase vocabDatabase;
    List<VocabItem> listVocabItems = new List<VocabItem>();
    private void Start()
    {
        listVocabItems =  vocabDatabase.GetVocabsByTags(vocabDatabase.GetAllTags());
        foreach (VocabItem vocabItem in listVocabItems)
        {
            assetManager.getSpriteAnimal(vocabItem.text.en.ToLower());
        }
    }
}
