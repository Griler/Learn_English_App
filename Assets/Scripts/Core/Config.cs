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
        _spriteNames.Add("Cat","0");
        _spriteNames.Add("Dog","1");
        _spriteNames.Add("Parrot","2");
        _spriteNames.Add("Rabbit","3");
        _spriteNames.Add("Hamster","4");
        _spriteNames.Add("Cow","5");
        _spriteNames.Add("Pig","6");
        _spriteNames.Add("Sheep","7");
        _spriteNames.Add("Horse","8");
        _spriteNames.Add("Chicken","9");
        _spriteNames.Add("Duck","10");
        _spriteNames.Add("Lion","11");
        _spriteNames.Add("Tiger","12");
        _spriteNames.Add("Elephant","13");
        _spriteNames.Add("Monkey","14");
        _spriteNames.Add("Dolphin","15");
        _spriteNames.Add("Kangaroo","16");
        _spriteNames.Add("Shark","17");
        _spriteNames.Add("Bear","18");
        _spriteNames.Add("Panda","19");
        _spriteNames.Add("Zebra","20");
        _spriteNames.Add("Snake","21");
        _spriteNames.Add("Octopus","22");
        _spriteNames.Add("Starfish","23"); 
        _spriteNames.Add("Turtle","24");
        // _spriteNames.Add("Rabbit","26");
        // _spriteNames.Add("Crab","27");
        // _spriteNames.Add("Shark","28");
        // _spriteNames.Add("Horse","29");
        // _spriteNames.Add("Duck","30");
        // _spriteNames.Add("Dolphin","31");
    }

    public string formatSpriteName(string name)
    {
        if (!_spriteNames.ContainsKey(name))
        {
            Debug.LogError("không có ảnh con vật đó"  + name);
            return null;
        }
        else
        {
            return prefixAtlas + "_"+ _spriteNames[name];
        }
    }
}
