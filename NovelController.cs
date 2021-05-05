using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NovelController : MonoBehaviour
{
    public static NovelController instance;

    public GameSavePanel saveLoadPanel;

    //The lines of data loaded directly from a chapter file
    List<string> data = new List<string>();

    void Awake()
    {
        instance = this;
    }

    [HideInInspector]
    public string activeGameFileName = "";

    GAMEFILE activeGameFile {get{ return GAMEFILE.activeFile;} set{GAMEFILE.activeFile = value;}}

    string activeChapterFile = "";
    
    // Start is called before the first frame update
    void Start()
    {
        saveLoadPanel.gameObject.SetActive(false);

        LoadGameFile(FileHandler.LoadFile(FileHandler.savPath + "savData/file.txt")[0]);
    }

    bool encryptGameFile = true;

    public void LoadGameFile(string gameFileName)
    {
        activeGameFileName = gameFileName;

        string filePath = FileHandler.savPath + "savData/gameFiles/" + activeGameFileName + ".txt";

        if(!System.IO.File.Exists(filePath))
        {
           activeGameFile = new GAMEFILE();
        }
        else
        {
            if(encryptGameFile)
            {
                activeGameFile = FileHandler.LoadEncryptedJSON<GAMEFILE>(filePath, keys);
            }
            else
            {
                activeGameFile = FileHandler.LoadJSON<GAMEFILE>(filePath);
            }
        }

        //Load the file
        TextAsset chapterAsset = Resources.Load<TextAsset>("Story/" + activeGameFile.chapterName);
        //data = FileHandler.LoadFile(FileHandler.savPath + "Resources/Story/" + activeGameFile.chapterName);
        data = FileHandler.ReadTextAsset(chapterAsset);
        activeChapterFile = activeGameFile.chapterName;
        cachedLastSpeaker = activeGameFile.cachedLastSpeaker;

        DialogueSystem.instance.Open(activeGameFile.currentTextSystemSpeakerNameText, activeGameFile.currentTextSystemDisplayText);

        //Load all characters back into the scene
        for(int i = 0; i < activeGameFile.charactersInScene.Count; i++)
        {
            GAMEFILE.CHARACTERDATA data = activeGameFile.charactersInScene[i];
            Character character = CharacterManager.instance.CreateCharacter(data.characterName, data.enabled);
            character.displayName = data.displayName;
            character.canvasGroup.alpha = 1;
            character.SetBody(data.bodyExpression);
            if(data.facingLeft)
            {
                character.FaceLeft();
            }
            else
            {
                character.FaceRight();
            }
            character.SetPosition(data.position);
        }

        //Load the layer images back into the scene
        if(activeGameFile.background != null)
        {
            BCFC.instance.background.SetTexture(activeGameFile.background);
        }
        if(activeGameFile.cinematic != null)
        {
            BCFC.instance.cinematic.SetTexture(activeGameFile.cinematic);
        }
        if(activeGameFile.foreground != null)
        {
            BCFC.instance.foreground.SetTexture(activeGameFile.foreground);
        }

        //start the music back up
        if(activeGameFile.music != null)
        {
            AudioManager.instance.PlaySong(activeGameFile.music);
        }

        if(handlingChapterFile != null)
        {
            StopCoroutine(handlingChapterFile);
        }
        handlingChapterFile = StartCoroutine(HandlingChapterFile());

        chapterProgress = activeGameFile.chapterProgress;

    }

    public void SaveGameFile()
    {
        string filePath = FileHandler.savPath + "savData/gameFiles/" + activeGameFileName + ".txt";

        activeGameFile.chapterName = activeChapterFile;
        activeGameFile.chapterProgress = chapterProgress;
        activeGameFile.cachedLastSpeaker = cachedLastSpeaker;

        activeGameFile.currentTextSystemSpeakerNameText = DialogueSystem.instance.speakerNameText.text;
        activeGameFile.currentTextSystemDisplayText = DialogueSystem.instance.speechText.text;

        //get all the characters and save their stats
        activeGameFile.charactersInScene.Clear();
        for(int i = 0; i < CharacterManager.instance.characters.Count; i++)
        {
            Character character = CharacterManager.instance.characters[i];
            GAMEFILE.CHARACTERDATA data = new GAMEFILE.CHARACTERDATA(character);
            activeGameFile.charactersInScene.Add(data);
        }

        //save the layers to disk
        BCFC b = BCFC.instance;
        activeGameFile.background = b.background.activeImage != null ? b.background.activeImage.texture : null;
        activeGameFile.cinematic = b.cinematic.activeImage != null ? b.cinematic.activeImage.texture : null;
        activeGameFile.foreground = b.foreground.activeImage != null ? b.foreground.activeImage.texture : null;

        //save the music to disk
        activeGameFile.music = AudioManager.activeSong != null ? AudioManager.activeSong.clip : null;

        //save a preview image (screenshot) to be viewed from the save/load screen
        string screenShotPath = FileHandler.savPath + "savData/gameFiles/" + activeGameFileName + ".png";
        if(FileHandler.TryCreateDirectoryFromPath(screenShotPath + ".png"))
        {
            GAMEFILE.activeFile.previewImage = ScreenCapture.CaptureScreenshotAsTexture();
            byte[] textureData = activeGameFile.previewImage.EncodeToPNG();
            FileHandler.SaveComposingBytes(screenShotPath, textureData);
        }
        //save the date and time this file was created or modified
        activeGameFile.modificationDate = System.DateTime.Now.ToString();

        if (encryptGameFile)
        {
            FileHandler.SaveEncryptedJSON(filePath, activeGameFile, keys);
        }
        else
        {
            FileHandler.SaveJSON(filePath, activeGameFile);
        }
    }

    byte[] keys {get{return FileHandler.keys;}}

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Mouse1))
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

    public void LoadChapterFile(string fileName)
    {
        activeChapterFile = fileName;

        data = FileHandler.ReadTextAsset(Resources.Load<TextAsset>($"Story/{fileName}"));
        cachedLastSpeaker = "";

        if(handlingChapterFile != null)
        {
            StopCoroutine(handlingChapterFile);
        }
        handlingChapterFile = StartCoroutine(HandlingChapterFile());

        Next();
    }

    bool _next = false;
    public void Next()
    {
        _next = true;
    }

    public bool isHandlingChapterFile {get{return handlingChapterFile != null;}}
    Coroutine handlingChapterFile = null;
    [HideInInspector]public int chapterProgress = 0;
    IEnumerator HandlingChapterFile()
    {
        //progress through the lines in this chapter
        chapterProgress = 0;

        while(chapterProgress < data.Count)
        {
            //need a way of knowing when the player wants to advance. This is a next trigger, not just a keypress
            //something that can be triggered by a click or keypress
            if(_next)
            {
                string line = data[chapterProgress]; //this is the line loaded in its pure format. No inections have taken place yet
                //make sure the line has the proper data injected in it where it needs it
                TagManager.Inject(ref line); //inject data into the line where it may be needed
                //now our line will be properly formatted and ready to use

                 //this is a choice
                if(line.StartsWith("CHOICE"))
                {
                    yield return HandlingChoiceLine(line);
                    chapterProgress++;
                }
                //this is user input
                else if(line.StartsWith("INPUT"))
                {
                    yield return HandlingInputLine(line);
                    chapterProgress++;
                }
                //this is a normal line of dialogue and actions.
                else
                {
                    HandleLine(line);
                    chapterProgress++;
                    while(isHandlingLine)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                }
            }
            yield return new WaitForEndOfFrame();
        }

        handlingChapterFile = null;
    }

    IEnumerator HandlingInputLine(string line)
    {
        string title = line.Split('"')[1];

        //get the one or more commands to execute when this input is done and accepted
        string[] parts = line.Split(' ');
        List<string> endingCommands = new List<string>();
        if(parts.Length >= 3)
        {
            for(int i = 2; i < parts.Length; i++)
            {
                endingCommands.Add(parts[1]);
            }
        }

        //we have the title and the ending commands to execute, not bring up the input screen
        InputScreen.Show(title);
        while(InputScreen.isWaitingForUserInput || InputScreen.isRevealing)
        {
            if(Input.GetKey(KeyCode.Return) && !InputScreen.isRevealing)
            {
                if(InputScreen.currentInput != "")
                {
                    InputScreen.instance.Accept();
                }
            }

            yield return new WaitForEndOfFrame();
        }

        for(int i = 0; i < endingCommands.Count; i++)
        {
            string command = endingCommands[i];
            HandleAction(command);
        }
    }

    IEnumerator HandlingChoiceLine(string line)
    {
        string title = line.Split('"')[1];
        List<string> choices = new List<string>();
        List<string> actions = new List<string>();

        bool gatheringChoices = true;
        while(gatheringChoices)
        {
            chapterProgress++;
            line = data[chapterProgress];

            if(line == "{")
            {
                continue;
            }
            line = line.Replace("  ", ""); //removes the tabs that have become quad spaces

            if(line != "}")
            {
                choices.Add(line.Split('"')[1]);
                actions.Add(data[chapterProgress+1].Replace("   ",""));
                chapterProgress++;
            }
            else
            {
                gatheringChoices = false;
            }
        }

        //display choices
        if(choices.Count > 0)
        {
            ChoiceScreen.Show(title, choices.ToArray()); yield return new WaitForEndOfFrame();
            while(ChoiceScreen.isWaitingForChoiceToBeMade)
            {
                yield return new WaitForEndOfFrame();
            }
            //choice is made. exectie the paired action
            string action = actions[ChoiceScreen.lastChoiceMade.index];
            HandleLine(action);

            while(isHandlingLine)
            {
                yield return new WaitForEndOfFrame();
            }        
        }
        else
        {
            Debug.LogError("Invalid choice operation. No choices were found!");
        }
    }

    void HandleLine(string rawLine)
    {
        CLM.LINE line = CLM.Interpret(rawLine);

        //now we need to handle the line. This requires a loop full of waiting for input since the line consists of multiple segments that have to be
		//handled individually.
        StopHandlingLine();
        handlingLine = StartCoroutine(HandlingLine(line));
    }

    void StopHandlingLine()
    {
        if(isHandlingLine)
        {
            StopCoroutine(handlingLine);
        }
        handlingLine = null;
    }

    [HideInInspector] 
    public string cachedLastSpeaker = ""; //used as a fallback when no speaker is given

    public bool isHandlingLine{get{return handlingLine != null;}}
    Coroutine handlingLine = null;
    IEnumerator HandlingLine(CLM.LINE line)
    {
        //since the "next" trigger controls the flow of a chapter by moving through lines and yet also controls the progression through a line by
		//its segments, it must be reset.
        _next = false;
        int lineProgress = 0;//progress through the segments of a line.

        while(lineProgress < line.segments.Count)
        {
            _next = false;//reset at the start of each loop
            CLM.LINE.SEGMENT segment = line.segments[lineProgress];

            //always run the first segment automatically. But wait for the trigger on all proceding segments.
            if(lineProgress > 0)
            {
                if(segment.trigger == CLM.LINE.SEGMENT.TRIGGER.autoDelay)
                {
                    for(float timer = segment.autoDelay; timer >= 0; timer -= Time.deltaTime)
                    {
                        yield return new WaitForEndOfFrame();
                        if(_next)
                        {
                            break;//allow the termination of a delay when "next" is triggered. Prevents unskippable wait timers.
                        }
                    }
                }
                else
                {
                    while(!_next)
                    {
                        yield return new WaitForEndOfFrame();//wait until the player says move to the next segment.
                    }
                }
            }
            _next = false;//next could have been triggered during an event above.

            //the segment now needs to build and run
            segment.Run();

            while(segment.isRunning)
            {
                yield return new WaitForEndOfFrame();
                //allow for auto completion of the current segment for skipping purposes.
                if(_next)
                {
                    //rapidly complete the text on first advance, force it to finish on the second.
                    if(!segment.architect.skip)
                    {
                        segment.architect.skip = true;
                    }
                    else
                    {
                        segment.ForceFinish();
                    }
                    _next = false;
                }
            }

            lineProgress++;

            yield return new WaitForEndOfFrame();
        }

        //Line is finished. Handle all the actions set at the end of the line
        for(int i = 0; i < line.actions.Count; i++)
        {
            HandleAction(line.actions[i]);
        }
        handlingLine = null;
    }
    
    public void HandleAction(string action)
    {
        string[] data = action.Split('(',')');
        switch(data[0])
        {

        case "enter":
            Command_Enter(data[1]);
            break;

        case "exit":
            Command_Exit(data[1]);
            break;

        case "savePlayerName":
            Command_SavePlayerName(InputScreen.currentInput);//saves the player name as what was last input by the player
            break;

        case("setBackground"):
            Command_SetLayerImage(data[1], BCFC.instance.background);
            break;
        
        case("setCinematic"):
            Command_SetLayerImage(data[1], BCFC.instance.cinematic);
            break;

        case("setForeground"):
            Command_SetLayerImage(data[1], BCFC.instance.foreground);
            break;

        case("playMusic"):
            Command_PlayMusic(data[1]);
            break;

        case("move"):
            Command_MoveCharacter(data[1]);
            break;

        case("setPosition"):
            Command_SetPosition(data[1]);
            break;

        case("setBody"):
            Command_SetBody(data[1]);
            break;

        case("flip"):
            Command_Flip(data[1]);
            break;

        case("faceLeft"):
            Command_FaceLeft(data[1]);
            break;

        case("faceRight"):
            Command_FaceRight(data[1]);
            break;

        case "transBackground":
            Command_TransLayer(BCFC.instance.background, data[1]);
            break;

        case "transCinematic":
            Command_TransLayer(BCFC.instance.cinematic, data[1]);
            break;

        case "transForeground":
            Command_TransLayer(BCFC.instance.foreground, data[1]);
            break;

        case "showScene":
            Command_ShowScene(data[1]);
            break;

        case "Load":
            Command_Load(data[1]);
            break;

        case "next":
			Next();
			break;
        }
    }

    void Command_SavePlayerName(string newName)
    {
        activeGameFile.playerName = newName;
    }

    void Command_Load(string chapterName)
    {
        NovelController.instance.LoadChapterFile(chapterName);
    }

    void Command_SetLayerImage(string data, BCFC.LAYER layer)
    {
        string texName = data.Contains(",") ? data.Split(',')[0] : data;
        Texture2D tex = texName == "null" ? null : Resources.Load("Images/UI/Backdrops/" + texName) as Texture2D;
        float spd = 2f;
        bool smooth = false;

        if(data.Contains(","))
        {
            string[] parameters = data.Split(',');
            foreach(string p in parameters)
            {
                float fVal = 0;
                bool bVal = false;
                if(float.TryParse(p, out fVal))
                {
                    spd = fVal; continue;
                }
                if(bool.TryParse(p, out bVal))
                {
                    smooth = bVal; continue;
                }
            }
        }

        layer.TransitionToTexture(tex, spd, smooth);
    }

    void Command_PlayMusic(string data)
    {
        if(data.ToLower() == "null")
        {
            AudioManager.instance.PlaySong(null);
        }
        else
        {
            AudioClip clip = Resources.Load("Audio/Music/" + data) as AudioClip;

            AudioManager.instance.PlaySong(clip);
        }
    }

    void Command_MoveCharacter(string data)
    {
        string[] parameters = data.Split(',');
        string character = parameters[0];
        float locationX = float.Parse(parameters[1]);
        float locationY = parameters.Length >= 3 ? float.Parse(parameters[2]) : 0;
        float speed = parameters.Length >= 4 ? float.Parse(parameters[3]) : 7f;
        bool smooth = parameters.Length == 5 ? bool.Parse(parameters[4]) : true;

        Character c = CharacterManager.instance.GetCharacter(character);
        c.MoveTo(new Vector2(locationX, locationY), speed, smooth);
    }

    void Command_SetPosition(string data)
    {
        string[] parameters = data.Split(',');
        string character = parameters[0];
        float locationX = float.Parse(parameters[1]);
        float locationY = parameters.Length == 3 ? float.Parse(parameters[2]) : 0;

        Character c = CharacterManager.instance.GetCharacter(character);
        c.SetPosition(new Vector2(locationX, locationY));
    }
    
    void Command_SetBody(string data)
    {
        string[] parameters = data.Split(',');
		string character = parameters[0];
		string expression = parameters[1];
		float speed = parameters.Length == 3 ? float.Parse(parameters[2]) : 3f;

		Character c = CharacterManager.instance.GetCharacter(character);
		Sprite sprite = c.GetSprite(expression);

		c.TransitionBody(sprite, speed, false);
    }
    
    void Command_Flip(string data)
    {
        string[] characters = data.Split(',');

        foreach(string s in characters)
        {
            Character c = CharacterManager.instance.GetCharacter(s);
            c.Flip();
        }
    }

    void Command_FaceLeft(string data)
    {
        string[] characters = data.Split(',');

        foreach(string s in characters)
        {
            Character c = CharacterManager.instance.GetCharacter(s);
            c.FaceLeft();
        }
    }

    void Command_FaceRight(string data)
    {
        string[] characters = data.Split(',');

        foreach(string s in characters)
        {
            Character c = CharacterManager.instance.GetCharacter(s);
            c.FaceRight();
        }
    }

    void Command_Exit(string data)
    {
        string[] parameters = data.Split(',');
        string[] characters = parameters[0].Split(';');
        float speed = 3;
        bool smooth = false;
        for(int i = 1; i < parameters.Length; i++)
        {
            float fVal = 0; bool bVal = false;
            if(float.TryParse(parameters[i], out fVal))
            {
                speed = fVal; continue;
            }
            if(bool.TryParse(parameters[i], out bVal))
            {
                smooth = bVal; continue;
            }
        }

        foreach(string s in characters)
        {
            Character c = CharacterManager.instance.GetCharacter(s);
            c.FadeOut(speed,smooth);
        }
    }

    void Command_Enter(string data)
    {
        string[] parameters = data.Split(',');
        string[] characters = parameters[0].Split(';');
        float speed = 3;
        bool smooth = false;
        for(int i = 1; i < parameters.Length; i++)
        {
            float fVal = 0; bool bVal = false;
            if(float.TryParse(parameters[i],out fVal))
            {
                speed = fVal; continue;
            }
            if(bool.TryParse(parameters[i], out bVal))
            {
                smooth = bVal; continue;
            }
        }

        foreach(string s in characters)
        {
            Character c = CharacterManager.instance.GetCharacter(s, true, false);
            c.enabled = true;
            c.FadeIn(speed, smooth);
        }
    }
    
    void Command_TransLayer(BCFC.LAYER layer, string data)
	{
		string[] parameters = data.Split(',');

		string texName = parameters[0];
		string transTexName = parameters[1];
		Texture2D tex = texName == "null" ? null : Resources.Load("Images/UI/Backdrops/" + texName) as Texture2D;
		Texture2D transTex = Resources.Load("Images/TransitionEffects/" + transTexName) as Texture2D;

		float spd = 2f;
		bool smooth = false;

		for(int i = 2; i < parameters.Length; i++)
		{
			string p = parameters[i];
			float fVal = 0;
			bool bVal = false;
			if (float.TryParse(p, out fVal))
				{spd = fVal; continue;}
			if (bool.TryParse(p, out bVal))
				{smooth = bVal; continue;}
		}

		TransitionMaster.TransitionLayer(layer, tex, transTex, spd, smooth);
	}

    void Command_ShowScene(string data)
	{
		string[] parameters = data.Split(',');
		bool show = bool.Parse(parameters[0]);
		string texName = parameters[1]; 
		Texture2D transTex = Resources.Load("Images/TransitionEffects/" + texName) as Texture2D; 
		float spd = 2f;
		bool smooth = false;

		for(int i = 2; i < parameters.Length; i++)
		{
			string p = parameters[i];
			float fVal = 0;
			bool bVal = false;
			if (float.TryParse(p, out fVal)) 
			{spd = fVal; continue;}
			if (bool.TryParse(p, out bVal)) 
			{smooth = bVal; continue;}
		}

		TransitionMaster.ShowScene(show, spd, smooth, transTex);
	}

}
