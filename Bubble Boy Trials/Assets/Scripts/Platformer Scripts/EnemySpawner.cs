﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour {

	private GameObject player;

    private bool can_spawn;
    private bool first_spawn;
	private float m_timer = 0.0f;
	private float m_new_x; 
	private float m_new_y;
    private float last_hit_time;
	private int m_count = 0;
	private float m_player_pos = 4.9f; //players current global view position
	private Vector2 spawn_position; //enemy spawn position
    private AudioSource source;

	public GameObject[] enemy_types; //stores type of enemies
	private GameObject[] active_objects; //active game objects
	private float[] enemy_1_positions = new float[]{/*32.1f,*/ 154.9f, 298f, 358.3f}; //all possible x-coordinate spawning points for grounded minions
    private float[] enemy_2_positions = new float[]{75.2f, 251.3f}; //all possible x-coordinate spawning points for flying minions
	private Dictionary<string, List<float>> curr_enemy_positions; /* dictionary that holds all active instantiated enemies 
																    & their current positions */
                                                                    																										

	private string ListToStr (List<float> list)
	{
		string x = "[";
		foreach (float e in list)
		{
			x += System.Convert.ToString(e) + "," ;
		}
		x += "]";
		return x;
	}

	private string ArrayToStr (GameObject[] arr)
	{
		string x = "[";
		foreach (GameObject e in arr)
		{
			x += e.name + ",";
		}
		x += "]";
		return x;
	}

	private void PrintDict ()
	{
		foreach (KeyValuePair<string,List<float>> pair in curr_enemy_positions)
		{
			Debug.Log("DICT:" + pair.Key + ", " + ListToStr(pair.Value));
		}
	}

	//initializes curr_enemy_positions Dictionary
	private void InitializeEnemyPos ()
	{
		curr_enemy_positions = new Dictionary<string, List<float>>();
		foreach (GameObject enemy in enemy_types)
		{
			curr_enemy_positions.Add(enemy.name + "(Clone)", new List<float>());
		}
	}


    // adds given position to the dictionary
	private void AddToDict (string name, float pos)
	{
		try 
		{
			curr_enemy_positions[name].Add(pos); 
		} 
		catch (Exception e)
		{
            Debug.Log("key is not in dictionary yet. Will add:" + e.Message);
			curr_enemy_positions.Add(name, new List<float>(new float[]{pos}));
		}
	}

    IEnumerator DelaySpawn ()
    {
        yield return new WaitForSeconds (8f);
        can_spawn = true;
    }

	//removes a position from the given enemy's list of positons.
	public void RemoveFromDict (string name, float pos)
	{
		try
		{
	        float new_pos = pos;
	        foreach (float x_pos in curr_enemy_positions[name])
        	{
            	if (Mathf.Abs(x_pos - pos) <= 25.0f)
            	{
	                new_pos = x_pos;
	                break;
	            }
      		}
			curr_enemy_positions[name].Remove(new_pos); 
            can_spawn = false;
            StartCoroutine(DelaySpawn());

		}
		catch (Exception e)
		{
        	Debug.LogError("key is not in dictionary yet. Cannot remove:" + e.Message);
		}
	}

    private bool ShouldInstantiate (string enemy_key, float enemy_pos)
    {
        try 
        {
    		if (active_objects.Length != 0){
        		if (!(curr_enemy_positions[enemy_key].Contains(enemy_pos)))
        		{
        		   return true;
           	 	}
            	else
            	{
           			return false;
        		} 			
            }
            else
            {
               return true;
            }
        }
        catch (Exception e)
        {
            return false;
        }
       
    }

	//spawns minions in designated positions if the player is in viewable range
	private void SpawnMinions ()
	{
        float[] curr_positions = new float[Mathf.Max(enemy_1_positions.Length, 
                                                   enemy_2_positions.Length)];

        foreach (GameObject enemy in enemy_types)
        {        
            switch (enemy.name)
            {
                case "enemy1":
                case "enemy1(Clone)":
                    curr_positions = enemy_1_positions;
                    break;
                case "enemy2":
                case "enemy2(Clone)":
                    curr_positions = enemy_2_positions;
                    break;
            }
            for (int i = 0; i< curr_positions.Length; i++)
            {
                if (Mathf.Abs(m_player_pos - curr_positions[i]) <= 30.0f){  
                    if (curr_positions[i] == 358.3f && ShouldInstantiate(enemy.name + "(Clone)", curr_positions[i]))
                    {
                        spawn_position = new Vector2 (curr_positions[i], 15.3f);
                        GameObject new_enemy = (GameObject) Instantiate(enemy, 
                                    spawn_position, enemy.transform.rotation);
                        source.Play();
                        AddToDict(new_enemy.name, curr_positions[i]);
                    }
                    else if (ShouldInstantiate(enemy.name + "(Clone)", curr_positions[i]))
                    {
						spawn_position = new Vector2 (curr_positions[i], 30);
						GameObject new_enemy = (GameObject) Instantiate(enemy, 
	                                spawn_position, enemy.transform.rotation);
                        source.Play();
	                    AddToDict(new_enemy.name, curr_positions[i]);
    		    	}
    		    }
    		}
        }
	}

    void Awake()
    {
        InitializeEnemyPos();
        player = GameObject.Find("Player");
        m_player_pos = player.transform.position.x;
        can_spawn = true;
        source = this.gameObject.GetComponent<AudioSource>();
    }

    void Update ()
    {
        active_objects = UnityEngine.GameObject.FindGameObjectsWithTag("Enemy"); 
        active_objects.Distinct();

    }

	void FixedUpdate () 
	{
		if(can_spawn)
		{
			m_player_pos = player.transform.position.x;
			SpawnMinions();
		}
	}
   
}
