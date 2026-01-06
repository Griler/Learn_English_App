using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchingButton : MonoBehaviour
{
    public MatchingPairData pairData;
    public bool isTextButton;

    private Button button;
    private Image backgroundImage;
    public Image imageComponent;
    private TextMeshProUGUI textComponent;
    private ReviewManager manager;

    [Header("Colors")]
    public Color normalColor = new Color32(86, 107, 132, 255);
    public Color selectedColor = Color.yellow;
    public Color matchedColor = Color.green;

    public void Setup(MatchingPairData pair, bool isText, ReviewManager mgr)
    {
        pairData = pair;
        isTextButton = isText;
        manager = mgr;

        button = GetComponent<Button>();
        backgroundImage = GetComponent<Image>();

        if (isTextButton)
        {
            textComponent = GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = pair.textEN;
            }
        }
        else
        {
            if (imageComponent != null && imageComponent != backgroundImage)
            {
                imageComponent.sprite = pair.image;
            }
        }

        button.onClick.AddListener(() => manager.OnMatchingButtonClicked(this));
        backgroundImage.color = normalColor;
    }

    public void Select()
    {
        backgroundImage.color = selectedColor;
    }

    public void Deselect()
    {
        backgroundImage.color = normalColor;
    }

    public void SetMatched()
    {
        backgroundImage.color = matchedColor;
        button.interactable = false;
    }
}