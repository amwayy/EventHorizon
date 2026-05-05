using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Jigsaw")]
public class JigsawSO : ScriptableObject
{
    public string jigsawName;
    public JigsawEdgeType upEdgeType;
    public JigsawEdgeType downEdgeType;
    public JigsawEdgeType leftEdgeType;
    public JigsawEdgeType rightEdgeType;
    public Texture2D texture;
    public GameObject prefab;
}