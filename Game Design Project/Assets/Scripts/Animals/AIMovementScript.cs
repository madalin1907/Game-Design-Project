using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIMovementScript : MonoBehaviour
{
    private float hp = 3000f;

    private bool isDead = false;
    private float deathAnimationSpeed = 5f;

    public GameObject[] ItemsDeadState = null;

    public float moveSpeed = 1f;
    public float rotSpeed = 5f;

    private bool isWandering = false;
    private bool isRotatingLeft = false;
    private bool isRotatingRight = false;
    private bool isWalking = false;
    private bool isEating = false;

    // Update is called once per frame
    void Update()
    {
        if (isDead == false)
        {
            if (isWandering == false)
            {
                StartCoroutine(Wander());
            }
            if (isRotatingRight == true)
            {
                gameObject.GetComponent<Animator>().Play("Idle");
                transform.Rotate(transform.up * Time.deltaTime * rotSpeed);
            }
            if (isRotatingLeft == true)
            {
                gameObject.GetComponent<Animator>().Play("Idle");
                transform.Rotate(transform.up * Time.deltaTime * -rotSpeed);
            }
            if (isWalking == true)
            {
                gameObject.GetComponent<Animator>().Play("Walk");
                transform.position += transform.forward * moveSpeed * Time.deltaTime;
            }
            if (isEating == true)
            {
                gameObject.GetComponent<Animator>().Play("Eat");
            }

            float xRot = transform.rotation.eulerAngles.x;
            if (transform.rotation.eulerAngles.y != 0 || transform.rotation.eulerAngles.z != 0 || Mathf.Abs(xRot) > 35)
            {   
                if(Mathf.Abs(xRot) > 35)
                {
                    xRot = 34.95f;
                    if (xRot < 0)
                        xRot *= -1;
                }
                // transform.rotation = Quaternion.EulerAngles(new Vector3(xRot, 0, 0));
            }
        }
        
        else
        {
            gameObject.GetComponent<Animator>().Play("Idle");

            Quaternion targetQuaternion = Quaternion.Euler(0, 0, 90);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetQuaternion, deathAnimationSpeed * Time.deltaTime);
        }
    }

    IEnumerator Wander()
    {
        int rotTime = Random.Range(1, 6);
        int rotateWait = Random.Range(1, 6);
        int rotateLorR = Random.Range(1, 3);
        int walkWait = Random.Range(1, 11);
        int walkTime = Random.Range(1, 6);
        int eatWait = Random.Range(1, 5);
        float eatTime = 4.5f;

        isWandering = true;
        if (Random.Range(1, 3) == 1)
        {
            yield return new WaitForSeconds(walkWait);
            isWalking = true;
            yield return new WaitForSeconds(walkTime);
            isWalking = false;

            gameObject.GetComponent<Animator>().Play("Idle");
        }


        if (Random.Range(1, 5) == 1)
        {
            yield return new WaitForSeconds(eatWait);
            isEating = true;
            yield return new WaitForSeconds(eatTime);
            isEating = false;

            gameObject.GetComponent<Animator>().Play("Idle");
        }


        if (Random.Range(1, 3) == 1)
        {
            yield return new WaitForSeconds(rotateWait);
            if (rotateLorR == 1)
            {
                isRotatingRight = true;
                yield return new WaitForSeconds(rotTime);
                isRotatingRight = false;
            }
            if (rotateLorR == 2)
            {
                isRotatingLeft = true;
                yield return new WaitForSeconds(rotTime);
                isRotatingLeft = false;
            }
        }

        isWandering = false;
    }

    private void OnMouseDown()
    {
        if (!isDead)
        {
            if (hp > 0)
            {
                hp -= 1;
            }

            if (hp == 0)
            {
                isDead = true;
                Invoke("ShowItemsDeadState", 3f);
            }

        }
    }

    private void ShowItemsDeadState()
    {
        foreach (var item in ItemsDeadState)
        {
            item.SetActive(true);
        }

        Destroy(GetComponent<BoxCollider>());

        transform.Find("mesh").GetComponent<SkinnedMeshRenderer>().enabled = false;
    }
}