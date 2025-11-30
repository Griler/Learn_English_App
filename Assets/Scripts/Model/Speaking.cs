[System.Serializable]
public class SpeakingCategory
{
      public int id;
      public string name;
}

[System.Serializable]
public class SpeakingQuestion
{
      public int id;
      public string en;
      public string vn;
      public SpeakingCategory topic;
}