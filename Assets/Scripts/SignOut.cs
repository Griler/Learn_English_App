using System;
using System.Collections.Generic;

namespace test
{


    [Serializable]
    public class Example
    {
        public string conjugatedVerb;
        public string sentence;
        public string translation;
    }

    [Serializable]
    public class MiniExercise
    {
        public string question;
        public string answer;
        public string difficultyLevel;
    }

    [Serializable]
    public class GrammarTopic
    {
        // ID này chính là Key (ví dụ: "present_simple"), ta lưu lại vào đây để dễ dùng
        public string grammarPointID;
        public string description;
        public string rule;

        public List<Example> examples;
        public List<MiniExercise> miniExercises;

        // Constructor để khởi tạo List, tránh lỗi Null khi không có dữ liệu
        public GrammarTopic()
        {
            examples = new List<Example>();
            miniExercises = new List<MiniExercise>();
        }
    }
}