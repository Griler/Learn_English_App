using System;
using System.Collections.Generic;

[Serializable]
public class Category
{
    public int id;
    public string name;
    public int? parent_id; // Nullable for root categories

    // We remove the nested lists to prevent serialization depth errors with JsonUtility.
    // After fetching the flat list of all categories, you can manually
    // reconstruct the parent-child hierarchy in your application logic if needed.
    // public List<Category> children;
    // public List<Vocabulary> vocabularies;
}
