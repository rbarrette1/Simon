using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class session : MonoBehaviour {
    // arrays and lists
    public int[,] cb;
    public string[,] sequences;
    public Sprite[] spriteArray = new Sprite[8];
    private static List<string> seq_list = new List<string>();

    // variables
    // ints
    private static int count=1; // which trial the user is on
    public static int round = 0; // which length of sequence the user is on
    private static int input_counter = 0; // number of inputs
    public int correct_count = 0; // number of correct inputs
    public static int id=-1; // counter balanced group number
    private static int seq_count = 0; // which sequence the user is on
    public int list_count=0; // used to prevent multiple posts of the same data to the db

    // floats
    public static float endTime; // time stamp to indicate end of game
    public static float startTime; // time stamp to indicate beginning of game
    public static float timePassed; // amount of time passed, used to update the timer
    public static float stamp=intercept.stamp+3.0f; // to account for different time factors, 3 seconds added for the show of the first sequence
    public static float pausestamp=0.0f; // time stamp to indicate when the game was paused
    public static float posttime=0.0f; // variable to indicate the amount of time that the game was paused
    public static float totaltime=150.0f; // total time the user has per trial

    // strings
    public string stringinline ="";
    public string currentTime;

    // bools
    public static bool og_round = true;
    public static bool start = true;
    public static bool pause = false;
    public static bool incorrect = false;
    public static bool interceptflag = true;
    public static bool endgame = false;

    // interface elements
	public SpriteRenderer spriteRenderer;
    public GameObject GameObject;
    public Button button;
    public Collider collider;
    public Text text;
    public Image image;
    public Slider slider;
    
    void Start() {
        slider =  GameObject.Find("time_slider").GetComponent<Slider>();
        slider.maxValue=150.0f;
        slider.minValue=0.0f;
        intializeCB();// call a method to initialize the array that contains all possible counterbalancing groups
        initializeSequences(); // call a method that initializes the array with all possible sequences
        spriteArray = Resources.LoadAll<Sprite>("sprite_sheet"); // pull in all sprites from my sprite sheet
        id= int.Parse(intercept.cb); // get counterbalancing group from the input field (and implicitly from the intercept script)
        round=0; // ensure round is set to 0
        Time.timeScale = 1.0f;
    }

    void Update() {
        // if the game is paused, continuously disable the box colliders for the input buttons (P1-P4) 
        // and update label so it does not look like it is waiting for the user to enter anything
        if(pause == true) {
            disableColliders();
        }
        // if the game is not paused, let the game play
        if(pause == false) {
            timePassed = Time.time; // take current time since start of game
            // if the total time with other variables taken into account are over totaltime, only show totaltime on interface
            if(((totaltime+stamp)-timePassed) >= totaltime){ 
                updateSlider(totaltime);
            } else {
                updateSlider(((totaltime+stamp) - timePassed));
            }
            
            // if the total time with other variables taken into account are under 0 seconds, reset the trial 
            if(((totaltime+stamp)-timePassed) <= 0.0f){
                hideSeq(7);// hide full sequence of 7
                reset(); // reset variables
                waitandstamp(); // waits 5 seconds and stamps time for new start of trial
                stamp+=3.0f; // 3 seconds added for the show of the first sequence
            }
            // emergency exit if the user must terminate the study, loads the final scene
            if (Input.GetKeyDown("space")) {
                SceneManager.LoadScene("final");
                Application.OpenURL("http://rbarrette1.cs.laurentian.ca/");
                endgame = true;
            }
            // if the game has started but no inputs yet and if the game has not ended
            if(round ==0 && endgame == false) {
                //StartCoroutine(testing());
                startTime = Time.time; // take time stamp to indicate beginning of game
                if(og_round==true) {
                    posttodb(intercept.pid, "", startTime.ToString(), count+"a", "start", 0,"trial_times");
                } else {
                    posttodb(intercept.pid, "", startTime.ToString(), count+"b", "start", 0, "trial_times");
                }
                // this if statement is used so invoke repeating is only called once
                // if this is not here, the invoke repeating would be called a minimum of 4 times, once for every interface
                // element that has its' script attached to it, start true/false ensures it's only called once
                if(start==true) {
                    InvokeRepeating("stim", 2, 7); // if the game has started, invoke the stim method to collect cognitive load data
                    start=false;
                }
                round=1; // allow the script to move onto the first round
                StartCoroutine(showthenhide());
            }
            if(seq_list.Count == 1 && round == 1) { // if 1 input for round 1 has been collected in seq_list
                round=round+1; // move onto the next round
                input_counter=0; // reset input counter, to make it seem like there has been no input. to either start the next round or restart this round
                if(seq_list[0] != sequences[seq_count,0]) { // if a mistake was made, remove last 10 elements and redo last input until correct
                    showInc(0,0); // show the first incorrect input
                    incorrect=true;
                    listtostring(og_round, 0, 0,incorrect); //convert seq_list to string
                    seq_list.RemoveAt(0); // remove incorrect inputs
                    round=1; // since there is an error, revert back to this round
                } else {
                    listtostring(og_round, 0, 0, incorrect); //convert seq_list to string
                }
                StartCoroutine(showthenhide());
            }
            if(seq_list.Count == 3 && round == 2) { // if 2 input for round 2 has been collected in seq_list, for a total of 3 inputs
                round=round+1; // move onto the next round
                input_counter=0; // reset input counter, to make it seem like there has been no input. to either start the next round or restart this round
                for(int i = 1; i <= 2;i++) {
                    if(seq_list[i] != sequences[seq_count,i-1]) { // if a mistake was made, remove last 10 elements and redo last input until correct
                        showInc(i,i); // show the first incorrect input
                        incorrect=true;
                        listtostring(og_round, 1, 2,incorrect); //convert seq_list to string
                        seq_list.RemoveRange(1,2); // remove incorrect inputs
                        round=2; // since there is an error, revert back to this round
                        correct_count=0;
                        break;
                    } else {
                        correct_count=correct_count+1;
                    }
                }
                if(correct_count == 2) {
                    listtostring(og_round, 1, 2, incorrect); //convert seq_list to string
                    correct_count=0;
                }
                StartCoroutine(showthenhide());
            }
            if(seq_list.Count == 6 && round == 3) { // if 3 input for round 3 has been collected in seq_list, for a total of 6 inputs
                round=round+1; // move onto the next round
                input_counter=0; // reset input counter, to make it seem like there has been no input. to either start the next round or restart this round
                for(int i = 3; i <= 5;i++) {
                    if(seq_list[i] != sequences[seq_count,i-3]) { // if a mistake was made, remove last 10 elements and redo last input until correct
                        showInc(i-2,i); // show the first incorrect input
                        incorrect=true;
                        listtostring(og_round, 3,5,incorrect); //convert seq_list to string
                        seq_list.RemoveRange(3,3); // remove incorrect inputs
                        round=3; // since there is an error, revert back to this round
                        correct_count=0;
                        break;
                    } else {
                        correct_count=correct_count+1;
                    }
                }
                if(correct_count == 3) {
                    listtostring(og_round, 3,5, incorrect); //convert seq_list to string
                    correct_count=0;

                }
                StartCoroutine(showthenhide());
            }
            if(seq_list.Count == 10 && round == 4) { // if 4 input for round 4 has been collected in seq_list, for a total of 10 inputs
                round=round+1; // move onto the next round
                input_counter=0; // reset input counter, to make it seem like there has been no input. to either start the next round or restart this round
                for(int i = 6; i <= 9;i++) {
                    if(seq_list[i] != sequences[seq_count,i-6]) { // if a mistake was made, remove last 10 elements and redo last input until correct
                        showInc(i-5,i); // show the first incorrect input
                        incorrect=true;
                        listtostring(og_round, 6,9,incorrect); //convert seq_list to string
                        seq_list.RemoveRange(6,4); // remove incorrect inputs
                        round=4; // since there is an error, revert back to this round
                        correct_count=0;
                        break;
                    } else {
                        correct_count=correct_count+1;
                    }
                }
                if(correct_count == 4) {
                    listtostring(og_round, 6,9, incorrect); //convert seq_list to string
                    correct_count=0;
                }
                StartCoroutine(showthenhide());
            }
            if(seq_list.Count == 15 && round == 5) { // if 5 input for round 5 has been collected in seq_list, for a total of 15 inputs
                round=round+1; // move onto the next round
                input_counter=0; // reset input counter, to make it seem like there has been no input. to either start the next round or restart this round
                for(int i = 10; i <= 14;i++) {
                    if(seq_list[i] != sequences[seq_count,i-10]) { // if a mistake was made, remove last 10 elements and redo last input until correct
                        showInc(i-9,i); // show the first incorrect input
                        incorrect=true;
                        listtostring(og_round, 10,14,incorrect); //convert seq_list to string
                        seq_list.RemoveRange(10,5); // remove incorrect inputs
                        round=5; // since there is an error, revert back to this round
                        correct_count=0;
                        break;
                    } else {
                        correct_count=correct_count+1;
                    }
                }
                if(correct_count == 5) {
                    listtostring(og_round, 10,14, incorrect); //convert seq_list to string
                    correct_count=0;
                }
                StartCoroutine(showthenhide());
            }
            if(seq_list.Count == 21 && round == 6) { // if 6 input for round 6 has been collected in seq_list, for a total of 21 inputs
                round=round+1; // move onto the next round
                input_counter=0; // reset input counter, to make it seem like there has been no input. to either start the next round or restart this round
                for(int i = 15; i <= 20;i++) {
                    if(seq_list[i] != sequences[seq_count,i-15]) { // if a mistake was made, remove last 10 elements and redo last input until correct
                        showInc(i-14,i); // show the first incorrect input
                        incorrect=true;
                        listtostring(og_round, 15,20,incorrect); //convert seq_list to string
                        seq_list.RemoveRange(15,6); // remove incorrect inputs
                        round=6; // since there is an error, revert back to this round
                        correct_count=0;
                        break;
                    } else {
                        correct_count=correct_count+1;
                    }
                }
                if(correct_count == 6) {
                    listtostring(og_round, 15,20, incorrect); //convert seq_list to string
                    correct_count=0;
                }
                StartCoroutine(showthenhide());    
            }
            if(seq_list.Count >=28) { // if 7 input for round 7 has been collected in seq_list, for a total of 28 inputs (max inputs expected)
                for(int i = 21; i <= 27;i++) {
                    if(seq_list[i] != sequences[seq_count,i-21]) { // if a mistake was made, remove last 10 elements and redo last input until correct
                        input_counter=0; // reset input counter, to make it seem like there has been no input. to either start the next round or restart this round
                        showInc(i-20,i); // show the first incorrect input
                        incorrect=true;
                        listtostring(og_round, 21,27,incorrect); //convert seq_list to string
                        seq_list.RemoveRange(21,7); // remove incorrect inputs
                        StartCoroutine(showthenhide());
                        round=7; // since there is an error, revert back to this round
                        correct_count=0;
                        break;
                    } else {
                        correct_count=correct_count+1; // how many correct inputs out of all inputs of the sequence
                        // if last 10 inputs are correct
                        if(correct_count == 7) {
                            listtostring(og_round, 21,27, incorrect);  //convert seq_list to string
                            // if its' any other trial but number 4
                            if(count!=4) {
                                endTime = Time.time-startTime; // take time stamp to indicate end of game
                                // if its trial a
                                if(og_round==true) {
                                    posttodb(intercept.pid, "", Time.time.ToString(), count.ToString()+"a", "end", 0,"trial_times");
                                    posttodb(intercept.pid, "", (endTime-posttime).ToString(), count.ToString()+"a", "total", 0,"trial_times");
                                    // reset time variables
                                    pausestamp=0.0f;
                                    posttime=0.0f;
                                    endTime=0.0f;
                                // if its trial b
                                } else {
                                    posttodb(intercept.pid, "", Time.time.ToString(), count.ToString()+"b", "end", 0,"trial_times");
                                    posttodb(intercept.pid, "", (endTime-posttime).ToString(), count.ToString()+"b", "total", 0,"trial_times");
                                    // reset time variables
                                    pausestamp=0.0f;
                                    posttime=0.0f;
                                    endTime=0.0f;
                                }
                            }
                            reset(); // reset variables
                            StartCoroutine(hideandwait());  
                            // if its trial a
                            if(og_round == true) {
                                og_round=false;
                                updateTrial(); 
                                // if its' trial 4/4a, post to db,, because it will be missed by the other if statement above and below
                                if (count == 4) {
                                    endTime = Time.time-startTime; // take time stamp to indicate end of game  
                                    posttodb(intercept.pid, "", Time.time.ToString(), count.ToString()+"a", "end", 0,"trial_times");
                                    posttodb(intercept.pid, "", (endTime-posttime).ToString(), count.ToString()+"a", "total", 0,"trial_times");
                                    // reset time variables
                                    pausestamp=0.0f;
                                    posttime=0.0f;
                                    endTime=0.0f; 
                                }

                            // if its trial b
                            } else {
                                // if its' trial 4/4b, must pause game and show intercept screen
                                if (count == 4) {
                                    endTime = Time.time-startTime;  
                                    posttodb(intercept.pid, "", Time.time.ToString(), count.ToString()+"b", "end", 0,"trial_times");
                                    posttodb(intercept.pid, "", (endTime-posttime).ToString(), count.ToString()+"b", "total", 0,"trial_times");
                                    // reset time variables
                                    pausestamp=0.0f;
                                    posttime=0.0f;
                                    endTime=0.0f;
                                    pauseGame();
                                    // show intercept button/screen
                                    Image image = GameObject.Find("intercept_button").GetComponent<Image>();
                                    image.enabled=true;
                                    text =  GameObject.Find("intercept_text").GetComponent<Text>();
                                    text.enabled = true;
                                }
                                og_round=true;
                                reset_og(); // reset the interface back to original since a trial has been completed
                                count=count+1; // increase trial count now that a and b have been completed
                                seq_count=seq_count+1; // change sequences
                            }
                            break;
                        }
                    }
                }
            }
        }
    }

    // reset all variables for a trial
    public void reset() {
        hideUnd();
        correct_count = 0;
        round=0;
        input_counter=0; 
        list_count=0;// reset to allow post, used to prevent multiple posts of the same data to the db
        seq_list.Clear();// remove all inputs from seq_list
        stamp=Time.time+3.0f; // add 3 seconds to account for the appearance of the first sequence
        // if its the final trial and b, load the final scene
        if(count==7 && og_round==false) {
            SceneManager.LoadScene("final");
            Application.OpenURL("http://rbarrette1.cs.laurentian.ca/");
            endgame = true;
        }
    }
    // update slider bar/timer
    public void updateSlider(float time) {
        slider =  GameObject.Find("time_slider").GetComponent<Slider>();
        slider.value=time;
    }
    ///////////////////////////////////
    // interface updates
    ///////////////////////////////////
    void updateIntLabels(string element, string toshow) {
        text =  GameObject.Find(element).GetComponent<Text>();
        text.text = toshow;    
    }

    // show sequence in original format
    // show only the length of sequence as the round number
    // if its round 2, only show 2
    void showSeq(int round) {
        for (int i = 1; i <= round; i++) {
            image =  GameObject.Find("S"+i).GetComponent<Image>();

            if(sequences[seq_count,i-1] == "1") {
                image.sprite = spriteArray[4];
            } else if (sequences[seq_count,i-1] == "2") {
                image.sprite = spriteArray[5];
            } else if (sequences[seq_count,i-1] == "3") {
                image.sprite = spriteArray[6];
            } else if (sequences[seq_count,i-1] == "4") {
                image.sprite = spriteArray[7];
            }
            image =  GameObject.Find("U"+i).GetComponent<Image>();
            image.enabled=true;
        }
    }

    // hide sequences
    // hide only the length of sequence as the round number, should hide everything that is there
    // if its round 2, only hide 2
    void hideSeq(int round) {
        for (int i = 1; i <= round; i++) {
            image =  GameObject.Find("S"+i).GetComponent<Image>();
            image.sprite = null; 
        }
    }
    // hide underline
    // hide only the length of sequence as the round number, should hide everything that is there
    // if its round 2, only hide 2
    void hideUnd() {
        for (int i = 1; i <= 7; i++) {
            image =  GameObject.Find("U"+i).GetComponent<Image>();
            image.enabled=false;
        }
    }

    // disable input buttons from being clicked
    void disableColliders() {
        for (int i = 1; i <= 4; i++) {
            button =  GameObject.FindWithTag("P"+i).GetComponent<Button>();
            button.enabled = false;
        }
    }

    // enable input buttons from being clicked
    void enableColliders() {
        for (int i = 1; i <= 4; i++) {
            button =  GameObject.FindWithTag("P"+i).GetComponent<Button>();
            button.enabled = true;
        }
        input_counter=0;
    }

   // change interface to the change according to trial
    void updateTrial() {
        // CHANGES FOR GAME 1
        if(count <= 4 && cb[id,count-1] == 1) {
            // changing sprite image
            for (int i = 1; i <= 4; i++) {
                image =  GameObject.FindWithTag("P"+i).GetComponent<Image>();
                image.sprite = spriteArray[i-1];
            }
        // CHANGES FOR GAME 2
        } else if (count <= 4 && cb[id,count-1] == 2) {
            // changing sprite positions
             image = GameObject.FindWithTag("P2").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-520,130);
             image = GameObject.FindWithTag("P3").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-270,-140);
             image = GameObject.FindWithTag("P4").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-525,-410);
            /*
            GameObject GameObject = GameObject.FindWithTag("P2");
            Vector3 pos = GameObject.transform.position;
            pos.x += (float)40.0;
            pos.y += (float)90.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P3");
            pos = GameObject.transform.position;
            pos.x += (float)90.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P4");
            pos = GameObject.transform.position;
            pos.y -= (float)90.0;
            pos.x += (float)35.0;
            GameObject.transform.position = pos;
            */

        // CHANGES FOR GAME 3
        } else if (count <= 4 && cb[id,count-1] == 3) {
            // changing sprite positions
             image = GameObject.FindWithTag("P1").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-760,60);
             image = GameObject.FindWithTag("P2").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-360,60);
             image = GameObject.FindWithTag("P3").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-360,-340);
             image = GameObject.FindWithTag("P4").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-760,-340);

            /*
            GameObject GameObject = GameObject.FindWithTag("P1");
            Vector3 pos = GameObject.transform.position;
            pos.y += (float)200.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P2");
            pos = GameObject.transform.position;
            pos.x += (float)200.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P3");
            pos = GameObject.transform.position;
            pos.y -= (float)200.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P4");
            pos = GameObject.transform.position;
            pos.x -= (float)200.0;
            GameObject.transform.position = pos;
            */
        // CHANGES FOR GAME 4
        } else if (count <= 4  && cb[id,count-1] == 4) {
            // change sprite labels
            image =  GameObject.FindWithTag("P1").GetComponent<Image>();
            image.sprite = spriteArray[7];
            image.tag = ("P4");
            image =  GameObject.FindWithTag("P2").GetComponent<Image>();
            image.sprite = spriteArray[4];
            image.tag = ("P1");
            image =  GameObject.FindWithTag("P3").GetComponent<Image>();
            image.sprite = spriteArray[5];
            image.tag = ("P2");
            image =  GameObject.FindWithTag("P4").GetComponent<Image>();
            image.sprite = spriteArray[6];
            image.tag = ("P3");

        // CHANGES FOR GAME 5
        } else if (count == 5) {
            // changing sprite positions
             image = GameObject.FindWithTag("P2").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-520,130);
             image = GameObject.FindWithTag("P3").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-270,-140);
             image = GameObject.FindWithTag("P4").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-525,-410);
            /*
            GameObject GameObject = GameObject.FindWithTag("P2");
            Vector3 pos = GameObject.transform.position;
            pos.x += (float)40.0;
            pos.y += (float)90.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P3");
            pos = GameObject.transform.position;
            pos.x += (float)90.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P4");
            pos = GameObject.transform.position;
            pos.y -= (float)90.0;
            pos.x += (float)35.0;
            GameObject.transform.position = pos;
            */
            // changing sprite image
            for (int i = 1; i <= 4; i++) {
                image =  GameObject.FindWithTag("P"+i).GetComponent<Image>();
                image.sprite = spriteArray[i-1];

            }
        // CHANGES FOR GAME 6
        } else if (count == 6) {
            // changing sprite positions
             image = GameObject.FindWithTag("P1").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-760,150);
             image = GameObject.FindWithTag("P2").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-270,150);
             image = GameObject.FindWithTag("P3").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-270,-340);
             image = GameObject.FindWithTag("P4").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-760,-340);
            /*
            GameObject GameObject = GameObject.FindWithTag("P1");
            Vector3 pos = GameObject.transform.position;
            pos.y += (float)200.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P2");
            pos = GameObject.transform.position;
            pos.x += (float)200.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P3");
            pos = GameObject.transform.position;
            pos.y -= (float)200.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P4");
            pos = GameObject.transform.position;
            pos.x -= (float)200.0;
            GameObject.transform.position = pos;
            */
            // changing sprite image
            for (int i = 1; i <= 4; i++) {
                image =  GameObject.FindWithTag("P"+i).GetComponent<Image>();
                image.sprite = spriteArray[i-1];
            }

        // CHANGES FOR GAME 7
        } else if (count == 7) {
            // changing sprite positions
             image = GameObject.FindWithTag("P1").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-760,150);
             image = GameObject.FindWithTag("P2").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-270,150);
             image = GameObject.FindWithTag("P3").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-270,-340);
             image = GameObject.FindWithTag("P4").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-760,-340);
            /*
            GameObject GameObject = GameObject.FindWithTag("P1");
            Vector3 pos = GameObject.transform.position;
            pos.y += (float)290.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P2");
            pos = GameObject.transform.position;
            pos.x += (float)290.0;
            pos.y += (float)90.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P3");
            pos = GameObject.transform.position;
            pos.x += (float)90.0;
            pos.y -= (float)200.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P4");
            pos = GameObject.transform.position;
            pos.x -= (float)200.0;
            GameObject.transform.position = pos;
            */
            // changing sprite tags
            image =  GameObject.FindWithTag("P1").GetComponent<Image>();
            image.sprite = spriteArray[3];
            image.tag = ("P4");
            image =  GameObject.FindWithTag("P2").GetComponent<Image>();
            image.sprite = spriteArray[0];
            image.tag = ("P1");
            image =  GameObject.FindWithTag("P3").GetComponent<Image>();
            image.sprite = spriteArray[1];
            image.tag = ("P2");
            image =  GameObject.FindWithTag("P4").GetComponent<Image>();
            image.sprite = spriteArray[2];
            image.tag = ("P3");
        }
    }

    // reset interface to original format, according to the current change
    void reset_og() {
        // reset sprites to non bold everytime
        for (int i = 4; i <= 7; i++) {
            image =  GameObject.Find("P"+(i-3)).GetComponent<Image>();
            image.sprite = spriteArray[i];
            image.tag = ("P"+(i-3));
        }
        // REVERT CHANGES FOR GAME 2
        if (count <= 4 && cb[id,count-1] == 2) {
            // changing sprite positions
             image = GameObject.FindWithTag("P1").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-760,-140);
             image = GameObject.FindWithTag("P2").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-560,60);
             image = GameObject.FindWithTag("P3").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-360,-140);
             image = GameObject.FindWithTag("P4").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-560,-340);
            /*
            GameObject GameObject = GameObject.FindWithTag("P2");
            Vector3 pos = GameObject.transform.position;
            pos.x -= (float)40.0;
            pos.y -= (float)90.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P3");
            pos = GameObject.transform.position;
            pos.x -= (float)90.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P4");
            pos = GameObject.transform.position;
            pos.y += (float)90.0;
            pos.x -= (float)35.0;
            GameObject.transform.position = pos;
            */
        // REVERT CHANGES FOR GAME 3
        } else if (count <= 4 && cb[id,count-1] == 3) {
            // changing sprite positions
             image = GameObject.FindWithTag("P1").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-760,-140);
             image = GameObject.FindWithTag("P2").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-560,60);
             image = GameObject.FindWithTag("P3").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-360,-140);
             image = GameObject.FindWithTag("P4").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-560,-340);
            /*
            GameObject GameObject = GameObject.FindWithTag("P1");
            Vector3 pos = GameObject.transform.position;
            pos.y -= (float)200.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P2");
            pos = GameObject.transform.position;
            pos.x -= (float)200.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P3");
            pos = GameObject.transform.position;
            pos.y += (float)200.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P4");
            pos = GameObject.transform.position;
            pos.x += (float)200.0;
            GameObject.transform.position = pos;
            */
        // REVERT CHANGES FOR GAME 4
        } else if (count == 5) {
            // changing sprite positions
             image = GameObject.FindWithTag("P1").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-760,-140);
             image = GameObject.FindWithTag("P2").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-560,60);
             image = GameObject.FindWithTag("P3").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-360,-140);
             image = GameObject.FindWithTag("P4").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-560,-340);
            /*
            GameObject GameObject = GameObject.FindWithTag("P2");
            Vector3 pos = GameObject.transform.position;
            pos.x -= (float)40.0;
            pos.y -= (float)90.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P3");
            pos = GameObject.transform.position;
            pos.x -= (float)90.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P4");
            pos = GameObject.transform.position;
            pos.y += (float)90.0;
            pos.x -= (float)35.0;
            GameObject.transform.position = pos;
            */

        // REVERT CHANGES FOR GAME 6
        } else if (count == 6) {
            // changing sprite positions
             image = GameObject.FindWithTag("P1").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-760,-140);
             image = GameObject.FindWithTag("P2").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-560,60);
             image = GameObject.FindWithTag("P3").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-360,-140);
             image = GameObject.FindWithTag("P4").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-560,-340);
            /*
            GameObject GameObject = GameObject.FindWithTag("P1");
            Vector3 pos = GameObject.transform.position;
            pos.y -= (float)200.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P2");
            pos = GameObject.transform.position;
            pos.x -= (float)200.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P3");
            pos = GameObject.transform.position;
            pos.y += (float)200.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P4");
            pos = GameObject.transform.position;
            pos.x += (float)200.0;
            GameObject.transform.position = pos;
            */
        // REVERT CHANGES FOR GAME 7
        } else if (count == 7) {
            // changing sprite positions
             image = GameObject.FindWithTag("P1").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-760,-140);
             image = GameObject.FindWithTag("P2").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-560,60);
             image = GameObject.FindWithTag("P3").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-360,-140);
             image = GameObject.FindWithTag("P4").GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(-560,-340);
            /*
            GameObject GameObject = GameObject.FindWithTag("P1");
            Vector3 pos = GameObject.transform.position;
            pos.y -= (float)290.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P2");
            pos = GameObject.transform.position;
            pos.x -= (float)290.0;
            pos.y -= (float)90.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P3");
            pos = GameObject.transform.position;
            pos.x -= (float)90.0;
            pos.y += (float)200.0;
            GameObject.transform.position = pos;

            GameObject = GameObject.FindWithTag("P4");
            pos = GameObject.transform.position;
            pos.x += (float)200.0;
            GameObject.transform.position = pos;
            */
            // changing sprites to correct tags
            image =  GameObject.FindWithTag("P1").GetComponent<Image>();
            image.sprite = spriteArray[7];
            image =  GameObject.FindWithTag("P2").GetComponent<Image>();
            image.sprite = spriteArray[4];
            image =  GameObject.FindWithTag("P3").GetComponent<Image>();
            image.sprite = spriteArray[5];
            image =  GameObject.FindWithTag("P4").GetComponent<Image>();
            image.sprite = spriteArray[6];
        }
    }

    // show incorrect sequences
    // i is the index for finding the sequence buttons to update
    // ii is the index for seq_list, the inputs currently being investigated
    void showInc(int i, int ii) {
        // if only investigating the first input
        if(i==0){

            if(seq_list[i] == "1") {
                image =  GameObject.Find("S1").GetComponent<Image>();
                image.sprite = spriteArray[0];
            } else if (seq_list[i] == "2") {
                image =  GameObject.Find("S1").GetComponent<Image>();
                image.sprite = spriteArray[1];
            } else if (seq_list[i] == "3") {
                image =  GameObject.Find("S1").GetComponent<Image>();
                image.sprite = spriteArray[2];
            } else if (seq_list[i] == "4") {
                image =  GameObject.Find("S1").GetComponent<Image>();
                image.sprite = spriteArray[3];
            }
        // if investigating more than 1 input, trials 2+
        } else {
            if(seq_list[ii] == "1") {
                image =  GameObject.Find("S"+i).GetComponent<Image>();
                image.sprite = spriteArray[0];
            } else if (seq_list[ii] == "2") {
                image =  GameObject.Find("S"+i).GetComponent<Image>();
                image.sprite = spriteArray[1];
            } else if (seq_list[ii] == "3") {
                image =  GameObject.Find("S"+i).GetComponent<Image>();
                image.sprite = spriteArray[2];

            } else if (seq_list[ii] == "4") {
                image =  GameObject.Find("S"+i).GetComponent<Image>();
                image.sprite = spriteArray[3];
            }
        }
    }

    ///////////////////////////////////
    // button - on click functions
    ///////////////////////////////////
    // this method is called by InvokeRepeating.
    // this method is to show the stim button to allow the user to respond
    void stim() {
        text =  GameObject.Find("stim_text").GetComponent<Text>();
        text.enabled = true;
        image =  GameObject.Find("stim").GetComponent<Image>();
        image.enabled = true;

        currentTime = Time.time.ToString("f6"); // time stim appeared
        // if trial a, add as so in db
        if(og_round == true) {
            posttodb(intercept.pid, "", currentTime, count.ToString()+"a", "app", 0,"stim_times");
        // if trial b, add as so in db
        } else {
            posttodb(intercept.pid, "", currentTime, count.ToString()+"b", "app", 0,"stim_times");
        }
    }

    // this method is called when the stim button is called
    public void stim_onclick() {
        text =  GameObject.Find("stim_text").GetComponent<Text>();
        text.enabled = false;
        image =  GameObject.Find("stim").GetComponent<Image>();
        image.enabled = false;

        currentTime = Time.time.ToString("f6"); // time stim was responded  
        // if trial a, add as so in db  
        if(og_round == true) {
            posttodb(intercept.pid, "", currentTime, count.ToString()+"a", "res", 0,"stim_times");
        // if trial b, add as so in db
        } else {
            posttodb(intercept.pid, "", currentTime, count.ToString()+"b", "res", 0,"stim_times");
        }
    }

    // this method is called when the intercept button (yellow screen) is clicked
    public void intercept_onclick() {
        interceptflag=false; // to prevent showAfterUnpaused() to be called
        pauseGame();
        // find intercept button
        Image image = GameObject.Find("intercept_button").GetComponent<Image>();
        // hide intercept button
        image.enabled=false;
        text =  GameObject.Find("intercept_text").GetComponent<Text>();
        text.enabled = false;
        interceptflag=true; // to prevent showAfterUnpaused() to be called
        pausestamp=0.0f; // to fix 5a negative
        posttime=0.0f; // to fix 5a negative
    }

    // when P1-P4 is clicked, this method is called
    public void OnMouseDown() {

        input_counter=input_counter+1;
        if(this.GetComponent<Button>().tag == "P1") {
            seq_list.Add("1");
            image =  GameObject.Find("S"+input_counter).GetComponent<Image>();
            image.sprite = spriteArray[4];
        } else if (this.GetComponent<Button>().tag == "P2") {
            seq_list.Add("2");
            image =  GameObject.Find("S"+input_counter).GetComponent<Image>();
            image.sprite = spriteArray[5];
        } else if (this.GetComponent<Button>().tag == "P3") {
            seq_list.Add("3");
            image =  GameObject.Find("S"+input_counter).GetComponent<Image>();
            image.sprite = spriteArray[6];
        } else if (this.GetComponent<Button>().tag == "P4") {
            seq_list.Add("4");
            image =  GameObject.Find("S"+input_counter).GetComponent<Image>();
            image.sprite = spriteArray[7];
        }
    }

    // if the pause button is clicked, this method is called
    public void pauseGame() {
        // the pause variable starts as false
        // when this is called the first time it first passes through "else" 
        if(pause==true) {
            updateIntLabels("pause_text", ("PAUSE"));
            // accounts for pause time in timer
            stamp=stamp+(Time.time-pausestamp);
            if(interceptflag==true) { // to ensure showAfterUnpaused() is be called
                StartCoroutine(showAfterUnpaused());
            }
            // the amount of time paused
            posttime=Time.time-pausestamp;
            // if trial a
            if(og_round==true) {
                posttodb(intercept.pid, "", (posttime).ToString(), count+"a", "paused", 0,"trial_times");
            // if trial b
            } else {
                posttodb(intercept.pid, "", (posttime).ToString(), count+"b", "paused", 0, "trial_times");
            }            
            // changes pause variable so when it's passed through again, it goes through the other if statement
            pause=false;
            enableColliders(); // enables inputs
        } else {
            disableColliders(); // disables inputs
            // sets text to make the game seem frozen
            updateIntLabels("pause_text", ("PAUSED"));
            // takes timestamps
            pausestamp=Time.time;
            // changes pause variable so when it's passed through again, it goes through the other if statement
            pause=true;
        }
    }

    ///////////////////////////////////
    // IEnumerator - wait functions
    ///////////////////////////////////
    // method called after every round to show and hide sequence to input
    IEnumerator showthenhide() {
        list_count=0;// reset to allow post, used to prevent multiple posts of the same data to the db
        disableColliders(); // disable inputs while showing sequence
        yield return new WaitForSeconds(3);
        showSeq(round);
        yield return new WaitForSeconds(3);
        hideSeq(round);
        enableColliders(); // enable inputs
    }

    // waits 5 seconds and stamps time for new start of trial
    IEnumerator waitandstamp() {
        stamp=Time.time;
        yield return new WaitForSeconds(5);
    }

    // once a full correct sequence is inputted, hide the seq and wait for reset, to show the new sequence of the next trial
    IEnumerator hideandwait() {
        list_count=0;// reset to allow post, used to prevent multiple posts of the same data to the db
        hideSeq(7); // hide 7 sequence items
        yield return new WaitForSeconds(5); // wait 5 seconds
    }

    // method to show sequence after the game being unpaused
    IEnumerator showAfterUnpaused() {
        showSeq(session.round);
        yield return new WaitForSeconds(5);
        hideSeq(session.round);
    }

    // method to test the different changes, ensure they are displaying properly
    IEnumerator testing() {
        // add for loop with range of changes to test
        for(int i = 1; i<=7; i++) {
            count=i;
            print(count);
            updateTrial(); // make appropriate changes for that trial
            yield return new WaitForSeconds(5);
            reset_og();// reset to the original interface
        }
       
    }

    ///////////////////////////////////
    // METHODS TO SEND INFORMATION TO THE DATABASE
    ///////////////////////////////////
    void posttodb(string name, string sequence, string timestamp, string trialnum, string type, int changetype, string tablename) {
    	StartCoroutine(Post(name, sequence, timestamp, trialnum, type, changetype, tablename));
    }

    IEnumerator Post(string name, string sequence, string timestamp, string trialnum, string type, int changetype, string tablename) {
		string post_url = "http://142.51.24.214/exp/addscore.php?";
		if(tablename == "seq_entered") {
			// POST TO SEQ_ENTERED
	    	post_url = post_url + "&name=" + name + "&sequence=" + sequence +  "&trialnum=" + trialnum + "&type=" + type +  "&changetype=" + changetype + "&tablename=" + tablename;

    	} else if (tablename == "stim_times") {
			// POST TO STIM_TIMES
	    	post_url = post_url + "&name=" + name + "&ts=" + timestamp +  "&trialnum=" + trialnum + "&type=" + type + "&tablename=" + tablename; 

    	} else if (tablename == "trial_times") {
			// POST TO TRIAL_TIMES
			post_url = post_url + "&name=" + name + "&ts=" + timestamp +  "&trialnum=" + trialnum + "&type=" + type + "&tablename=" + tablename; 
    	}
	    //print(post_url);
	    WWW hs_post = new WWW(post_url);
	    yield return hs_post; 
	    if (hs_post.error != null)
	    {
	        Debug.Log("There was an error posting the high score: " + hs_post.error);
	    }
	}

    // converts seq_list to a string that is postable to the db
    public string listtostring(bool og_round, int index1, int index2, bool inc) {
        stringinline="";// reset this variable so the previous time it was posted, no longer existss
        list_count=list_count+1;// used to prevent multiple posts of the same data to the db
        // add elements of seq_list to string
		for(int i = index1; i <= index2; i++) {
			stringinline += seq_list[i];
		}
        // if trial a
        if(og_round==true && list_count==1) {
            if(inc==true) { // if the sequence was incorrect
                if(count<=4) { // if trial 4 or below use cb array to identify the type/ number of change
                    posttodb(intercept.pid, stringinline, "", count.ToString()+"a", "incorrect", cb[id,count-1], "seq_entered");
                } else { // if its trial 5+, post the trial number for the type of change
                    posttodb(intercept.pid, stringinline, "", count.ToString()+"a", "incorrect", count, "seq_entered");                    
                }
            } else {// if the sequence was correct
                if(count<=4) { // if trial 4 or below use cb array to identify the type/ number of change
                    posttodb(intercept.pid, stringinline, "", count.ToString()+"a", "correct",cb[id,count-1], "seq_entered");

                } else { // if its trial 5+, post the trial number for the type of change
                    posttodb(intercept.pid, stringinline, "", count.ToString()+"a", "correct",count, "seq_entered");
                }
            }
        // if trial b
        } else if(og_round==false && list_count==1) {

            if(inc==true) {// if the sequence was incorrect
                if(count<=4){ // if trial 4 or below use cb array to identify the type/ number of change
                    posttodb(intercept.pid, stringinline, "", count.ToString()+"b", "incorrect", cb[id,count-1], "seq_entered");
                } else { // if its trial 5+, post the trial number for the type of change
                    posttodb(intercept.pid, stringinline, "", count.ToString()+"b", "incorrect", count, "seq_entered");
                }
            } else {// if the sequence was correct
                if(count<=4) { // if trial 4 or below use cb array to identify the type/ number of change
                    posttodb(intercept.pid, stringinline, "", count.ToString()+"b", "correct", cb[id,count-1], "seq_entered");
                } else { // if its trial 5+, post the trial number for the type of change
                    posttodb(intercept.pid, stringinline, "", count.ToString()+"b", "correct", count, "seq_entered");
                }
            }        
        }
        // reset incorrect flag
        incorrect=false;
        print(list_count);
		return stringinline;
    }

    ///////////////////////////////////
	// methods to be called at the start
    // initialization methods
    ///////////////////////////////////
    // initialize sequences
    void initializeSequences() {
    	sequences = new string[,] {{"2","3","2","4","1","2","3"},
                                {"2","3","1","2","3","4","2"},
                                {"4","3","2","1","3","2","4"},
                                {"3","2","1","3","2","3","3"},
                                {"4","3","2","1","4","1","1"},
                                {"3","1","3","1","4","3","4"},
                                {"2","1","3","2","3","3","1"}};
    }

    // initialize counterbalancing groups
    void intializeCB() {
    	cb = new int[,] {{1,2,3,4}, {1,2,4,3}, {1,4,2,3}, {4,1,2,3}, 
    					{1,3,2,4}, {1,3,4,2}, {1,4,3,2}, {4,1,3,2}, 
    					{3,1,2,4}, {3,1,4,2}, {3,4,1,2}, {4,3,1,2}, 
    					{2,1,3,4}, {2,1,4,3}, {2,4,1,3}, {4,2,1,3}, 
    					{2,3,1,4}, {2,3,4,1}, {2,4,3,1}, {4,2,3,1}, 
    					{3,2,1,4}, {3,2,4,1}, {3,4,2,1}, {4,3,2,1}};
    }
}