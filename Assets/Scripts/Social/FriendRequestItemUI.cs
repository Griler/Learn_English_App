using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class FriendRequestItemUI : BaseCode
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI rankText;
    public Image avatarImage;
    public Image borderImage;
    public Button acceptButton;
    public Button declineButton;

    private string _senderId;
    private Action<string> _onAcceptCallback;
    private Action<string> _onDeclineCallback;

    public void Setup(string senderId, UserInfoData info, Action<string> onAccept, Action<string> onDecline)
    {
        _senderId = senderId;
        _onAcceptCallback = onAccept;
        _onDeclineCallback = onDecline;

        // Set UI
        if (info != null)
        {
            nameText.text = info.name;
            avatarImage.sprite = assetManager.getSpriteAvatar(info.avatar);
            borderImage.sprite = assetManager.getSpriteAvatar(info.border);
            rankText.text = info.rankPoint.ToString();
            // Set avatar...
        }

        acceptButton.onClick.RemoveAllListeners();
        acceptButton.onClick.AddListener(() => _onAcceptCallback?.Invoke(_senderId));

        declineButton.onClick.RemoveAllListeners();
        declineButton.onClick.AddListener(() => _onDeclineCallback?.Invoke(_senderId));
    }
}