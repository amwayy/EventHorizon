using TMPro;
using UnityEngine;

public class GameStat : MonoBehaviour
{
    [SerializeField] private TMP_Text statText;

    private string _originalString;

    private void Awake()
    {
        _originalString = statText.text;
    }

    private void Start()
    {
        var collectedCount = GameManager.Instance.GetCompletedLevelCount();
        statText.text = string.Format(_originalString, collectedCount);
    }
}