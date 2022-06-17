using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Level Data")]
public class LevelLoadData : ScriptableObject
{
    [SerializeField]
    private string _referenceName;
    public string ReferenceName => _referenceName;

    [SerializeField]
    private int _primarySceneBuildIndex;
    public int PrimarySceneBuildIndex => _primarySceneBuildIndex;

    [SerializeField]
    private List<int> _additiveSceneBuildIndecies;
    public List<int> AdditiveSceneBuildIndecies => _additiveSceneBuildIndecies;
}
