using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// script pentru aggresive mobs (zombie)
// passive behaviour - mob-ul va urmari player-ul pana cand ajunge la o distanta fixa de acesta si va incepe sa il atace dandu-i damage
// take damage from player - zombie-ul va putea lua damage de la player, atat timp cat este in viata
// drop items - dupa ce mob-ul moare, acesta va dropa obiecte
public class AIEnemyScript : MonoBehaviour {
    // variabile publice pentru a putea fi modificate in functie de mob
    public float hp = 10f;
    public float damage = 10f;

    // vector de iteme ce vor fi dropate atunci cand mob-ul moare
    public GameObject[] ItemsDeadState = null;

    // variabile private ce se folosesc pentru a sti starea mob-ului si a reda animatii
    private bool isDead = false;
    private bool isAttacking = false;
    private float deathAnimationSpeed = 5f;

    //  variabile private pentru ca zombie-ul sa urmareasca si sa atace player-ul
    private GameObject RigidBodyFPSController;
    Vector3 wayPointPos;
    private float speed = 1.0f;
    private float waitBetweenAnim;

    private const float waitBetweenAttacksOnPlayer = 0.8f;
    private float waitBetweenAttacksOnPlayerTimer = 0f;

    // Start is called before the first frame update
    void Start() {
        // referinta la obiectul ce reprezinta player-ului
        RigidBodyFPSController = GameObject.Find("Player");
    }

    // Update is called once per frame
    void Update() {
        // daca mob-ul nu este mort si nu ataca player-ul, el va urmari player-ul in functie de pozitia sa data prin referinta obiectului
        if (!isDead && !isAttacking) {
            // verificam distanta dintre player si mob
            if (Vector3.Distance(transform.position, RigidBodyFPSController.transform.position) > 1.1) {
                // redam animatia de Walk
                gameObject.GetComponent<Animator>().Play("Walk");

                // mob-ul va fi intotdeauna orientat cu fata spre player
                transform.LookAt(new Vector3(RigidBodyFPSController.transform.position.x, transform.position.y, RigidBodyFPSController.transform.position.z));
                transform.rotation = Quaternion.Euler(
                    transform.rotation.eulerAngles.x,
                    transform.rotation.eulerAngles.y + 180,
                    transform.rotation.eulerAngles.z
                    );

                // primim coordonatele player-ului si modificam pozitia zombie-ului astfel incat sa se deplaseze spre acestea
                wayPointPos = new Vector3(RigidBodyFPSController.transform.position.x, transform.position.y, RigidBodyFPSController.transform.position.z);
                transform.position = Vector3.MoveTowards(transform.position, wayPointPos, speed * Time.deltaTime);
            }
        }

        // daca mob-ul e mort ii setam starea mob-ului idle si simulam o animatie de Death
        else if (isDead && !isAttacking) {
            gameObject.GetComponent<Animator>().Play("Idle");

            Quaternion targetQuaternion = Quaternion.Euler(0, 0, 90);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetQuaternion, deathAnimationSpeed * Time.deltaTime);
        }

        // daca mob-ul nu este mort si ataca player-ul redam animatia de Attack cu un interval de timp de asteptare
        else if (!isDead && isAttacking) {
            gameObject.GetComponent<Animator>().Play("Attack");

            if (waitBetweenAttacksOnPlayerTimer <= 0) {
                StatsMechanism statsMechanism = RigidBodyFPSController.GetComponent<StatsMechanism>();
                statsMechanism.TakeDamage(damage);
                waitBetweenAttacksOnPlayerTimer = waitBetweenAttacksOnPlayer;
            }
        }

        if (waitBetweenAnim >= 0)
            waitBetweenAnim -= Time.deltaTime;
        if (waitBetweenAnim < 0)
            isAttacking = false;
        if (waitBetweenAttacksOnPlayerTimer > 0)
            waitBetweenAttacksOnPlayerTimer -= Time.deltaTime;
    }

    // la coliziunea cu un obiect cu tag-ul Player, mob-ul va ataca continuu
    private void OnCollisionStay(Collision collision) {
        if (collision.collider.gameObject.CompareTag("Player")) {
            isAttacking = true;
            waitBetweenAnim = 0.5f;
        }
    }
}
