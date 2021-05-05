using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//Responsible for adding and maintaing characterts in the scene
public class CharacterManager : MonoBehaviour
{
    public static CharacterManager instance;

    //All characters must be attached to the character panel
    public RectTransform characterPanel;

    //A list of all characters currently in our scene
    public List<Character> characters = new List<Character>();

    //Easy lookup for the characters
    public Dictionary<string, int> characterDictionary = new Dictionary<string, int>();

    void Awake()
    {
        instance = this;
    }

    //Try to get a character by the name provided from the character list.
    public Character GetCharacter(string characterName, bool createCharacterIfDoesNotExist = true, bool enableCreatedCharacterOnStart = true)
    {
        //search our dictionary to find the character quickly if it is already in our scene
        int index = -1;
        if(characterDictionary.TryGetValue(characterName, out index))
        {
            return characters[index];
        }
        else if (createCharacterIfDoesNotExist)//the character may not have a prefab such as if this is a character who's name is used only
        {
            //ensure the characters exists before trying to load it in
            if(Resources.Load("Characters/Character[" + characterName + "]") != null)
            {
                return CreateCharacter(characterName, enableCreatedCharacterOnStart);
            }
            return null;
        }
        return null;
    }

    //Creates the charcter
    public Character CreateCharacter(string characterName, bool enableOnStart = true)
    {
        Character newCharacter = new Character (characterName, enableOnStart);

        characterDictionary.Add (characterName, characters.Count);
        characters.Add (newCharacter);

        return newCharacter;
    }

    /*
    //Destorys a character in a the scene
    public void DestroyCharacter(Character character)
    {
        if(characters.Contains(character))
        {
            characters.Remove(character);
        }
        characterDictionary.Remove(character.characterName);

        Destroy(character.root.gameObject, 0.01f);
    }

    public void DestroyCharacter(string characterName)
    {
        Character character = GetCharacter(characterName, false, false);
        if(character != null)
        {
            DestroyCharacter(character);
        }
    }

*/
    public class CHARACTERPOSITIONS
    {
        public Vector2 bottomLeft = new Vector2 (0, 0);
        public Vector2 topRight = new Vector2 (1f, 1f);
        public Vector2 center = new Vector2 (0.5f, 0.5f);
        public Vector2 bottomRight = new Vector2 (1f, 0);
        public Vector2 topLeft = new Vector2 (0, 1f);
    }
    public static CHARACTERPOSITIONS characterPositions = new CHARACTERPOSITIONS();
}
