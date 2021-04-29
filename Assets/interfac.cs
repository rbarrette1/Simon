using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class interfac : MonoBehaviour
{
	public int[,] cb;
	public string[,] sequences;
	public Sprite[] spriteArray = new Sprite[8];
	public SpriteRenderer spriteRenderer;
	public GameObject GameObject;
    public Image image;
    
    void Start() {
            // when game starts, initialize the sequence for the first time
            // this will only be called once because only the UI Manager has this script
            spriteArray = Resources.LoadAll<Sprite>("sprite_sheet");
            for (int i = 4; i <= 7; i++) {
    			image =  GameObject.Find("P"+(i-3)).GetComponent<Image>();
    			image.sprite = spriteArray[i];
    		}
      

    }

}
