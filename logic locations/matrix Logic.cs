using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

//steps in order
/*
1. Perform the medical hand washing. (sanitizing)
2. Clean and arrange the dressing trolley. (putting to trolley)
3. Again, perform the medical hand washing. (sanitizing)
4. Take the dressing solutions and check the solutions (Name, expiration date etc) with staff nurse. (taking to hand)
5. Take the sterile dressing packet from the sterile cupboard and check the dressing packet. Confirm the sterile packet with staff nurse. (putting to the trolley)
6. Bring the arranged trolley with equipment and solutions to the patient area. (taking the trolley to location)
*/

public class MatrixLogic : MonoBehaviour
{
    public List<bool> userMatrixBool = new List<bool>() { false, false, false, false, false, false };

    // References
    public Transform trolleyObject;
    public Transform bedObject;
    public Transform sanitizerBottle;
    public Transform handHoldingBottle;
    public Transform otherHand;
    public Transform triangleObject;
    public Transform rectangleObject;
    public Transform dressigSolution;
    public Transform sterilizePacket;

    public RequestHandler requestHandler;
    public QuestionManager QuestionManager;

    //initializing
    public SanitizerChecker sanitizerChecker;
    public TrolleyChecker trolleyChecker;
    public TrolleyArranger trolleyArranger;
    public ObjectInteractionChecker objectInteractionChecker;

    //on matrix monitor
    private List<bool> previousMatrixState;
    private float lastChangeTime;

    //step numbers
    public int step1 = 0;
    public int step2 = 1;
    public int step3 = 2;
    public int step4 = 3;
    public int step5 = 4;
    public int step6 = 5;
    public string APIEndPointURL = "/evaluate/step";

    void Update()
    {
        //if (!QuestionManager.isActivateStepsLogic) return; //wait until the quiz is finished

        // Only check every N frames for optimization
        if (Time.frameCount % 3 == 0)
        {
            callSteps();
            lookMatrixChanges();
        }
    }

    void callSteps()
    {
        // Check trolley position
        if (trolleyChecker != null && trolleyObject != null && bedObject != null)
        {
            trolleyChecker.CheckTrolley(trolleyObject, bedObject, this);
        }

        // Check sanitizer
        if (sanitizerChecker != null && sanitizerBottle != null &&
            handHoldingBottle != null && otherHand != null)
        {
            sanitizerChecker.CheckSanitizer(sanitizerBottle, handHoldingBottle, otherHand, this);
        }

        //check arrange trolley
        if (trolleyArranger != null && trolleyObject != null &&
            triangleObject != null && rectangleObject != null)
        {
            trolleyArranger.ArrangeTrolley(trolleyObject, this, step2, triangleObject, rectangleObject);
        }

        //check take from trolley
        if (objectInteractionChecker != null && trolleyObject != null &&
            handHoldingBottle != null && dressigSolution != null)
        {
            objectInteractionChecker.CheckObjectTakenToHand(dressigSolution, handHoldingBottle, this, 1);
        }

        //check sterilization packet put to the trolley
        if (trolleyArranger != null && trolleyObject != null &&
            sterilizePacket != null)
        {
            trolleyArranger.ArrangeTrolley(trolleyObject, this, step5, sterilizePacket);
        }
    }

    void lookMatrixChanges()
    {
        float maxTimeWithoutChange = 120f;
        // Initialize previous state if first run
        if (previousMatrixState == null)
        {
            previousMatrixState = new List<bool>(userMatrixBool);
            lastChangeTime = Time.time;
            return;
        }

        // Check if matrix has changed
        bool hasChanged = false;
        if (userMatrixBool.Count == previousMatrixState.Count)
        {
            for (int i = 0; i < userMatrixBool.Count; i++)
            {
                if (userMatrixBool[i] != previousMatrixState[i])
                {
                    hasChanged = true;
                    break;
                }
            }
        }
        else
        {
            hasChanged = true;
        }

        // If changed or 2 minutes passed without changes
        if (hasChanged || Time.time - lastChangeTime >= maxTimeWithoutChange)
        {
            APIEndPoint(userMatrixBool);
            previousMatrixState = new List<bool>(userMatrixBool);
            lastChangeTime = Time.time;
        }
    }

