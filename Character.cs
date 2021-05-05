using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class Character 
{
    public string characterName;

    //name when the player character speaks
    public string displayName = "";
    //he root is the container for all images related to the characters in the scene
    [HideInInspector]public RectTransform root;

    public bool enabled {get{return root.gameObject.activeInHierarchy;} set{root.gameObject.SetActive (value);}}

    //the space between the anchors of the character. Defines how much space a character takes up on the canvas
    public Vector2 anchorPadding {get{ return root.anchorMax - root.anchorMin;}}

    DialogueSystem dialogue;

    //Make this character say something
    public void Say(string speech, bool add = false)
    {
        if(!enabled)
        {
            enabled = true;
        }
        if(!isInScene)
        {
            FadeIn();
        }

        Debug.Log(characterName);

        dialogue.Say(speech, displayName, add);
    }

    public Vector2 _targetPosition
    {
        get{return targetPosition;}
    }

    Vector2 targetPosition;
    Coroutine moving;
    bool isMoving{get{ return moving != null;}}

    //Move to a specific point relative to the canvas space
    public void MoveTo(Vector2 Target, float speed, bool smooth = true)
    {
        Debug.Log("move " + characterName + " to " + Target.ToString());
        //if the character is moving, stop moving
        StopMoving();
        //start the moving coroutine
        moving = CharacterManager.instance.StartCoroutine(Moving(Target, speed, smooth));
    }

    //stops the moving of the character in its tracks, either setting it immediaely at the target position or not
    public void StopMoving(bool arriveAtTargetImmediately = false)
    {
        if(isMoving)
        {
            CharacterManager.instance.StopCoroutine(moving);
            if(arriveAtTargetImmediately)
            {
                SetPosition(targetPosition);
            }
        }
        moving = null;
    }

    //immediately set the position of this character to the intended target
    public void SetPosition(Vector2 target)
    {
        targetPosition = target;

        Vector2 padding = anchorPadding;
        float maxX = 1f - padding.x;
        float maxY = 1f - padding.y;

        Vector2 minAnchorTarget = new Vector2(maxX * targetPosition.x, maxY * targetPosition.y);

        root.anchorMin = minAnchorTarget;
        root.anchorMax = root.anchorMin + padding;
    }   

    //the coroutine that runs to gradually move the character towards a position
    IEnumerator Moving(Vector2 target, float speed, bool smooth)
    {
            targetPosition = target;

            //we want to get the padding between the anchors of the character so we know what their min and max positions are
            Vector2 padding = anchorPadding;

            //get the limitations for 0 to 100% movement (calculates percentage)
            float maxX = 1f - padding.x;
            float maxY = 1f - padding.y;

            //get the actual position target for the minimum anchors (left/bottom bounds) of the character
            Vector2 minAnchorTarget = new Vector2(maxX * targetPosition.x, maxY * targetPosition.y);
            speed *= Time.deltaTime;

            //move until we reach target position
            while(root.anchorMin != minAnchorTarget)
            {
                root.anchorMin = (!smooth) ? Vector2.MoveTowards(root.anchorMin, minAnchorTarget, speed) : Vector2.Lerp(root.anchorMin, minAnchorTarget, speed);
                root.anchorMax = root.anchorMin + padding;
                yield return new WaitForEndOfFrame();
            }

            StopMoving();
    }

    public Sprite GetSprite(int index = 0)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite> ("Images/Characters/" + characterName);
        return sprites[index];
    }
    public Sprite GetSprite(string spriteName = "")
    {
        Sprite[] sprites = Resources.LoadAll<Sprite> ("Images/Characters/" + characterName);
        for(int i = 0; i < sprites.Length; i++)
        {
            if(sprites[i].name == spriteName)
            {
                return sprites[i];
            }
        }
        return sprites.Length > 0 ? sprites[0] : null;
    }
    
    public void SetBody(int index)
    {
        renderers.bodyRenderer.sprite = GetSprite(index);
    }
    public void SetBody(Sprite sprite)
    {
        renderers.bodyRenderer.sprite = sprite;
    }
    public void SetBody(string spriteName)
    {
        renderers.bodyRenderer.sprite = GetSprite(spriteName);
    }

    bool isTransitioningBody {get{return transitioningBody != null;}}
    Coroutine transitioningBody = null;

    public void TransitionBody(Sprite sprite, float speed, bool smooth)
    {
        StopTransitioningBody();
        transitioningBody = CharacterManager.instance.StartCoroutine(TransitioningBody(sprite, speed, smooth));
    }

    void StopTransitioningBody()
    {
        if(isTransitioningBody)
        {
            CharacterManager.instance.StopCoroutine(transitioningBody);
        }
        transitioningBody = null;
    }

    public IEnumerator TransitioningBody(Sprite sprite, float speed, bool smooth)
	{
		for (int i = 0; i < renderers.allBodyRenderers.Count; i++) 
		{
			Image image = renderers.allBodyRenderers [i];
			if (image.sprite == sprite) 
			{
				renderers.bodyRenderer = image;
				break;
			}
		}

		if (renderers.bodyRenderer.sprite != sprite) 
		{
			Image image = GameObject.Instantiate (renderers.bodyRenderer.gameObject, renderers.bodyRenderer.transform.parent).GetComponent<Image> ();
			renderers.allBodyRenderers.Add (image);
			renderers.bodyRenderer = image;
			image.color = GlobalF.SetAlpha (image.color, 0f);
			image.sprite = sprite;
		}

		while (GlobalF.TransitionImages (ref renderers.bodyRenderer, ref renderers.allBodyRenderers, speed, smooth, true))
			yield return new WaitForEndOfFrame();

		StopTransitioningBody ();
	}
    
    public void Flip()
    {
        root.localScale = new Vector3(root.localScale.x * -1 , 1, 1);
    }

    public bool isFacingLeft {get{return root.localScale.x == 1;}}
    public void FaceLeft()
    {
        root.localScale = Vector3.one;
    }

    public bool isFacingRight {get{return root.localScale.y == -1;}}
    public void FaceRight()
    {
        root.localScale = new Vector3(-1,1,1);
    }

    public void FadeOut(float speed = 3, bool smooth = false)
    {
        if(isEnteringOrExitingScene)
        {
            CharacterManager.instance.StopCoroutine(enteringExiting);
        }

        enteringExiting = CharacterManager.instance.StartCoroutine(ExitingScene(speed, smooth));
    }

    public bool isInScene = false;
    Coroutine enteringExiting = null;
    public bool isEnteringOrExitingScene{get{return enteringExiting != null;}}

    IEnumerator EnteringScene(float speed = 3, bool smooth = false)
    {
        isInScene = true;

        while(canvasGroup.alpha < 1)
        {
            canvasGroup.alpha = smooth ? Mathf.Lerp(canvasGroup.alpha, 1, speed * Time.deltaTime) : Mathf.MoveTowards(canvasGroup.alpha, 1, speed * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }

        enteringExiting = null;
    
    }
    
    IEnumerator ExitingScene(float speed = 3, bool smooth = false)
    {
        isInScene = false;

        while(canvasGroup.alpha > 0)
        {
            canvasGroup.alpha = smooth ? Mathf.Lerp(canvasGroup.alpha, 0, speed * Time.deltaTime) : Mathf.MoveTowards(canvasGroup.alpha, 0, speed * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }

        enteringExiting = null;

        //character is completely faded out and exited the scene. Destroy it so it is no longer saved to file until recalled
        //CharacterManager.instance.DestroyCharacter(this);
    }

    //Sprite lastBodySprite  = null;
    public void FadeIn(float speed = 3, bool smooth = false)
    {
       if(isEnteringOrExitingScene)
       {
           CharacterManager.instance.StopCoroutine(enteringExiting);
       }

       enteringExiting = CharacterManager.instance.StartCoroutine(EnteringScene(speed, smooth));
    }

    public CanvasGroup canvasGroup;
    //Create a new character
    public Character (string _name, bool enableOnStart = true)
    {
        CharacterManager cm = CharacterManager.instance;
        //loacte the character prefab
        GameObject prefab = Resources.Load("Characters/Character[" + _name + "]") as GameObject;
        //spawn an instance of the prefb directly on the character panel
        GameObject ob = GameObject.Instantiate (prefab, cm.characterPanel);

        root = ob.GetComponent<RectTransform> ();
        canvasGroup = ob.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;

        characterName = _name;
        displayName = characterName;

        //get the renderer(s)
        renderers.bodyRenderer = ob.transform.Find("bodyLayer").GetComponentInChildren<Image>();
        renderers.allBodyRenderers.Add(renderers.bodyRenderer);
        
        dialogue = DialogueSystem.instance;

        enabled = enableOnStart;
    }

    [System.Serializable]
    public class Renderers
    {
        //used as the only image for a single layer character
        public Image bodyRenderer;

        public List<Image> allBodyRenderers = new List<Image>();
    }

    public Renderers renderers = new Renderers();
}
