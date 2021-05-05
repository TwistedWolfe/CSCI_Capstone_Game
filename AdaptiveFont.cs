using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AdaptiveFont : MonoBehaviour
{
    Text txt;
    public bool continualUpdate = true;

    public int fontSizeAtDefaultResolution = 24;
    public static float defaultResolution = 2827f;

    // Start is called before the first frame update
    void Start()
    {
        txt = GetComponent<Text> ();

        if(continualUpdate)
        {
            InvokeRepeating ("Adjust", 0f, Random.Range (0.3f, 1f));
        }
        else
        {
            Adjust();
            enabled = false;
        }
    }

    void Adjust()
    {
        if(!enabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        float totalCurrentRes = Screen.height + Screen.width;
        float perc = totalCurrentRes / defaultResolution;
        int fontsize = Mathf.RoundToInt((float)fontSizeAtDefaultResolution * perc);

        txt.fontSize = fontsize;
    }
}
