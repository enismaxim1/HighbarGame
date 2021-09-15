using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Move : MonoBehaviour
{

    public GameObject[] bars;
    public float springConstant;
    public float damper;
    private Vector3[] barPositions;

    public GameObject person;
    private GameObject head, arms, stomach, thighs, legs, feet, hand1, hand2;
    private HingeJoint headHinge, armsHinge, stomachHinge, thighsHinge, legsHinge, feetHinge;
    private JointSpring headSpring, armsSpring, stomachSpring, thighsSpring, legsSpring, feetSpring;
    private GameObject[] bodyParts;
    
    public float headStrength = 150;
    public float stomachStrength = 300;
    public float thighsStrength = 250;
    public float legsStrength = 250;
    public float feetStrength = 150;

    public float headDamper = 50;
    public float stomachDamper = 50;
    public float thighsDamper = 50;
    public float legsDamper = 50;
    public float feetDamper = 50;

    public float maxVelocity;
    public float catchTolerance;

    private bool dontCatch = true;
    private bool onBar = true;
    private int numCurrentBar = 0;

    private int regrabs;
    public Text regrabText;

    // Start is called before the first frame update
    void Start() {

        head = person.transform.GetChild(0).gameObject;
        arms = person.transform.GetChild(1).gameObject;
        stomach = person.transform.GetChild(2).gameObject;
        thighs = person.transform.GetChild(3).gameObject;
        legs = person.transform.GetChild(4).gameObject;
        feet = person.transform.GetChild(5).gameObject;
        hand1 = arms.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject;
        hand2 = arms.transform.GetChild(1).gameObject.transform.GetChild(0).gameObject;
        
        bodyParts = new GameObject[6];

        bodyParts[0] = arms;
        bodyParts[1] = head;
        bodyParts[2] = stomach;
        bodyParts[3] = thighs;
        bodyParts[4] = legs;
        bodyParts[5] = feet;

        headHinge = bodyParts[1].GetComponent<HingeJoint>();
        armsHinge = arms.GetComponent<HingeJoint>();
        stomachHinge = bodyParts[2].GetComponent<HingeJoint>();
        thighsHinge = bodyParts[3].GetComponent<HingeJoint>();
        legsHinge = bodyParts[4].GetComponent<HingeJoint>();
        feetHinge = bodyParts[5].GetComponent<HingeJoint>();


        headSpring = headHinge.spring;
        armsSpring = armsHinge.spring;
        stomachSpring = stomachHinge.spring;
        thighsSpring = thighsHinge.spring;
        legsSpring = legsHinge.spring;
        feetSpring = feetHinge.spring;


        for (int i = 0; i < bodyParts.Length; i++) {
            bodyParts[i].GetComponent<Rigidbody>().maxAngularVelocity = maxVelocity;
        }

        if (PlayerPrefs.HasKey("SavedRegrabs")) {
            regrabs = PlayerPrefs.GetInt("SavedRegrabs");
        } else {
            regrabs = 0;
        }

        regrabText.text = regrabs + " regrabs";

        barPositions = new Vector3[bars.Length];
        for (int i=0; i<bars.Length; i++) {
            barPositions[i] = bars[i].transform.position;
            Debug.Log(barPositions[i]);
        }

    }


    void Update() {
        if (Input.GetKey(KeyCode.W)) {
            ArchState();
        }
        else if (Input.GetKey(KeyCode.E)) {
            TuckState();
        } 
        else {
            NormalState();
        }

        if (Input.GetKeyDown("space")) {
            if (onBar) LetGo();
        }

        // if (!onBar) {
        //     if (Input.GetKey(KeyCode.S)) {
        //         twist(20f);
        //     } else {
        //         twist(0);
        //     }
        // }

        if (Input.GetKeyDown(KeyCode.Q)) {
              SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }



        transform.position = new Vector3(stomach.transform.position.x, stomach.transform.position.y, stomach.transform.position.z + 3);
    }

    void FixedUpdate() {

        Vector3[] velocities = new Vector3[6];
        Vector3[] angularVelocities = new Vector3[6];

        int caughtBar = catchBar();
        if (caughtBar != -1 && !onBar) {
            var velData = regripBar(caughtBar);
            velocities = velData.Item1;
            angularVelocities = velData.Item2;
        }

        if (onBar == true && arms.GetComponent<HingeJoint>() == null && atBar(numCurrentBar) == true) {
            attachHinge(numCurrentBar, velocities, angularVelocities);
        }

        //updateBars();
    }

    void createJoints() {
        for (int i = 0; i < bodyParts.Length; i++) {
            bodyParts[i].GetComponent<Rigidbody>().isKinematic = false;
        }
    }

    void destroyJoints() {
        for (int i = 0; i < bodyParts.Length; i++) {
            bodyParts[i].GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    void NormalState() {

        setPosition(0, 90, 130, 0, 0);
    }

    void TuckState() {

        setPosition(-60, 140, 150, -140, 0);

    }

    void ArchState() {
        setPosition(30, -10, -10, -20, -60);
    }

    void setPosition(float headTarget, float stomachTarget, float thighsTarget, float legsTarget, float feetTarget) {
        
        headSpring.spring = headStrength;
        headSpring.damper = headDamper;
        headSpring.targetPosition = headTarget;
        headHinge.spring = headSpring;

        stomachSpring.spring = stomachStrength;
        stomachSpring.damper = stomachDamper;
        stomachSpring.targetPosition = stomachTarget;
        stomachHinge.spring = stomachSpring;
        
        thighsSpring.spring = thighsStrength;
        thighsSpring.damper = thighsDamper;
        thighsSpring.targetPosition = thighsTarget;
        thighsHinge.spring = thighsSpring;

        legsSpring.spring = legsStrength;
        legsSpring.damper = legsDamper;
        legsSpring.targetPosition = legsTarget;
        legsHinge.spring = legsSpring;

        feetSpring.spring = feetStrength;
        feetSpring.damper = feetDamper;
        feetSpring.targetPosition = feetTarget;
        feetHinge.spring = feetSpring;

    }

    void LetGo() {
        Destroy(armsHinge);
        onBar = false;
    }

    float xyDistance(GameObject a, GameObject b) {
        float x1 = a.transform.position.x;
        float x2 = b.transform.position.x;
        float y1 = a.transform.position.y;
        float y2 = b.transform.position.y;
        return (float)Math.Sqrt(Math.Pow(x1-x2, 2) + Math.Pow(y1-y2, 2));
    }

    int catchBar() {

        if (!onBar) { 
            for (int i = 0; i < bars.Length; i++) {
                if (xyDistance(hand1, bars[i]) < catchTolerance) {
                    if (dontCatch == false) {
                        return i;     
                    } else {
                        return -1;
                    }
                }
            }
            if (dontCatch) {
                dontCatch = false;
            }
        }


        return -1;
    }

    Tuple<Vector3[], Vector3[]> regripBar(int numBar) {
        incrementRegrabs();
        Vector3[] velocities = new Vector3[6];
        Vector3[] angularVelocities = new Vector3[6];
        for (int i = 0; i < bodyParts.Length; i++) {
            velocities[i] = bodyParts[i].GetComponent<Rigidbody>().velocity;
            angularVelocities[i] = bodyParts[i].GetComponent<Rigidbody>().angularVelocity;
        }

        correctPosition(numBar);
        numCurrentBar = numBar;
        onBar = true;
        return Tuple.Create(velocities, angularVelocities);
    }

    void attachHinge(int numBar, Vector3[] velocities, Vector3[] angularVelocities) {
        HingeJoint joint = arms.AddComponent<HingeJoint>();
        joint.anchor = new Vector3(0, 4.02f, 0);
        joint.axis = new Vector3(0, 0, 1);
        joint.connectedBody = bars[numBar].GetComponent<Rigidbody>();
        bars[numBar].GetComponent<Rigidbody>().isKinematic = true;

        armsHinge = joint;
        dontCatch = true;

        createJoints();

        for (int i = 0; i < bodyParts.Length; i++) {
            bodyParts[i].GetComponent<Rigidbody>().velocity = velocities[i];
            bodyParts[i].GetComponent<Rigidbody>().angularVelocity = angularVelocities[i];
        }

    }

    void shiftPosition(GameObject a, float xShift, float yShift) {
        a.transform.position = new Vector3(a.transform.position.x + xShift, a.transform.position.y + yShift, a.transform.position.z);
    }

    bool atBar(int numBar) {
        if (Math.Abs(bars[numBar].transform.position.x - hand1.transform.position.x) < 0.0001) {
            if (Math.Abs(bars[numBar].transform.position.y - hand1.transform.position.y) < 0.0001) {
                return true;
            }
        }
        return false;
    }

    void correctPosition(int numBar) {
        float xShift = bars[numBar].transform.position.x - hand1.transform.position.x;
        float yShift = bars[numBar].transform.position.y - hand1.transform.position.y;
        destroyJoints();

        for (int i = 0; i < bodyParts.Length; i++) {
            shiftPosition(bodyParts[i], xShift, yShift);
        }
    }  

    void twist(float speed) {
        for (int i = 0; i < bodyParts.Length; i++) {
            //bodyParts[i].GetComponent<Rigidbody>().angularVelocity = new Vector3(speed, bodyParts[i].GetComponent<Rigidbody>().angularVelocity.y, bodyParts[i].GetComponent<Rigidbody>().angularVelocity.z);
            bodyParts[i].GetComponent<Rigidbody>().AddRelativeTorque(Vector3.up*speed);
        }
    }

    void incrementRegrabs() {
        regrabs++;
        PlayerPrefs.SetInt("SavedRegrabs", regrabs);
        regrabText.text = regrabs + " regrabs";
    }

    void updateBars() {
        for (int i=0; i<bars.Length; i++) {
            Vector3 barPos = barPositions[i];
            GameObject bar = bars[i];
            bar.GetComponent<Rigidbody>().AddForce(-springConstant*(bar.transform.position-barPos) - damper*bar.GetComponent<Rigidbody>().velocity);
            bar.transform.rotation = Quaternion.Euler(90, 0, 0);
        }
    }

}