    void APIEndPoint(List<bool> userMatrixBool)
    {
        bool[] matrixArray = userMatrixBool.ToArray();
        Debug.Log("Sending API: " + string.Join(", ", userMatrixBool));

        MatrixData matrixData = new MatrixData(userMatrixBool);
        string jsonString = JsonUtility.ToJson(matrixData);
        requestHandler.SendApiRequest(APIEndPointURL, jsonString);
    }
}

[System.Serializable]
public class MatrixData
{
    public List<bool> matrix;

    public MatrixData(List<bool> matrix)
    {
        this.matrix = matrix;
    }
}

[System.Serializable]
public class SanitizerChecker
{

    // Threshold distances for considering the bottle "grabbed"
    private int grabDistanceThreshold = 1;

    // Threshold angle for considering the bottle pointed at the other hand
    private float angleThreshold = 60f;

    public void CheckSanitizer(Transform sanitizerBottle, Transform handHoldingBottle, Transform otherHand, MatrixLogic matrixLogic)
    {
        if (sanitizerBottle == null || handHoldingBottle == null || otherHand == null) return;

        // Check if bottle is grabbed (close enough to the hand)
        float distanceToHand = Vector3.Distance(sanitizerBottle.position, handHoldingBottle.position);
        bool isGrabbed = distanceToHand < grabDistanceThreshold;

        Debug.Log("sanatizer distanceToHand: " + distanceToHand + " isGrabbed: " + isGrabbed);

        if (!isGrabbed)
        {
            return;
        }

        // Check if bottle is pointing towards the other hand
        Vector3 bottleForward = sanitizerBottle.forward;
        Vector3 directionToOtherHand = (otherHand.position - sanitizerBottle.position).normalized;

        float angle = Vector3.Angle(bottleForward, directionToOtherHand);
        bool isPointingCorrectly = angle <= angleThreshold;
        Debug.Log("sanatizer angle: " + angle + " isPointingCorrectly: " + isPointingCorrectly + " angleThreshold: " + angleThreshold);

        if (isPointingCorrectly)
        {
            if (matrixLogic.userMatrixBool[matrixLogic.step1] == true &&
                (matrixLogic.userMatrixBool[matrixLogic.step2] == true ||
                matrixLogic.userMatrixBool[matrixLogic.step4] == true ||
                matrixLogic.userMatrixBool[matrixLogic.step5] == true ||
                matrixLogic.userMatrixBool[matrixLogic.step6] == true))
            {
                matrixLogic.userMatrixBool[matrixLogic.step3] = true;
                //Debug.Log("sanatizer step3: ");
                Debug.Log("sanatizer User Matrix: " + string.Join(", ", matrixLogic.userMatrixBool));
            }
            else
            {
                matrixLogic.userMatrixBool[matrixLogic.step1] = true;
                //Debug.Log("sanatizer step1: ");
                Debug.Log("sanatizer User Matrix: " + string.Join(", ", matrixLogic.userMatrixBool));
            }
        }
    }
}

[System.Serializable]
public class TrolleyChecker
{
    private float minX = -1.3f;
    private float maxX = -0.9f;
    private float minY = -0.3f;
    private float maxY = 0.3f;
    private float minZ = -0.3f;
    private float maxZ = 0.6f;

    public void CheckTrolley(Transform obj, Transform reference, MatrixLogic matrixLogic)
    {
        if (obj == null || reference == null || matrixLogic == null) return;

        Vector3 relativePos = obj.position - reference.position;

        bool isInRange =
            relativePos.x >= minX && relativePos.x <= maxX &&
            relativePos.y >= minY && relativePos.y <= maxY &&
            relativePos.z >= minZ && relativePos.z <= maxZ;
        Debug.Log("trolleyloc obj2RelativePos.x " + relativePos.x + " obj2RelativePos.y: " + relativePos.y + " obj2RelativePos.z: " + relativePos.z + " isInRange: " + isInRange);

        if (isInRange)
        {
            matrixLogic.userMatrixBool[matrixLogic.step6] = true;
        }
    }
}

