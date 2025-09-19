using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitItem : MonoBehaviour
{
    [SerializeField] private ContainerSO containerSo;
    [SerializeField] private TextMeshProUGUI nameUnit;
    [SerializeField] private GameObject layout;
    [SerializeField] private GameObject lessonItemPrefab;

    private void Start()
    {
        nameUnit.text = containerSo.containerName;
        loadData();
    }
    private void loadData()
    {
        if (lessonItemPrefab == null)
        {
            Debug.LogError("No Lesson Item Prefab Set");
            return;
        }
        for (int i = 0; i < containerSo.GetItemCount(); i++)
        {
            GameObject lessonItem = Instantiate(lessonItemPrefab);
            lessonItem.transform.SetParent(layout.transform);
            lessonItem.GetComponent<LessonItem>().setData(containerSo.items[i]);
        }
    }
}
