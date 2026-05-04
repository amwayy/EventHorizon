using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "ScriptableObjects/Jigsaw Database")]
public class JigsawDatabase : ScriptableObject
{
    public List<JigsawSO> allJigsaws;
}