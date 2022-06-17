using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class ProgressObject : MonoBehaviour
{
    private enum LogicType { AND, OR, XOR }

    [SerializeField, Tooltip("The logic type to use between these conditions")]
    private LogicType logicType;
    [SerializeField]
    private List<GenericProgressTracker> conditions = new List<GenericProgressTracker>();

    public UnityEngine.Events.UnityEvent OnConditionsMet;
    public UnityEngine.Events.UnityEvent OnConditionsFailed;

    private void OnDestroy()
    {
        GenericProgressTracker.ProgressUpdated -= UpdateState;
    }

    private void Start()
    {
        GenericProgressTracker.ProgressUpdated += UpdateState;
        UpdateState();
    }

    public void UpdateState()
    {
        if (CheckConditions())
            OnConditionsMet?.Invoke();
        else
            OnConditionsFailed?.Invoke();
    }

    private bool CheckConditions()
    {
        if (conditions.Count == 0)
            return false;
        switch (logicType)
        {
            case LogicType.AND:
                {
                    bool b = true;
                    foreach (GenericProgressTracker gpt in conditions)
                        if (!gpt.isReached)
                            b = false;
                    return b;
                }
            case LogicType.OR:
                {
                    bool b = false;
                    foreach (GenericProgressTracker gpt in conditions)
                        if (gpt.isReached)
                            b = true;
                    return b;
                }
            case LogicType.XOR:
                {
                    bool b = false;
                    foreach (GenericProgressTracker gpt in conditions)
                        if (!b && gpt.isReached)
                            b = true;
                        else if (gpt.isReached)
                            return false;
                    return b;
                }
        }
        return false;
    }
}
