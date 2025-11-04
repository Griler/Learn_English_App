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

    private void Start()
    {
        currentView = viewDaily;
        //viewLesson.SetActive(true);
        informationUser.GetComponent<InformationUser>().updateInformation();
        viewDaily.SetActive(false);
        viewLesson.SetActive(true);
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
            case GlobalData.MissionKeys.LEARN_NEW:
                openViewLesson();
                break;
            case GlobalData.MissionKeys.PVP:
            case GlobalData.MissionKeys.REVIEW:
                openViewReview();
                break;
            case GlobalData.MissionKeys.PRACTICE3:
                openViewGrammar();
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
}