using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameSavePanel : MonoBehaviour
{
    public List<BUTTON> buttons = new List<BUTTON>();

    [HideInInspector]
    public int currentSaveLoadPage = 1;

    public Button loadButton;
    public Button saveButton;

    public enum TASK
    {
        saveToSlot,
        loadFromSlot,
    }

    public TASK slotTask = TASK.loadFromSlot;

    public void LoadFilesOntoScreen(int page = 1)
    {
        currentSaveLoadPage = page;

        string directory = FileHandler.savPath + "savData/gameFiles/" + page.ToString() + "/";

        if(System.IO.Directory.Exists(directory))
        {
            for(int i = 0; i < buttons.Count; i++)
            {
                BUTTON b = buttons[i];
                string expectedFile = directory + (i + 1).ToString() + ".txt";
                if (System.IO.File.Exists(expectedFile))
                {
                    GAMEFILE file = FileHandler.LoadEncryptedJSON<GAMEFILE>(expectedFile, FileHandler.keys);

                    b.button.interactable = true;
                    byte[] previewImageData = FileHandler.LoadComposingBytes(directory + (i + 1).ToString() + ".png");
                    Texture2D previewImage = new Texture2D(2, 2);
                    ImageConversion.LoadImage(previewImage, previewImageData);
                    file.previewImage = previewImage;
                    b.previewDisplay.texture = file.previewImage;

                    //read the date and time information from file
                    b.dateTimeText.text = page.ToString() + "\n" + file.modificationDate;
                }
                else
                {
                    b.button.interactable = allowSavingFromThisScreen;
                    b.previewDisplay.texture = Resources.Load<Texture2D>("Images/UI/EmptyGameFile");
                    b.dateTimeText.text = page.ToString() + "\n" + "Empty";
                }
            }
        }
        else
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                BUTTON b = buttons[i];
                b.button.interactable = allowSavingFromThisScreen;
                b.previewDisplay.texture = Resources.Load<Texture2D>("Images/UI/EmptyGameFile");
                b.dateTimeText.text = page.ToString() + "\n" + "Empty";
            }
        }
    }

    [HideInInspector]
    public BUTTON selectedButton = null;
    string selectedGameFile = "";
    string selectedFilePath = "";
    public bool allowSavingFromThisScreen = true;
    public bool allowLoadingFromThisScreen = true;
    public bool allowDeletingFromThisScreen = true;

    public void ClickOnSaveSlot(Button button)
    {
        foreach(BUTTON B in buttons)
        {
            if(B.button == button)
            {
                selectedButton = B;
            }
        }

        selectedGameFile = currentSaveLoadPage.ToString() + "/" + (buttons.IndexOf(selectedButton) + 1).ToString();
        selectedFilePath = FileHandler.savPath + "savData/gameFiles/" + selectedGameFile + ".txt";

        //run an error check to be sure the file has not been removed since load
        if(System.IO.File.Exists(selectedFilePath))
        {
            loadButton.interactable = allowLoadingFromThisScreen;
            saveButton.interactable = allowSavingFromThisScreen;
        }
        else
        {
            selectedButton.dateTimeText.text = "FILE NOT FOUND";
            loadButton.interactable = false;
            saveButton.interactable = allowSavingFromThisScreen;
        }
    }

    public void LoadFromSelectedSlot()
    {
        //we need to load the data from this slot to know what to do
        GAMEFILE file = FileHandler.LoadEncryptedJSON<GAMEFILE>(selectedFilePath, FileHandler.keys);
        
        //save the name of the file that we will be loading in the visual novel, this carries over to the next scene
        FileHandler.SaveFile(FileHandler.savPath + "savData/file", selectedGameFile);

        UnityEngine.SceneManagement.SceneManager.LoadScene("Novel");

        gameObject.SetActive(false);//deactivate the panel after loading
    }

    public void SaveToSelectedSlot()
    {
        //save the open game file to this slot
        if(NovelController.instance != null)
        {
            savingFile = StartCoroutine(SavingFile());
        }
    }

    public void ClosePanel()
    {
        if(gameObject.activeInHierarchy)
        {
            GetComponent<Animator>().SetTrigger("deactivate");
        }
    }

    Coroutine savingFile= null;
    IEnumerator SavingFile()
    {
        NovelController.instance.activeGameFileName = selectedGameFile;

        GetComponent<Animator>().SetTrigger("instantInvisible");
        yield return new WaitForEndOfFrame();

        //a screenshot is made right at this point
        NovelController.instance.SaveGameFile();

        selectedButton.dateTimeText.text = currentSaveLoadPage.ToString() + "\n" + GAMEFILE.activeFile.modificationDate;
        selectedButton.previewDisplay.texture = GAMEFILE.activeFile.previewImage;

        yield return new WaitForEndOfFrame();

        GetComponent<Animator>().SetTrigger("instantVisible");

        savingFile = null;
    }

    [System.Serializable]
    public class BUTTON
    {
        public Button button;
        public RawImage previewDisplay;
        public TextMeshProUGUI dateTimeText;
    }
}
