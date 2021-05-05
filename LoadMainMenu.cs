using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadMainMenu : MonoBehaviour
{
    public void LoadScene(int level)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}
