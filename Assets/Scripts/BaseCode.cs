using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class BaseCode : MonoBehaviour
{
  protected AssetManager assetManager;

  public BaseCode()
  {
    assetManager = AssetManager.Instance;
  }
}