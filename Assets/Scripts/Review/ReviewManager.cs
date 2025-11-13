using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class ReviewManager : MonoBehaviour
{
    [SerializeField] List<GrammarQuestion> grammarQuestions = new List<GrammarQuestion>();
    [SerializeField] List<WordData> listVocabulary;
    private async void Start()
    { 
        FirebaseDatabaseManager.Instance.FetchAllQuestionsByGrammar("simple_present",setGrammarQuestions);
        //FirebaseDatabaseManager.Instance.TestLoadFunction();
    }

    private void setGrammarQuestions(List<GrammarQuestion> grammarQuestions)
    {
        this.grammarQuestions = grammarQuestions;
    }
}