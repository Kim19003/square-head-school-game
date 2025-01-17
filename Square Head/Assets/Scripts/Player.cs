﻿using Assets.Scripts;
using System;
using System.Collections;
using System.Drawing;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class Player : MonoBehaviour
{
    public float jumpForce;
    public float movementForce;
    public float knockoutForce;

    public Text overPlayerText;

    public Vector3 DefaultPosition { get; private set; }
    public float DefaultGravityScale { get; private set; }
    public float DefaultDrag { get; private set; }
    public float DefaultJumpForce { get; private set; }
    public float DefaultMovementForce { get; private set; }
    public float DefaultKnockoutForce { get; private set; }
    public RigidbodyInterpolation2D DefaultRigidbodyInterpolation { get; private set; }

    public float CurrentHorizontalLookingDirection { get; private set; }
    public float CurrentVerticalLookingDirection { get; private set; }
    public float PreviousOtherThanZeroXVelocity { get; private set; }
    public float PreviousOtherThanZeroYVelocity { get; private set; }
    public Vector2 PreviousSafePosition { get; private set; }

    public int Lifes { get { return gameControllerScript.PlayerLifes; } }
    public int Points { get { return gameControllerScript.PlayerPoints; } }
    public int Ammunation { get { return gameControllerScript.PlayerAmmunation; } }

    public bool IsGrounded { get { return thisGroundCheck.IsGrounded; } }
    public bool IsOnPlatform { get { return thisGroundCheck.IsOnPlatform; } }
    public bool HasAttackedEnemy { get; set; }
    public bool IsOnLadder { get; set; }
    public bool IsInWater { get; set; }
    public bool IsOnIce { get; set; }
    public bool IsJumping { get; private set; }
    public bool IsAttacking { get; private set; }
    public bool IsImmune { get; private set; }
    public bool IsDead { get; private set; }
    public bool HasBossWeapon { get; private set; }

    Rigidbody2D thisRigidBody2D;
    Collider2D[] thisCollider2Ds;
    SpriteRenderer thisSpriteRenderer;
    GroundCheck thisGroundCheck;
    Animator thisAnimator;
    
    GameController gameControllerScript;

    GameObject bullet;
    GameObject redBullet;

    bool canAttack = true;
    bool canJumpInWater = true;

    float nextImmunityFlashingTime = 0f;
    float immunityFlashingInterval = 0.1f;

    int consecutiveEasterEggActions = 0;

    void Start()
    {
        thisRigidBody2D = GetComponent<Rigidbody2D>();
        thisCollider2Ds = transform.Find("Normal Body").GetComponents<Collider2D>();
        thisSpriteRenderer = GetComponent<SpriteRenderer>();
        thisGroundCheck = transform.Find("GroundCheck").GetComponent<GroundCheck>();
        thisAnimator = GetComponent<Animator>();
        
        gameControllerScript = GameObject.Find("GameController").GetComponent<GameController>();

        bullet = GameObject.Find("Bullet");
        redBullet = GameObject.Find("Red Bullet");

        DefaultPosition = transform.position;
        DefaultGravityScale = thisRigidBody2D.gravityScale;
        DefaultDrag = thisRigidBody2D.drag;
        DefaultJumpForce = jumpForce;
        DefaultMovementForce = movementForce;
        DefaultKnockoutForce = knockoutForce;
        DefaultRigidbodyInterpolation = thisRigidBody2D.interpolation;
    }

    void Update()
    {
        if (gameControllerScript.LevelCompleted)
        {
            return;
        }
        else if (IsDead)
        {
            StartCoroutine(GameOverInTime(3f));

            return;
        }

        if (!overPlayerTextCoroutineStarted)
        {
            overPlayerText.transform.position = GetDefaultOverPlayerTextPosition();
        }

        HandleImmunityFlashing(ref nextImmunityFlashingTime, immunityFlashingInterval, IsImmune);

        Vector2 movementDirection = GetWantToMoveDirection();

        Debug.Log(thisRigidBody2D.velocity.x);

        thisAnimator.SetFloat("Horizontal", movementDirection.x);
        thisAnimator.SetFloat("Vertical", movementDirection.y);
        if (movementDirection.x != 0)
        {
            CurrentHorizontalLookingDirection = movementDirection.x;
            thisAnimator.SetFloat("HorizontalLookingDirection", CurrentHorizontalLookingDirection);
        }
        if (movementDirection.y != 0)
        {
            CurrentVerticalLookingDirection = movementDirection.y;
            thisAnimator.SetFloat("VerticalLookingDirection", CurrentVerticalLookingDirection);
        }
        thisAnimator.SetBool("IsOnLadder", IsOnLadder);
        thisAnimator.SetBool("IsGrounded", IsGrounded);
        thisAnimator.SetBool("IsAttacking", IsAttacking);
        thisAnimator.SetBool("IsOnIce", IsOnIce);
        thisAnimator.SetFloat("HorizontalSpeed", Mathf.Abs(thisRigidBody2D.velocity.x));
        thisAnimator.SetFloat("VerticalSpeed", Mathf.Abs(thisRigidBody2D.velocity.y));

        if (thisRigidBody2D.velocity.x != 0)
        {
            PreviousOtherThanZeroXVelocity = thisRigidBody2D.velocity.x;
        }
        if (thisRigidBody2D.velocity.y != 0)
        {
            PreviousOtherThanZeroYVelocity = thisRigidBody2D.velocity.y;
        }

        SlowFasterWhenHasDrag(0.2f);

        // Boss weapon easter egg
        if (!HasBossWeapon)
        {
            if (gameControllerScript.PlayerAmmunation == 9)
            {
                if (Input.GetKeyDown(KeyCode.Alpha9))
                {
                    consecutiveEasterEggActions++;

                    if (consecutiveEasterEggActions == 9)
                    {
                        SetOverPlayerText("BOSS WEAPON ACTIVATED!", Helpers.GetCustomColor(CustomColor.OrangeRed));
                        gameControllerScript.PlayerHasBossWeapon(true);
                        HasBossWeapon = true;
                    }
                }
            }
            else
            {
                consecutiveEasterEggActions = 0;
            }
        }
        // -----

        if (IsInWater && canJumpInWater)
        {
            IsJumping = false;

            if (WantToMoveUp()) // Want to jump
            {
                thisRigidBody2D.velocity = new Vector2(thisRigidBody2D.velocity.x, jumpForce);

                IsJumping = true;

                if (!canJumpInWaterAfterActivated)
                {
                    StartCoroutine(CanJumpInWaterAfter(0.75f));
                }
            }
        }
        else if (!IsInWater && canJumpInWaterAfterActivated)
        {
            canJumpInWater = true;
            StopCoroutine(nameof(CanJumpInWaterAfter));
            canJumpInWaterAfterActivated = false;
        }

        if (IsOnLadder)
        {
            StopAttack();
        }
        else if (IsGrounded)
        {
            IsJumping = false;
            canJumpInWater = true;
            if (canJumpInWaterAfterActivated)
            {
                StopCoroutine(nameof(CanJumpInWaterAfter));
                canJumpInWaterAfterActivated = false;
            }
            
            if (IsOnPlatform && thisRigidBody2D.velocity.y == 0)
            {
                PreviousSafePosition = transform.position;
            }

            if (WantToMoveUp()) // Want to jump
            {
                thisRigidBody2D.velocity = new Vector2(thisRigidBody2D.velocity.x, jumpForce);

                IsJumping = true;
            }
            else if (WantToAttack())
            {
                if (!IsAttacking && canAttack)
                {
                    StartCoroutine(Attack(0.2f, 0.2f, 0.3f));
                }
            }
        }
        else // Is on air
        {
            if (WantToAttack())
            {
                if (!IsAttacking && canAttack)
                {
                    StartCoroutine(Attack(0.2f, 0.2f, 0.3f));
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (gameControllerScript.LevelCompleted)
        {
            gameControllerScript.StopRunTime();
            gameControllerScript.HideAllUIElements();
            gameControllerScript.ShowBlackbars(true);

            IsImmune = true;
            thisRigidBody2D.velocity = new Vector2(movementForce / 2, thisRigidBody2D.velocity.y);

            StartCoroutine(LevelOverInTime(5));

            return;
        }
        else if (IsDead)
        {
            return;
        }

        if (IsOnLadder)
        {
            HandleLadderMovement();
        }
        else
        {
            if (WantToMoveLeft())
            {
                thisRigidBody2D.velocity = new Vector2(-movementForce, thisRigidBody2D.velocity.y);
            }
            else if (WantToMoveRight())
            {
                thisRigidBody2D.velocity = new Vector2(movementForce, thisRigidBody2D.velocity.y);
            }
            else
            {
                if (!IsOnIce)
                {
                    thisRigidBody2D.velocity = new Vector2(0, thisRigidBody2D.velocity.y);
                }
            }
        }
    }

    void HandleLadderMovement()
    {
        if (WantToMoveUp())
        {
            if (WantToMoveLeft())
            {
                thisRigidBody2D.velocity = new Vector2(-movementForce, movementForce);
            }
            else if (WantToMoveRight())
            {
                thisRigidBody2D.velocity = new Vector2(movementForce, movementForce);
            }
            else
            {
                thisRigidBody2D.velocity = new Vector2(0f, movementForce);
            }
        }
        else if (WantToMoveDown())
        {
            if (WantToMoveLeft())
            {
                thisRigidBody2D.velocity = new Vector2(-movementForce, -movementForce);
            }
            else if (WantToMoveRight())
            {
                thisRigidBody2D.velocity = new Vector2(movementForce, -movementForce);
            }
            else
            {
                thisRigidBody2D.velocity = new Vector2(0f, -movementForce);
            }
        }
        else if (WantToMoveLeft())
        {
            if (WantToMoveUp())
            {
                thisRigidBody2D.velocity = new Vector2(-movementForce, movementForce);
            }
            else if (WantToMoveDown())
            {
                thisRigidBody2D.velocity = new Vector2(-movementForce, -movementForce);
            }
            else
            {
                thisRigidBody2D.velocity = new Vector2(-movementForce, 0f);
            }
        }
        else if (WantToMoveRight())
        {
            if (WantToMoveUp())
            {
                thisRigidBody2D.velocity = new Vector2(movementForce, movementForce);
            }
            else if (WantToMoveDown())
            {
                thisRigidBody2D.velocity = new Vector2(movementForce, -movementForce);
            }
            else
            {
                thisRigidBody2D.velocity = new Vector2(movementForce, 0f);
            }
        }
        else
        {
            thisRigidBody2D.velocity = new Vector2(0, 0);
        }
    }

    Vector2 GetWantToMoveDirection()
    {
        Vector2 direction = Vector2.zero;

        if (WantToMoveLeft())
        {
            direction += Vector2.left;
        }
        else if (WantToMoveRight())
        {
            direction += Vector2.right;
        }
        else if (WantToMoveUp())
        {
            direction += Vector2.up;
        }
        else if (WantToMoveDown())
        {
            direction += Vector2.down;
        }

        return direction;
    }

    public void Die()
    {
        thisCollider2Ds.DisableAll();
        thisSpriteRenderer.sortingLayerName = "Player Dying";
        thisRigidBody2D.velocity = new Vector2(0, knockoutForce / 2);

        thisRigidBody2D.gravityScale = 2f;
        thisRigidBody2D.freezeRotation = false;
        thisRigidBody2D.MoveRotation(-180);

        IsDead = true;
    }

    IEnumerator GameOverInTime(float time)
    {
        yield return new WaitForSeconds(time);

        gameControllerScript.GameOver(false);
    }

    IEnumerator LevelOverInTime(float time)
    {
        yield return new WaitForSeconds(time);

        gameControllerScript.LevelOver();
    }

    IEnumerator Attack(float attackDuration, float bulletDelay, float attackDelayDuration)
    {
        IsAttacking = true;
        canAttack = false;

        if (HasBossWeapon)
        {
            ShootBullet(true);

            yield return new WaitForSeconds(bulletDelay);

            ShootBullet(true);

            yield return new WaitForSeconds(bulletDelay);

            ShootBullet(true);
        }
        else if (gameControllerScript.PlayerHasAmmunation())
        {
            ShootBullet();
        }

        yield return new WaitForSeconds(attackDuration);

        IsAttacking = false;

        yield return new WaitForSeconds(attackDelayDuration);

        canAttack = true;
    }

    bool canJumpInWaterAfterActivated = false;
    IEnumerator CanJumpInWaterAfter(float seconds)
    {
        canJumpInWaterAfterActivated = true;
        canJumpInWater = false;

        yield return new WaitForSeconds(seconds);

        canJumpInWater = true;
        canJumpInWaterAfterActivated = false;
    }

    void ShootBullet(bool isBossBullet = false)
    {
        GameObject newBullet;
        if (isBossBullet)
        {
            newBullet = Instantiate(redBullet);
        }
        else
        {
            newBullet = Instantiate(bullet);
        }
        Bullet newBulletScript = newBullet.GetComponent<Bullet>();
        newBulletScript.Owner = gameObject;
        newBulletScript.CurrentHorizontalLookingDirection = CurrentHorizontalLookingDirection;
        newBullet.transform.position = transform.position + new Vector3(CurrentHorizontalLookingDirection * 0.5f, 0, 0);
        newBulletScript.Launch();
        if (!isBossBullet)
        {
            gameControllerScript.LosePlayerAmmunation(1);
        }
    }

    void StopAttack()
    {
        IsAttacking = false;
    }

    public void TakeDamage(float damage)
    {
        if (!IsImmune)
        {
            gameControllerScript.LosePlayerLifes((int)damage, false);
            SetOverPlayerText($"-{(int)damage}", Color.red);

            if (gameControllerScript.PlayerLifes > 0)
            {
                thisRigidBody2D.velocity = new Vector2(thisRigidBody2D.velocity.x, DefaultKnockoutForce);

                StartCoroutine(StartImmunity(2));
            }
            else
            {
                Die();
            }
        }
    }

    public IEnumerator StartImmunity(float duration)
    {
        if (!IsInWater)
        {
            thisSpriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);
        }
        else
        {
            thisSpriteRenderer.color = Helpers.GetInWaterColor(0.5f);
        }
        GameObject[] foundEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in foundEnemies)
        {
            foreach (Collider2D collider2D in thisCollider2Ds)
            {
                if (collider2D.enabled)
                {
                    Physics2D.IgnoreCollision(collider2D, enemy.GetComponent<Collider2D>(), true);
                }
            }
        }
        IsImmune = true;

        yield return new WaitForSeconds(duration);

        if (!IsInWater)
        {
            thisSpriteRenderer.color = new Color(1f, 1f, 1f, 1f);
        }
        else
        {
            thisSpriteRenderer.color = Helpers.GetInWaterColor(1f);
        }
        foundEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in foundEnemies)
        {
            foreach (Collider2D collider2D in thisCollider2Ds)
            {
                if (collider2D.enabled)
                {
                    Physics2D.IgnoreCollision(collider2D, enemy.GetComponent<Collider2D>(), false);
                }
            }
        }
        IsImmune = false;
    }

    bool WantToMoveLeft()
    {
        return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
    }

    bool WantToMoveRight()
    {
        return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
    }

    bool WantToMoveUp()
    {
        return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.Space);
    }

    bool WantToMoveDown()
    {
        return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
    }

    bool WantToAttack()
    {
        return Input.GetKey(KeyCode.Z);
    }

    public void SetJumpForce(float jumpForce)
    {
        this.jumpForce = jumpForce;
    }

    public void SetMovementForce(float movementForce)
    {
        this.movementForce = movementForce;
    }
    
    public void SetKnockoutForce(float knockoutForce)
    {
        this.knockoutForce = knockoutForce;
    }

    void SlowFasterWhenHasDrag(float minSpeed)
    {
        if (thisRigidBody2D.drag > 0 && Mathf.Abs(thisRigidBody2D.velocity.x) < minSpeed)
        {
            thisRigidBody2D.velocity = new Vector2(0, thisRigidBody2D.velocity.y);
        }
    }
    
    void HandleImmunityFlashing(ref float nextImmunityFlashingTime, float immunityFlashingInterval, bool isImmune)
    {
        if (Time.timeSinceLevelLoad > nextImmunityFlashingTime)
        {
            if (isImmune)
            {
                if (thisSpriteRenderer.color.a == 0.5f)
                {
                    thisSpriteRenderer.color = new Color(thisSpriteRenderer.color.r, thisSpriteRenderer.color.g, thisSpriteRenderer.color.b, 0f);
                }
                else
                {
                    thisSpriteRenderer.color = new Color(thisSpriteRenderer.color.r, thisSpriteRenderer.color.g, thisSpriteRenderer.color.b, 0.5f);
                }
            }
            else if (thisSpriteRenderer.color.a.IsAny(0.5f, 0f))
            {
                thisSpriteRenderer.color = new Color(thisSpriteRenderer.color.r, thisSpriteRenderer.color.g, thisSpriteRenderer.color.b, 1f);
            }

            nextImmunityFlashingTime += immunityFlashingInterval;
        }
    }

    Coroutine overPlayerTextCoroutine = null;
    bool overPlayerTextCoroutineStarted = false;

    public void SetOverPlayerText(string text, Color color)
    {
        overPlayerText.color = color;
        overPlayerText.text = text;
        overPlayerText.transform.position = new Vector2(overPlayerText.transform.position.x, overPlayerText.transform.position.y - 0.5f);

        if (overPlayerTextCoroutine != null && overPlayerTextCoroutineStarted)
        {
            StopCoroutine(overPlayerTextCoroutine);

            overPlayerText.transform.position = GetDefaultOverPlayerTextPosition();

            overPlayerTextCoroutineStarted = false;
        }

        LeanTween.moveY(overPlayerText.gameObject, overPlayerText.transform.position.y + 0.5f, 0.5f).setEase(LeanTweenType.easeInOutSine);

        overPlayerTextCoroutine = StartCoroutine(ClearOverPlayerTextAfter(2));
    }

    IEnumerator ClearOverPlayerTextAfter(float seconds)
    {
        overPlayerTextCoroutineStarted = true;

        yield return new WaitForSeconds(seconds);

        overPlayerText.color = Color.black;
        overPlayerText.text = string.Empty;
        overPlayerText.transform.position = GetDefaultOverPlayerTextPosition();

        overPlayerTextCoroutineStarted = false;
    }

    public void SetOverPlayerTextPosition(Vector2 position)
    {
        overPlayerText.transform.position = position;
    }

    public Vector2 GetDefaultOverPlayerTextPosition()
    {
        return new Vector2(transform.position.x, transform.position.y + 1);
    }

    public Collider2D[] GetCollider2Ds()
    {
        return thisCollider2Ds;
    }
}
