﻿using UnityEngine;
using System.Collections;
using System;

public class EnemyMovement : MonoBehaviour {
  
    [SerializeField] private LayerMask m_WhatIsGround; 

    const float k_GroundedRadius = 1f; // Radius of the overlap circle to determine if grounded

    private int m_move_state = 0;
    private int m_direction = -1;
    private float m_speed = 5.0f;
    private float m_timer = 0.0f;
    private float curr_y_pos;
    public bool m_can_move = false;
    private bool m_move_up = false; 
    private bool m_just_switched = false;
    private bool m_Grounded;
    private bool m_incline;
    private bool m_platform_switched = false;
    private Animator m_anim;
    private Rigidbody2D m_rb2d;
    
    private void Awake()
    {
        if (this.gameObject.name == "enemy1(Clone)")
        {
            m_anim = gameObject.transform.Find("Collider").GetComponent<Animator>();
        }

        m_rb2d = this.GetComponent<Rigidbody2D>();
        transform.GetComponentInChildren<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
        StartCoroutine(WaitToSpawn());
    }

    IEnumerator WaitToSpawn()
    {
        yield return new WaitForSeconds(1f);
        m_can_move = true;
    }


    //moves enemy up and down;
    private void MoveUpAndDown()
    {
        transform.Translate(Vector2.up * m_speed * m_direction* Time.deltaTime);
        m_just_switched = false;
    }


    //switches enemy's current moving direction
    private void SwitchDirection()
    {
        m_direction *= -1;
        m_timer = 1.0f;
        m_just_switched = true;
    }
        
    //used for dealing with rigidbodies; called every physics step
    void FixedUpdate ()
    {

//        transform.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;

        m_Grounded = false;
        m_incline = false;


        Transform m_GroundCheck = transform.Find("GroundCheck").transform;
       
        Collider2D[] colliders = ((Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround)) ?? (new Collider2D[1]));

        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].gameObject != gameObject){
                    m_Grounded = true;
                }
            }
        }

        Transform FrontCheck = GameObject.Find("frontCheck").transform;

        Collider2D[] colliders2 = ((Physics2D.OverlapCircleAll(FrontCheck.position, k_GroundedRadius, m_WhatIsGround)) ?? (new Collider2D[1]));

        if (colliders2 != null)
        {
            for (int i = 0; i < colliders2.Length; i++)
            {
                if (colliders2[i].gameObject.tag == "Incline"){
                    m_incline = true;
                }
            }
        }


        if (m_can_move == true)
        {
            if (this.gameObject.name == "enemy1" || this.gameObject.name == "enemy1(Clone)")
            {
                 //need to fix this physics; enemy going up a slope
                if (!m_Grounded)
                {
                    this.GetComponentInChildren<Rigidbody2D>().velocity = new Vector2(-2.5f, -20f);

                }
                else if (m_incline && m_Grounded)
                {  
                    this.GetComponentInChildren<Rigidbody2D>().velocity = new Vector2(-2.5f, 0);
                }

                else if (!m_incline && m_Grounded)
                {
                    this.GetComponentInChildren<Rigidbody2D>().velocity = new Vector2(-5f, 0f);   
                }
                m_anim.SetFloat("x_velocity", Mathf.Abs(this.GetComponentInChildren<Rigidbody2D>().velocity.x));
            }
        }
    }

    void Update(){
        curr_y_pos = this.transform.position.y;

        m_timer += Time.deltaTime;

        if (m_can_move)
        {
            if (this.name == "enemy2" || this.name == "enemy2(Clone)")
            {
                MoveUpAndDown();          
                if (m_timer > 3.0f)
                {
                    SwitchDirection();
                }
            }
        }
    }

}
