using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Linq;

public static class Extensions
{
    public static void CopyComponentValues<T>(this T self, T other) where T : Component
    {
        foreach (PropertyInfo property in self.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanWrite || !property.CanRead) continue;
            
            property.SetValue(self, property.GetValue(other));
        }
    }

    public static void CopyComponentValues<T>(this T self, T other, string[] exclude) where T : Component
    {
        foreach (PropertyInfo property in self.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanWrite || !property.CanRead || exclude.Contains(property.Name)) continue;

            property.SetValue(self, property.GetValue(other));
        }
    }

    public static Texture2D toTexture2D(this RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
        var old_rt = RenderTexture.active;
        RenderTexture.active = rTex;

        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();

        RenderTexture.active = old_rt;
        return tex;
    }

    public static void DoFunctionToTree(this Transform root, System.Action<Transform> function)
    {
        function.Invoke(root);
        foreach (Transform child in root)
            child.DoFunctionToTree(function);
    }

    public static void RunFunctionOnDelay(this MonoBehaviour mb, System.Action function, YieldInstruction delay)
    {
        IEnumerator DoFunction()
        {
            yield return delay;
            function.Invoke();
        }
        mb.StartCoroutine(DoFunction());
    }

    public static void RunFunctionOnDelay(this MonoBehaviour mb, System.Action function, CustomYieldInstruction delay)
    {
        IEnumerator DoFunction()
        {
            yield return delay;
            function.Invoke();
        }
        mb.StartCoroutine(DoFunction());
    }
}
