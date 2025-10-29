public class FlashcardUIExerciseController : FlashcardUIController
{
    private void OnEnable()
    {
        GameEvents.showExerciseUI += showExerciseUI;
    }

    private void OnDestroy()
    {
        GameEvents.showExerciseUI -= showExerciseUI;
    }
     
        
    private void showExerciseUI()
    {
       setActiveFlashCard(true);
    }
}