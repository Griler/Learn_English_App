using System;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [SerializeField] private GameObject currentView;
    public GameObject viewDaily;
    public GameObject viewLesson;
    public GameObject viewGrammar;
    public GameObject viewReview;
    public GameObject viewProfile;
    public GameObject informationUser;
    public GameObject viewTopic;
    public GameObject viewSubTopic;
    public GameObject viewChoose;
    public GameObject viewListen;
    public GameObject viewSpeaking;
    public GameObject viewFriend;

    private void Start()
    {
        currentView = viewChoose;
        //viewLesson.SetActive(true);
        viewDaily.SetActive(false);
        viewLesson.SetActive(false);
        viewGrammar.SetActive(false);
        viewReview.SetActive(false);
        viewProfile.SetActive(false);
        informationUser.SetActive(true);
    }

    public void OpenViewByMissionid(string id)
    {
        closeCurentView();
        switch (id)
        {
            case GlobalData.MissionKeys.LEARN_VOCA:
                openViewLesson();
                break;
            case GlobalData.MissionKeys.P2P:
            case GlobalData.MissionKeys.WIN_P2P:
                openViewReview();
                break;
            case GlobalData.MissionKeys.LEARN_GRAMMAR:
                openViewGrammar();
                break;
            case GlobalData.MissionKeys.LEARN_LISTEN:
                openViewListen();
                break;
            case GlobalData.MissionKeys.LEARN_SPEAKING:
                openViewSpeaking();
                break;
        }
    }

    public void openViewDaily()
    {
        currentView = viewDaily;
        viewDaily.SetActive(true);
    }

    public void openViewLesson()
    {
        currentView = viewLesson;
        viewLesson.SetActive(true);
        viewTopic.SetActive(true);
        viewSubTopic.SetActive(false);
    }

    public void openViewGrammar()
    {
        currentView = viewGrammar;
        viewGrammar.SetActive(true);
    }

    public void openViewReview()
    {
        currentView = viewReview;
        viewReview.SetActive(true);
    }

    public void openViewProfile()
    {
        currentView = viewProfile;
        viewProfile.SetActive(true);
    }

    public void closeCurentView()
    {
        currentView.SetActive(false);
    }

    public void openViewChoose()
    {
        currentView = viewChoose;
        viewChoose.SetActive(true);
    } 
    public void openViewListen()
    {
        currentView = viewListen;
        viewListen.SetActive(true);
    }  
    public void openViewSpeaking()
    {
        currentView = viewSpeaking;
        viewSpeaking.SetActive(true);
    }  
    public void openViewFriend()
    {
        currentView = viewFriend;
        viewFriend.SetActive(true);
    }
    
}