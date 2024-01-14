using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class animationStateController : MonoBehaviour
{
    Animator animator;
    int isWalkingHash;
    int isRunningHash;
    int isJumpingHash;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();

        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");
        isJumpingHash = Animator.StringToHash("isJumping");
    }

    // Update is called once per frame
    void Update()
    {
        bool isRunning = animator.GetBool(isRunningHash);
        bool isWalking = animator.GetBool(isWalkingHash);
        bool isJumping = animator.GetBool(isJumpingHash);

        bool wPressed = Input.GetKey("w");
        bool sPressed = Input.GetKey("s");
        bool aPressed = Input.GetKey("a");
        bool dPressed = Input.GetKey("d");
        bool runPressed = Input.GetKey("left shift");
        bool jumpPressed = Input.GetKey("space");


        if (!isWalking && (wPressed || sPressed || aPressed || dPressed))
        {
            animator.SetBool(isWalkingHash, true);
        }

        if (isWalking && !(wPressed || sPressed || aPressed || dPressed))
        {
            animator.SetBool(isWalkingHash, false);
        }


        if (!isRunning && ((wPressed || sPressed || aPressed || dPressed) && runPressed))
        {
            animator.SetBool(isRunningHash, true);
        }

        if (isRunning && (!(wPressed || sPressed || aPressed || dPressed) || !runPressed))
        {
            animator.SetBool(isRunningHash, false);
        }

        if (!isJumping && jumpPressed && (wPressed || sPressed || aPressed || dPressed))
        {
            animator.SetBool(isJumpingHash, true);
        }

        if (isJumping && (!(wPressed || sPressed || aPressed || dPressed) || !jumpPressed))
        {
            animator.SetBool(isJumpingHash, false);
        }

        if (!isJumping && jumpPressed && runPressed)
        {
            animator.SetBool(isJumpingHash, true);
        }

        if (isJumping && (!runPressed || !jumpPressed))
        {
            animator.SetBool(isJumpingHash, false);
        }
    }
}
