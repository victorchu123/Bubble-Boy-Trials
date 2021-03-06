﻿using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityStandardAssets._2D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PlatformPlayer : MonoBehaviour {

    public float m_repeat_damage_period= .25f; // how frequently the player can be damaged.
    public float m_health = 100f; // the player's m_health
    public AudioClip[] m_ouch_clips;               // Array of clips to play when the player is damaged.
    public float m_hurt_force = 1000f;               // The force with which the player is pushed when hurt.
    public float m_damage_amount = 20f;            // The amount of damage to take when enemies touch the player
    public float hit_height = 10.0f; //height at which player will hit the enemy's head 
    public bool is_dead = false;
    public Text score_text; 
    public bool collided = false;
    public bool can_open_door = false;
    public GameObject health_bar;
    public AnimationClip enemy1_hit;
    public AnimationClip enemy2_hit;
    public AnimationClip player_defend;

    private int m_lives = 3; //player's remaining lives
    private int m_score = 0; //player's current score
    private int m_num_combos = 0; // player's current number of combos
    private bool m_touched_head = false;
    public bool m_touched_door = false;
    private bool m_touched_chest = false;
    private float m_last_hit_time; // the time at which the player was last hit.
    private float m_score_penalty = .50f; // decimal percentage the player's score is reduced after dying
    private float cast_radius = .1f;
    private EnemySpawner m_spawner;
    private SpriteRenderer m_health_bar;           // Reference to the sprite renderer of the m_health bar.
    private Vector3 m_health_scale;                // The local scale of the m_health bar initially (with full m_health).
    private Animator m_door_anim;                      // Reference to the animator on the door
    private Animator m_player_anim;                     // Reference to the animator on the player
    private Door m_door;
    private Animator m_emy_anim;
    private Platformer2DUserControl player_control;
    private PlatformerCharacter2D plat_char;
    private Weapon weapon;
    private AudioSource[] player_source;
    private AudioSource enemy1_source;
    private AudioSource enemy2_source;
    private PlatformLevel platform_lvl;
    private Animator treasure_anim;
    private bool facing_dir;
    private float jump_force;
    private Rigidbody2D player_rg2d;

    private float[] respawn_x_positions = new float[]{-11.1f};
    private float[] respawn_y_positions = new float[]{15.7f};

    private Dictionary<float, float> respawn_positions = new Dictionary<float, float>();

    public void SetCollide (bool did_collide)
    {
        collided = did_collide;
    }

    void InitializeRespawnDict ()
    {  
        int arr_length = respawn_x_positions.Length;
        for (int i = 0; i < arr_length; i++)
        {   
            respawn_positions.Add (respawn_x_positions[i],respawn_y_positions[i]);
        }
    }

    // adds given position to the dictionary
    public void AddToDict (float x_pos, float y_pos)
    {
        if (!respawn_positions.ContainsKey(x_pos))
        {    
            respawn_positions.Add(x_pos, y_pos);
        }
    }

    private bool IsInDictionary (float x_pos)
    {
        return (respawn_positions.ContainsKey(x_pos));
    }

    void Awake ()
    {
        // Setting up references.
        m_spawner = Camera.main.GetComponent<EnemySpawner>(); // need to set this back to Camera.current for scene integration
        m_player_anim = this.gameObject.GetComponent<Animator>();
        m_door = GameObject.Find("BossDoor").GetComponent<Door>();
        m_door_anim = GameObject.Find("BossDoor").GetComponent<Animator>();
        player_control = this.gameObject.GetComponent<Platformer2DUserControl>();
        plat_char = this.gameObject.GetComponent<PlatformerCharacter2D>();
        weapon = this.gameObject.GetComponent<Weapon>();
        player_source = this.gameObject.GetComponents<AudioSource>();
        platform_lvl = GameObject.Find("PlatformLevel").GetComponent<PlatformLevel>();
        facing_dir = this.gameObject.GetComponent<PlatformerCharacter2D>().m_FacingRight;
        treasure_anim = GameObject.Find("treasure").GetComponent<Animator>();
        jump_force = plat_char.m_JumpForce;
        player_rg2d = this.gameObject.GetComponent<Rigidbody2D>();
        InitializeRespawnDict();
    }

    void Start()
    {       
        health_bar = GameObject.Instantiate(Resources.Load("HealthBar"), new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
        health_bar.transform.SetParent(GameObject.Find("Canvas").GetComponent<RectTransform>(), false);
        health_bar.GetComponent<Slider>().value = m_health;
    }

    private void RespawnPlayer()
    {   
        // Find all of the colliders on the gameobject and set them all to not be triggers
        Collider2D[] cols = GetComponents<Collider2D>();
        foreach(Collider2D c in cols)
        {
            c.isTrigger = false;
        }

        // Move all sprite parts of the player to the back
        SpriteRenderer[] spr = GetComponentsInChildren<SpriteRenderer>();
        foreach(SpriteRenderer s in spr)
        {
            s.sortingLayerName = "Character";
        }
            
        GetComponent<Platformer2DUserControl>().enabled = true;
        GetComponentInChildren<Weapon>().enabled = true;
        health_bar.SetActive(true);
        m_health = 100f;
        health_bar.GetComponent<Slider>().value = m_health;
        m_score -= (int) (m_score * m_score_penalty);

        Vector2 player_pos = this.gameObject.transform.position;

        float closest_key = -11.1f;

        foreach (KeyValuePair<float, float> pair in respawn_positions)
        {
            if ((Mathf.Abs(player_pos.x - pair.Key) < Mathf.Abs(player_pos.x - closest_key)) && IsInDictionary(pair.Key))
            {   
                closest_key = pair.Key;
            }
        }

        this.gameObject.transform.position = new Vector3 (closest_key, respawn_positions[closest_key], 0f);
    }

    //increases score by the increment number
    public void GainScore (int increment)
    {
//        m_num_combos++;-
        m_score += increment /* * m_num_combos*/;
    }
   
	// Update is called once per frame
	void Update () 
    {   
        Debug.DrawRay(this.transform.Find("CastOrigin").transform.position, Vector3.down);
        score_text.text = "Score: " + m_score;
        if (this.gameObject.transform.position.y <= 5 
            || m_lives <= 0)
        {
            RespawnPlayer();
        }
        // canvas is null when in platformer mode
        GameObject canvas = GameObject.Find("Canvas");
        if (health_bar != null && canvas != null)
        {
            // centers the health bar above the player
            health_bar.transform.SetParent(GameObject.Find("Canvas").GetComponent<RectTransform>(), false);
            RectTransform CanvasRect = canvas.GetComponent<RectTransform>();
            Vector2 ViewportPos = Camera.main.WorldToViewportPoint(transform.position);

            Vector2 ScreenPos = new Vector2(
                                    (ViewportPos.x * CanvasRect.sizeDelta.x) - (CanvasRect.sizeDelta.x * 0.5f),
                                    (ViewportPos.y * CanvasRect.sizeDelta.y) - (CanvasRect.sizeDelta.y * 0.5f)
                                );

            health_bar.GetComponent<RectTransform>().anchoredPosition = ScreenPos + new Vector2(0, 100);
        }

        // Find all of the colliders on the gameobject and set them all to be triggers.
        if (m_health <= 0f)
        {
            Collider2D[] cols = GetComponents<Collider2D>();
            foreach(Collider2D c in cols)
            {
                c.isTrigger = true;
            }

            // Move all sprite parts of the player to the front
            SpriteRenderer[] spr = GetComponentsInChildren<SpriteRenderer>();
            foreach(SpriteRenderer s in spr)
            {
                s.sortingLayerName = "UI";
            }

            GetComponent<Platformer2DUserControl>().enabled = false;
            GetComponentInChildren<Weapon>().enabled = false;
            RespawnPlayer();
        }
	}

    public void HurtEnemy(GameObject curr_enemy, int score_increase)
    {
        transform.Rotate(Vector2.left);

        m_emy_anim = curr_enemy.GetComponentInChildren<Animator>();
        EnemyMovement movement = curr_enemy.GetComponent<EnemyMovement>();
        curr_enemy.transform.GetComponent<Rigidbody2D>().constraints = (RigidbodyConstraints2D.FreezePositionX |
                                                                  RigidbodyConstraints2D.FreezePositionY) ;
        movement.m_can_move = false;
        m_emy_anim.SetTrigger("Hit");

        float curr_length;

        if (curr_enemy.name == "enemy1(Clone)")
        {
            curr_length = enemy1_hit.length/5;
        } 
        else
        {
            curr_length = enemy2_hit.length/2;
        }

        GainScore(score_increase);
        StartCoroutine(DelayHeadHit(curr_enemy, curr_length));
    }

    IEnumerator DelayHeadHit (GameObject curr_enemy, float curr_length)
    {   
        yield return new WaitForSeconds(curr_length);
        m_touched_head = false;
        collided = false;
        Destroy(curr_enemy);
    }

    public void RenderPlayerImmobile()
    {
        m_player_anim.SetFloat("Speed",0);
        this.gameObject.transform.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        player_control.m_can_move = false;
        weapon.can_attack = false;
    }

    void FixedUpdate()
    {   
        float player_y_velocity = this.gameObject.GetComponent<Rigidbody2D>().velocity.y;

        GameObject cast_origin = GameObject.Find("CastOrigin");

        RaycastHit2D[] hit = Physics2D.CircleCastAll(cast_origin.transform.position, cast_radius, Vector2.down, hit_height, 1 << 13);
        if (!m_touched_head)
        {
            if (hit != null)
            {
                foreach (RaycastHit2D collider_hit in hit)
                {
                    if(collider_hit.transform.tag == "Enemy")
                    {
                        m_touched_head = true;
                        collided = true;
                        this.gameObject.GetComponent<Rigidbody2D>().AddForce(new Vector2(0, .1f * jump_force *  Mathf.Abs(player_y_velocity)));
//                        this.gameObject.GetComponent<Rigidbody2D>().velocity = this.gameObject.GetComponent<Rigidbody2D>().velocity 
//                                                                               + new Vector2(0, 20f);
                        GameObject parent_enemy = collider_hit.transform.root.gameObject;
                        m_spawner.RemoveFromDict(parent_enemy.name, parent_enemy.transform.position.x);
                        HurtEnemy(parent_enemy, 10);
                        player_source[2].Play();  
                    }   
                }
                
            }
        }


        Vector2 right_dir = transform.TransformDirection(Vector2.right);
        RaycastHit2D[] hit2 = Physics2D.RaycastAll(cast_origin.transform.position, right_dir, 30f, 1 << 14);
        RaycastHit2D[] hit3 = Physics2D.CircleCastAll(cast_origin.transform.position, 2f, Vector2.down, 1f, 1 << 14);

        if (!m_touched_door)
        {
            //detects when player is near the exit boss door.
            if (hit2 != null)
            {
                foreach (RaycastHit2D collider_hit in hit2){
                    if(collider_hit.transform.gameObject.name == "BossDoor")
                    {       
                        RenderPlayerImmobile();
                        m_touched_door = true;
                        m_door_anim.SetBool("active", true);
                        StartCoroutine(m_door.WaitToSwitch(collider_hit.transform.position));
                    }
                }
            }
           
        }
        if (hit3 != null)
        {
            foreach (RaycastHit2D collider_hit in hit3)
            {
                Debug.Log(collider_hit.transform.gameObject.name);
                if (collider_hit.transform.gameObject.name == "BossDoor" && Input.GetKeyDown("up") && can_open_door)
                {
                    platform_lvl.SwitchToMaze(collider_hit.transform.gameObject);
                }
            }
        }
    }


    void OnCollisionEnter2D(Collision2D coll)
    { 
        if (!collided){     
            if (coll.gameObject.name == "enemy1(Clone)")
            {
                if(m_health > 0f)
                {   
                    collided = true;
                    StartCoroutine(TakeDamage(coll.transform)); 
                    m_last_hit_time = Time.time; 
                }
            }
        }
        if (coll.gameObject.tag == "Death")
        {
            RespawnPlayer();
        }
        else if (coll.gameObject.tag == "Water")
        {
            plat_char.touching_water = true;
        }
    }

    void OnCollisionExit2D(Collision2D coll)
    {
        if (coll.gameObject.tag == "Water")
        {
            plat_char.touching_water = false;
        }
    }

    void OnTriggerEnter2D(Collider2D coll)
    {   
        if (!collided)
        {
            if (coll.transform.root.gameObject.name == "enemy2(Clone)")
            {
                if(m_health > 0f)
                {
                    collided = true; 
                    StartCoroutine(TakeDamage(coll.transform)); 
                }
            }
        }

        if (coll.gameObject.tag == "Coin")
        {
            player_source[0].Play();
            Destroy(coll.gameObject);
            GainScore(30);
        }
        else if (coll.gameObject.tag == "Coin2")
        {
            player_source[1].Play();
            Destroy(coll.gameObject);
            GainScore(10); 
        }
        else if (coll.gameObject.tag == "Treasure")
        {   
            if (!m_touched_chest)
            {
                m_touched_chest = true;
                can_open_door = true;
                player_source[5].Play();
                StartCoroutine(OpenChest(coll.gameObject));
            }
        }

    }

    IEnumerator OpenChest (GameObject coll)
    {
        treasure_anim.SetTrigger("Open");
        yield return new WaitForSeconds(player_source[5].clip.length/2);
        Destroy(coll);
    }

    IEnumerator TakeDamage (Transform enemy)
    {   
        m_player_anim.SetTrigger("Defend");
        // Make sure the player can't jump.
        player_control.m_Jump = false;

        int direction;

        //player facing right
        if (facing_dir)
        {
            direction = -1;
        }
        else
        {
            direction = 1;
        }


        // Create a vector that's from the enemy to the player with an upwards boost.
        Vector3 hurtVector = transform.position - enemy.position + Vector3.up * 0f + direction * (Vector3.right * 20f);

        // Add a force to the player in the direction of the vector and multiply by the m_hurt_force.
//        GetComponent<Rigidbody2D>().AddForce(hurt_vector);
        GetComponent<Rigidbody2D>().AddForce(hurtVector * 200f);


        // Update what the m_health bar looks like.
        UpdateHealthBar(m_damage_amount);

        yield return new WaitForSeconds(player_defend.length/3);
        collided = false;


        // Play a random clip of the player getting hurt.
//        int i = Random.Range (0, m_ouch_clips.Length);
//        AudioSource.PlayClipAtPoint(m_ouch_clips[i], transform.position);
    }

    public void UpdateHealthBar (float damage)
    {
        m_health -= damage;

        if (health_bar != null)
        {
            health_bar.GetComponent<Slider>().value = m_health;
        }

        if (m_health <= 0)
        {
            health_bar.SetActive(false);
            is_dead = true;
            m_touched_door = false;
        }
    }
}
