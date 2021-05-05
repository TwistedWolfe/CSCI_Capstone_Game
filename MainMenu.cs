using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public static MainMenu instance;

    public Animator loadGamePanel;
    public Animator settingsPanel;

    public GameSavePanel saveLoadPanel;

    public string selectedGameFile = "";

    int currentSaveLoadPage 
    {
        get {return saveLoadPanel.currentSaveLoadPage;}
    }

    void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        CloseLoadGamePanel();
        CloseSettingsPanel();
    }

    public void ClickLoadGame()
    {
        loadGamePanel.gameObject.SetActive(true);
        loadGamePanel.SetTrigger("activate");

        //populate the saveload panel
        saveLoadPanel.LoadFilesOntoScreen(currentSaveLoadPage);

        if(settingsPanel.gameObject.activeInHierarchy)
        {
            settingsPanel.SetTrigger("deactivate");
        }
    }

    public void ClickSettings()
    {
        settingsPanel.gameObject.SetActive(true);
        settingsPanel.SetTrigger("activate");

        if(loadGamePanel.gameObject.activeInHierarchy)
        {
            loadGamePanel.SetTrigger("deactivate");
        }
    }

    public void ExitOutOfLoadGamePanel()
    {
        loadGamePanel.SetTrigger("deactivate");
    }

    public void ExitOutOfSettingsPanel()
    {
        settingsPanel.SetTrigger("deactivate");
    }

    public void CloseLoadGamePanel()
    {
        loadGamePanel.gameObject.SetActive(false);
    }

    public void CloseSettingsPanel()
    {
        settingsPanel.gameObject.SetActive(false);
    }

    public void StartNewGame()
    {
        selectedGameFile = "new file";
        FileHandler.SaveFile(FileHandler.savPath + "savData/file", selectedGameFile);

        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }
}
