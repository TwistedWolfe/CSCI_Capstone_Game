using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem instance;

    public ELEMENTS elements;

    void Awake()
    {
        instance = this;
    }

    //Say something and show it on the speech box.
    public void Say(string speech, string speaker = "", bool additive = false)
    {
        StopSpeaking();

        if(additive)
        {
            speechText.text = targetSpeech;
        }

        speaking = StartCoroutine(Speaking(speech, additive, speaker));
    }

    public void StopSpeaking()
    {
        if (isSpeaking)
        {
            StopCoroutine(speaking);
        }
        if(textArchitect != null && textArchitect.isConstructing)
        {
            textArchitect.Stop();
        }
        speaking = null;
    }

    public bool isSpeaking {get{return speaking != null;}}
    [HideInInspector] public bool isWaitingForUserInput = false;

    public string targetSpeech = "";
    Coroutine speaking = null;
    TextArchitect textArchitect = null;
    public TextArchitect currentArchitect {get{return textArchitect;}}

    IEnumerator Speaking(string speech, bool additive, string speaker = "")
    {
        speechPanel.SetActive(true);
        
        string additiveSpeech = additive ? speechText.text : "";
        targetSpeech = additiveSpeech + speech;

        if(textArchitect == null)
        {
            textArchitect = new TextArchitect(speechText, speech, additiveSpeech);
        }
        else 
        {
            textArchitect.Renew(speech, additiveSpeech);
        }

        speakerNameText.text = DetermineSpeaker(speaker);
        speakerNamePanel.SetActive(speakerNameText.text != "");

        //get or create a fresh set of dialogue details to make the dialogue system look a certain way for certain characters
        string speakerValue = speakerNameText.text;
        if(speakerValue.Contains("<color="))
        {
            speakerValue = speakerValue.Split('>')[1];
        }
      
        isWaitingForUserInput = false;

        if(isClosed)
        {
            OpenAllRequirementsForDialogueSystemVisibility(true);
        }

        while(textArchitect.isConstructing)
        {
            if(Input.GetKey(KeyCode.Space))
            {
                textArchitect.skip = true;
            }

            yield return new WaitForEndOfFrame();
        }
        
        //text finished
        isWaitingForUserInput = true;
        while(isWaitingForUserInput)
        {
            yield return new WaitForEndOfFrame();
        }
        StopSpeaking();
    }

    string DetermineSpeaker(string s)
    {
        string retVal = speakerNameText.text; //default return is the current name
        if(s != speakerNameText.text && s != "")
        {
            retVal = (s.ToLower().Contains("narrator")) ? "" : s;
        }
        if(retVal.Contains("*"))
        {
            retVal = retVal.Remove(0,1);
        }
        return retVal;
    }

    public void OpenAllRequirementsForDialogueSystemVisibility(bool v)
    {
        for (int i = 0; i < SpeechPanelRequirements.Length; i++)
        {
            SpeechPanelRequirements[i].SetActive(v);
        }
    }

    public void Open(string speakerName = "", string speech = "")
    {
        if (speakerName == "" && speech == "")
        {
            OpenAllRequirementsForDialogueSystemVisibility(false);
            return;
        }

        OpenAllRequirementsForDialogueSystemVisibility(true);

        speakerNameText.text = speakerName;

        speakerNamePanel.SetActive(speakerName != "");

        speechText.text = speech;
    }

    public bool isClosed
    {
        get { return !speechBox.activeInHierarchy; }
    }

    public void Close() 
    {
        StopSpeaking();
        
        for(int i = 0; i < SpeechPanelRequirements.Length; i++)
        {
            SpeechPanelRequirements[i].SetActive(false);
        }
    }

    [System.Serializable]
    public class ELEMENTS
    {
        //The main panel containing all dialogue related elements on the UI
        public GameObject speechPanel;
        public GameObject speakerNamePanel;
        public TextMeshProUGUI speakerNameText;
        public TextMeshProUGUI speechText;
    }
    public GameObject speechPanel {get{return elements.speechPanel;}}
    public TextMeshProUGUI speakerNameText {get{return elements.speakerNameText;}}
    public TextMeshProUGUI speechText {get{return elements.speechText;}}
    public GameObject speakerNamePanel {get{return elements.speakerNamePanel;}}

    public GameObject[] SpeechPanelRequirements;
    public GameObject speechBox;
}
