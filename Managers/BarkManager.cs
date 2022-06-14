// Written by: Trevor Thacker
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarkManager : Singleton<BarkManager>
{
    private List<Bark> barks = new List<Bark>();
    [SerializeField]
    private Bark defaultBark;
    [SerializeField]
    private int initialBarks = 6;
    private void Start()
    {
        for (int i = 0; i < initialBarks; i++)
        {
            Bark initial = Instantiate(defaultBark);
            initial.transform.parent = transform;
            barks.Add(initial);
        }
    }

    public void ClearAllBarks()
    {
        foreach(Bark b in barks)
        {
            b.Clear();
        }
    }

    /// <summary>
    /// Plays a bark
    /// </summary>
    /// <param name="bark">The bark to played</param>
    /// <param name="origin">The transform to create the bark at and follow</param>
    /// <returns>The bark actively being played</returns>
    public Bark PlayBark(Bark bark, Transform origin, Vector3 offset)
    {
        Bark b = barks.Find(x => !x.inUse);
        if (b)
        {
            b.SetToBark(bark);
            b.Trigger(origin, offset);
        }
        return b;
    }
}
