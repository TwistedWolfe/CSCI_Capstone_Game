using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TagManager : MonoBehaviour
{

  public static void Inject(ref string s )
  {
    if(!s.Contains("["))
    {
      return;
    }

    s = s.Replace("[playername]", GAMEFILE.activeFile != null ? GAMEFILE.activeFile.playerName : "No Game File"); 
  }

  public static string[] SplitByTags(string targetText)
  {
      return targetText.Split(new char[2]{'<','>'});
  }
}
