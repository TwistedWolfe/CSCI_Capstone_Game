using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowLoadPanel : MonoBehaviour
{

    public GameSavePanel saveLoadPanel;
    
    public void ShowPanel()
    {
        if(InputScreen.isShowingInputField || ChoiceScreen.isWaitingForChoiceToBeMade)
        {
            return;
        }

        if(!saveLoadPanel.gameObject.activeInHierarchy)
        {
            saveLoadPanel.gameObject.SetActive(true);
            saveLoadPanel.GetComponent<Animator>().SetTrigger("activate");
            saveLoadPanel.LoadFilesOntoScreen(saveLoadPanel.currentSaveLoadPage);
        }
        else
        {
            saveLoadPanel.GetComponent<Animator>().SetTrigger("deactivate");
        }
    }
}
