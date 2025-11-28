using System;

[Serializable]
public class Vocabulary
{
    public long id;
    public string nameEn;
    public string nameVi;
    public long category_id; // Assuming the API returns the category ID
}
