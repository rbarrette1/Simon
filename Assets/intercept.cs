using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

public class intercept : MonoBehaviour
{
    // variables
	public static string cb = ""; // counter balancing group number
	public static string pid = ""; // participant id
    public static float stamp; // stamp to pass in to main script, to account for amount of time it takes to put in pid
    public string[] strArr; // this array is used to split the ##-### into the cb number and pid
    public string highscoreURL = "http://142.51.24.214/tablet/display.php";

    // interface elements
	public InputField input;
	public GameObject GameObject;
    public Text text;
    public Image image;
    
    void Start()
    {
        StartCoroutine(GetScores());
    }
    // load main game once the start game button is clicked
    public void onclick() {
        stamp=Time.time;
        SceneManager.LoadScene("SampleScene");

    }
    IEnumerator GetScores() {
        WWW hs_get = new WWW(highscoreURL);
        yield return hs_get;

        if (hs_get.error != null) {
            print("There was an error getting the high score: " + hs_get.error);
        } else {
            text = GameObject.Find("in").GetComponent<Text>();
            text.text = hs_get.text; // this is a GUIText that will display the scores in game.
            check_input();
        }
    }
    
    // check if the participant id entered by the user matches the same structure as what is expected
    public void check_input() {
        string pattern = @"^\d{2}-\d{3}$"; // regular expression to match ##-###
        if(Regex.IsMatch(text.text.ToString(), pattern)) { // if proper input
            // show start game button
            text =  GameObject.Find("txt_start").GetComponent<Text>();
            text.enabled = true;
            image =  GameObject.Find("start").GetComponent<Image>();
            image.enabled = true;
            // set pid to access in session.cs
            set_pid();
        }
    }
    // set pid to access in session.cs script
    public void set_pid() {
        text = GameObject.Find("in").GetComponent<Text>();
    	strArr = text.text.Split('-');
    	cb = strArr[0];
        print(cb);
    	pid = strArr[1];
        print(pid);
    }
    
}