[System.Serializable]
public class TrolleyArranger
{
    // Define trolley bounds
    private float trolleyMinX = -0.5f;
    private float trolleyMaxX = 0.5f;
    private float trolleyMinY = 0.1f;
    private float trolleyMaxY = 0.9f;
    private float trolleyMinZ = -0.34f;
    private float trolleyMaxZ = 0.34f;
    private bool isObj2OnTrolley;
    private bool isObj1OnTrolley;

    public bool ArrangeTrolley(Transform trolley, MatrixLogic matrixLogic, int step, Transform obj1 = null, Transform obj2 = null)
    {
        if (Time.frameCount < 5) return false;
        if (trolley == null) return false;
        Debug.Log("trolley strted ");


        if (obj2 != null)
        {
            Vector3 obj2RelativePos = obj2.position - trolley.position;
            isObj2OnTrolley =
                obj2RelativePos.x >= trolleyMinX && obj2RelativePos.x <= trolleyMaxX &&
                obj2RelativePos.y >= trolleyMinY && obj2RelativePos.y <= trolleyMaxY &&
                obj2RelativePos.z >= trolleyMinZ && obj2RelativePos.z <= trolleyMaxZ;

            Debug.Log("trolley obj2RelativePos.x " + obj2RelativePos.x + " obj2RelativePos.y: " + obj2RelativePos.y + " obj2RelativePos.z: " + obj2RelativePos.z + " isObj2OnTrolley: " + isObj2OnTrolley);
        }
        else
        {
            isObj2OnTrolley = true;
        }

        if (obj1 != null)
        {
            Vector3 obj1RelativePos = obj1.position - trolley.position;
            isObj1OnTrolley =
                obj1RelativePos.x >= trolleyMinX && obj1RelativePos.x <= trolleyMaxX &&
                obj1RelativePos.y >= trolleyMinY && obj1RelativePos.y <= trolleyMaxY &&
                obj1RelativePos.z >= trolleyMinZ && obj1RelativePos.z <= trolleyMaxZ;

            Debug.Log("trolley obj1RelativePos.x " + obj1RelativePos.x + " obj1RelativePos.y: " + obj1RelativePos.y + " obj1RelativePos.z: " + obj1RelativePos.z + " isObj1OnTrolley: " + isObj1OnTrolley);
        }
        else
        {
            return false;
        }

        bool allObjectsOnTrolley = isObj1OnTrolley && isObj2OnTrolley;

        if (allObjectsOnTrolley)
        {
            matrixLogic.userMatrixBool[step] = true;
            Debug.Log("trolley changed the matrix ");
        }
        return allObjectsOnTrolley;
    }
}

[System.Serializable]
public class ObjectInteractionChecker
{
    public bool CheckObjectTakenToHand(Transform objectOnTrolley, Transform hand, MatrixLogic matrixLogic, float pickupThreshold = 0.5f)
    {
        if (objectOnTrolley == null || hand == null) return false;

        float distanceToHand = Vector3.Distance(objectOnTrolley.position, hand.position);
        bool isObjectTaken = distanceToHand <= pickupThreshold;

        if (isObjectTaken)
        {
            matrixLogic.userMatrixBool[matrixLogic.step4] = true;
        }

        return isObjectTaken;
    }

    public bool IsObjectOnTrolley(Transform obj, Transform trolley, Vector3 trolleyHalfExtents)
    {
        if (obj == null || trolley == null) return false;

        Vector3 relativePos = trolley.InverseTransformPoint(obj.position);
        return Mathf.Abs(relativePos.x) <= trolleyHalfExtents.x &&
               relativePos.y >= 0 && relativePos.y <= trolleyHalfExtents.y &&
               Mathf.Abs(relativePos.z) <= trolleyHalfExtents.z;
    }
}