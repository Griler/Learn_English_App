
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Container", menuName = "Game Data/Container")]
public class ContainerSO : ScriptableObject
{
    [Header("Container Info")]
    public string containerName;
    
    [Header("Data List")]
    public List<AnimalCategorySO> items = new List<AnimalCategorySO>();
    
    // Method để thêm item
    public void AddItem(AnimalCategorySO item)
    {
        if (item != null && !items.Contains(item))
        {
            items.Add(item);
        }
    }
    
    // Method để xóa item
    public void RemoveItem(AnimalCategorySO item)
    {
        if (items.Contains(item))
        {
            items.Remove(item);
        }
    }
    
    // Method để tìm item theo tên
    public AnimalCategorySO FindItemByCategoryName(string name)
    {
        return items.Find(item => item.categoryName == name);
    }
    
    // Method để lấy số lượng items
    public int GetItemCount()
    {
        return items.Count;
    }
}