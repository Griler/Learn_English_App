using UnityEngine;
using UnityEngine.UI;

public class RewardSystem : MonoBehaviour
{
    public Text coinText;
    public int winReward = 100;
    public int dailyReward = 200;
    public int minReward = 10;
    public int maxReward = 20;

    private async void Start()
    {
        int currentCoins = await FirebaseDatabaseManager.Instance.GetCoins();
        coinText.text = "Coins: " + currentCoins;
    }

    public async void OnBattleWin()
    {
        await FirebaseDatabaseManager.Instance.AddCoins(winReward);
        int newCoins = await FirebaseDatabaseManager.Instance.GetCoins();
        coinText.text = "Coins: " + newCoins;
    }

    public async void OnCollectDaily()
    {
        await FirebaseDatabaseManager.Instance.CollectDailyReward(dailyReward);
        int newCoins = await FirebaseDatabaseManager.Instance.GetCoins();
        coinText.text = "Coins: " + newCoins;
    }
    
    public async void OnFinishSomeThing()
    {
        int coins = Random.Range(minReward,maxReward);
        await FirebaseDatabaseManager.Instance.AddCoins(coins);
        int newCoins = await FirebaseDatabaseManager.Instance.GetCoins();
        coinText.text = "Coins: " + newCoins;
    }
}