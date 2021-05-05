using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GAMEFILE
{
    //the currently open game file
    public static GAMEFILE activeFile = new GAMEFILE();

    public string chapterName;
    public int chapterProgress = 0;

    public string playerName = "";

    public string cachedLastSpeaker = "";

    public string currentTextSystemSpeakerNameText = "";
    public string currentTextSystemDisplayText = "";

    public List<CHARACTERDATA> charactersInScene = new List<CHARACTERDATA>();

    public Texture background = null;
    public Texture cinematic = null;
    public Texture foreground = null;

    public AudioClip music = null;

    public string modificationDate = "";
    public Texture2D previewImage = null;

    public GAMEFILE()
    {
        this.chapterName = "CH0_Start";
        this.chapterProgress = 0;
        this.cachedLastSpeaker = "";

        this.playerName = "Player";

        this.background = null;
        this.cinematic = null;
        this.foreground = null;

        this.music = null;

        charactersInScene = new List<CHARACTERDATA>();
    }

    [System.Serializable]
    public class CHARACTERDATA
    {
        public string characterName = "";
        public string displayName = "";
        public bool enabled = true;
        public string bodyExpression = "";
        public bool facingLeft = true;
        public Vector2 position = Vector2.zero;

        public CHARACTERDATA(Character character)
        {
            this.characterName = character.characterName;
            this.displayName = character.displayName;
            this.enabled = character.enabled;
            this.bodyExpression = character.renderers.bodyRenderer.sprite.name;
            this.facingLeft = character.isFacingLeft;
            this.position = character._targetPosition;
        }
    }
}
