using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Config : MonoBehaviour
{
    public static Config Instance { get; private set; }
    private Dictionary<string, string> _spriteNames =  new Dictionary<string, string>();
    string prefixAtlas = "animal_asset";
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        setUpConfig();
        DontDestroyOnLoad(gameObject); // giữ qua scene
    }

    private void setUpConfig()
    {
        _spriteNames.Add("Dog","0");
        _spriteNames.Add("Cat","1");
        _spriteNames.Add("Pig","2");
        _spriteNames.Add("Rabbit","3");
        _spriteNames.Add("Hamster","4");
        _spriteNames.Add("Parrot","5");
        _spriteNames.Add("Goldfish","6");
        _spriteNames.Add("Sheep","7");
        _spriteNames.Add("Lion","8");
        _spriteNames.Add("Tiger","9");
        //_spriteNames.Add("dog","10");
        _spriteNames.Add("Elephant","11");
        _spriteNames.Add("Whale","12");
        //_spriteNames.Add("dog","13");
        _spriteNames.Add("Giraffe","14");
        //_spriteNames.Add("dog","15");
        //_spriteNames.Add("dog","16");
        _spriteNames.Add("Bear","17");
        _spriteNames.Add("Monkey","18");
        _spriteNames.Add("Panda","19");
        // _spriteNames.Add("dog","20");
        _spriteNames.Add("Turtle","21");
        // _spriteNames.Add("null","22");
        // _spriteNames.Add("null","23");
        // _spriteNames.Add("null","24");
        _spriteNames.Add("Snake","25");
        //_spriteNames.Add("Rabbit","26");
        _spriteNames.Add("Crab","27");
        _spriteNames.Add("Shark","28");
        _spriteNames.Add("Horse","29");
        _spriteNames.Add("Duck","30");
        _spriteNames.Add("Dolphin","31");
    }

    public string formatSpriteName(string name)
    {
        if (!_spriteNames.ContainsKey(name))
        {
            Debug.LogError("không có ảnh con vật đó");
            return "null";
        }
        else
        {
            return prefixAtlas + "_"+ _spriteNames[name];
        }
    }
}
