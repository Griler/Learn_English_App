using System;
using System.Collections.Generic;

[Serializable]
public class Category
{
    public int id;
    public string name;
    public int? parent_id; // Nullable for root categories

}
