﻿using UnityEngine;
using System.Collections;

public class MarioControl : MonoBehaviour {
	
	BarrelSpawner barrelSpawner;
	SoundEffectPlayer soundEffectPlayer;

	bool facingRight = true;
	bool shouldJump = false;
	bool isJumping = false;
	bool isDead = false;
	bool hasWon = false;
	
	float maxSpeed = 0.5f;
	float jumpForce = 45f;

	private bool grounded = false;
	private Animator anim;
	private Collider2D climbingLadder;
	private Vector3 bottomPosition;
	private SpriteRenderer gameOverSprite;
	private float spriteHeight;

	void Awake() {

		spriteHeight = GetComponent<SpriteRenderer>().sprite.bounds.size.y;

		anim = GetComponent<Animator>();
		bottomPosition = new Vector3(0, -spriteHeight*0.5f, 0);

		if (GameObject.Find("DonkeyKong") != null) barrelSpawner = GameObject.Find("DonkeyKong").GetComponent<BarrelSpawner>();
		soundEffectPlayer = gameObject.AddComponent<SoundEffectPlayer>();

		if (GameObject.Find ("GameOver") != null) gameOverSprite = GameObject.Find ("GameOver").GetComponent<SpriteRenderer>();
	}
	
	
	void Update() {

		if (isDead) return;

		grounded = Physics2D.Raycast(new Vector3(transform.position.x, transform.position.y - spriteHeight*0.5f, 0), new Vector2(0, -1), 0.1f, 1 << LayerMask.NameToLayer("Ground"));  

		// If the jump button is pressed and the player is grounded then the player should jump
		if(Input.GetButtonDown("Jump") && grounded && !isJumping)
			shouldJump = true;
		else if (Input.GetAxis("Vertical") != 0 && climbingLadder == null) {

			int climbingDir = (int)Mathf.Sign(Input.GetAxis("Vertical"));
			float lookDistance;
			if (climbingDir > 0) lookDistance = spriteHeight*0.5f;
			else lookDistance = spriteHeight*0.2f;

			climbingLadder = FindLadderInDirection(climbingDir, lookDistance);
			if (climbingLadder != null) {
				GetComponent<Rigidbody2D>().isKinematic = true;
			}
		}
	}
	
	
	void FixedUpdate () {

		if (isDead) return;

		if (isJumping && grounded && GetComponent<Rigidbody2D>().velocity.y <= 0) {
			isJumping = false;
		}

		float h = Input.GetAxis("Horizontal");

		if (climbingLadder != null) {
			float v = Input.GetAxis("Vertical");

			float dir = v == 0 ? 0 : Mathf.Sign(v);

			Collider2D ladder = FindLadderInDirection((int)dir, spriteHeight*0.2f);
			if (ladder != climbingLadder) {
				if (grounded) {
					climbingLadder = null;
					GetComponent<Rigidbody2D>().isKinematic = false;
				}
				else {
					GetComponent<Rigidbody2D>().velocity = new Vector2(0, 0);
				}
			}
			else {
				GetComponent<Rigidbody2D>().velocity = new Vector2(0, dir * maxSpeed * 0.5f);
			}

			// Play walking sound
			if (Mathf.Abs(GetComponent<Rigidbody2D>().velocity.y) > 0.01) {
				soundEffectPlayer.PlayWalkEffect(true);
			}
			else soundEffectPlayer.PlayWalkEffect(false);
		}
		else {

			float dir = h == 0 ? 0 : Mathf.Sign(h);
			GetComponent<Rigidbody2D>().velocity = new Vector2(dir * maxSpeed, GetComponent<Rigidbody2D>().velocity.y);

			if(h > 0 && !facingRight)
				Flip();
			else if(h < 0 && facingRight)
				Flip();
			
			if(shouldJump) {

				soundEffectPlayer.PlayJumpEffect();

				// Add a vertical force to the player.
				GetComponent<Rigidbody2D>().AddForce(new Vector2(0f, jumpForce));
				
				// Make sure the player can't jump again until the jump conditions from Update are satisfied.
				shouldJump = false;
				isJumping = true;
			}

			// Play walking sound
			if (!isJumping && Mathf.Abs (GetComponent<Rigidbody2D>().velocity.x) > 0.01f) {
				soundEffectPlayer.PlayWalkEffect(true);
			}
			else {
				soundEffectPlayer.PlayWalkEffect(false);
			}
		}

		// Update the animation state
		if (anim != null) anim.SetFloat("Speed", Mathf.Abs(h));
		if (anim != null) anim.SetBool("Jumping", isJumping);
		if (anim != null) anim.SetBool("Climbing", climbingLadder != null);
	}
	
	
	void Flip () {

		facingRight = !facingRight;
		
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}

	Collider2D FindLadderInDirection(int dir, float distance) {

		RaycastHit2D hit = Physics2D.Raycast(transform.position+bottomPosition, new Vector2(0, dir), distance, 1 << LayerMask.NameToLayer("Ladder"));  
		return hit.collider;
	}

	void OnCollisionEnter2D(Collision2D collision) {

		if (collision.collider.gameObject.name == "Barrel" && !isDead) {

			isDead = true;

			// Play the death animation
			if (anim != null) anim.SetBool("Death", true);

			// Set Mario's velocity to 0, and increase his mass so that the barrels can't push him around :)
			GetComponent<Rigidbody2D>().velocity = Vector2.zero;
			GetComponent<Rigidbody2D>().mass = 1000;

			// Stop the background music & walking sound, and play the death sound effect instead
			soundEffectPlayer.PlayWalkEffect(false);
			soundEffectPlayer.StopBackgroundMusic();
			soundEffectPlayer.PlayDieEffect();

			// Don't spawn any more barrels
			if (barrelSpawner != null) barrelSpawner.Stop();

			// Show the Game Over sprite
			if (gameOverSprite != null) gameOverSprite.enabled = true;
		}
	}

	void OnTriggerEnter2D(Collider2D otherCollider) {

		if (otherCollider.gameObject.name == "WinArea" && !hasWon) {
			Debug.Log("You win!");

			hasWon = true;

			// Stop background music and play the level completion sound effect instead
			soundEffectPlayer.StopBackgroundMusic();
			soundEffectPlayer.PlayWinEffect();

			// Don't spawn any more barrels
			if (barrelSpawner != null) barrelSpawner.Stop();
		}
	}
}
