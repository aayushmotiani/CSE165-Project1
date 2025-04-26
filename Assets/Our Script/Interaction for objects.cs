using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEditor;
using System;
using TMPro;
using static UnityEngine.Rendering.VolumeComponent;

public class Interactionforobjects : MonoBehaviour
{
    //public XRNode[] controllerNodes = new XRNode[2] {XRNode.LeftHand, XRNode.RightHand};
    public LineRenderer[] lineRenderers; // ray cast line renderer
    public Transform[] controllerTransform; // assign transform for both controllers
    public InputActionProperty[] gripAction; // action map for both grip button on controller
    public InputActionProperty[] triggerAction; // action map for both trigger button on controller
    public float maxRayDistance = 5f; // ray length
    public LayerMask grabbableLayer; // grabbable object

    public float ObjMoveSpeed = 20f; // object moving speed with controller
    //public float ObjRotateSpeed = 100f; // object roatation speed with controller

    //private InputDevice[] devices = new InputDevice[2]; // gets Input from both hands
    private GameObject selectedObject = null; // get selected Object 
    private Vector3 selectionLocalOffset; // distance between ray hit point and object center,
    private Quaternion prevRotation; // controller rotation at prev frame

    private float initialDistance; // intial between controller distance
    private float controlToRayHitDistance; // distance of controller to ray hit point 
    private Vector3 initialScale; // initial object scale vector
    private Color originalColor; // initial object color
    private bool[] isGripping = new bool[2]; //store selected object result for each controller
    private bool snapToController = false; // track if the object is close and should snap
    private float grabSnapDistance = 0.5f; // theshold for object snapping
    private Vector3 localHitOffset; // save the relative local offset from object center to hit point
    private Vector3 initialOtherControllerPos; // controller position when second grip starts
    private Vector3 initialObjectPos; // object position when second grip starts
    public float moveSpeedMultiplier = 50f; //multipler for object following other hand movement
    private float objToActiveControllerDistance;

    private int activeControllerIndex = -1; // tracks which hand is currently manipulating: 0 -> lefthand; 1 -> righthand

    private float sphereRaycastRadius = 0.5f; // 

    void Awake()
    {
        for(int i=0; i < controllerTransform.Length; i++){
            gripAction[i].action.Enable();
            triggerAction[i].action.Enable();
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < controllerTransform.Length; i++)
        {
            bool triggerPressed = triggerAction[i].action.ReadValue<float>() > 0.5f; 
            bool gripPressed = gripAction[i].action.ReadValue<float>() > 0.5f;
            LineRenderer(i, gripPressed);

            //Use grip button to grab onto object
            if (gripPressed && !isGripping[i])
            {
                Debug.Log($"grip pressed on controller{i}");
                isGripping[i] = true;
                TrySelect(i);
            }
            else if (!gripPressed && isGripping[i])
            {
                Debug.Log($"grip released on controller{i}");
                isGripping[i] = false;
                if (selectedObject != null && activeControllerIndex == i)
                {
                    
                    DeSelect();
                }
            }

            if (!isGripping[i]) // only if not already gripping something
            {
                if (triggerPressed)
                {
                    TryTeleportPull(i);
                }
            }

            if (selectedObject != null && activeControllerIndex == i) // Controls
            {
                Debug.Log($"Controller{i} js manipulating object");
                Debug.Log($"gripPressed: {gripPressed}, triggerPressed: {triggerPressed}");
                if (gripPressed) { Movement(i); } // continous grip move

                if (triggerPressed) { Rotation(i); } // press trigger to rotate

                int otherController = 1 - i;
                bool otherGrip = gripAction[otherController].action.ReadValue<float>() > 0.5f;
                bool otherTriggerPressed = triggerAction[otherController].action.ReadValue<float>() > 0.5f;
                if (gripPressed && otherGrip)
                {
                    Scale(i, otherController); // trigger pressed - scale
                    /**
                    if (otherTriggerPressed)
                    {
                        Scale(i, otherController); // trigger pressed - scale
                    }
                    else
                    {
                        FollowOtherController(otherController); // only grip  - follow other controller motion
                    }
                    */

                }
                /**
                if (gripPressed && !otherGrip)
                {
                    UpdateAfterSecondGripReleased();
                }
                */

            }
        }
    }
    /**
     * 
     * Made to detect second control grip and kept OBJ position
     * 
     */
    void UpdateAfterSecondGripReleased()
    {
        if (selectedObject != null)
        {
            Vector3 controllerPos = controllerTransform[activeControllerIndex].position;
            Vector3 objectPos = selectedObject.transform.position;

            objToActiveControllerDistance = Vector3.Distance(controllerPos, objectPos);
            selectionLocalOffset = objectPos - (controllerPos + controllerTransform[activeControllerIndex].forward * objToActiveControllerDistance);

            Debug.Log($"Updated grabDistance: {objToActiveControllerDistance}, updated selectionLocalOffset: {selectionLocalOffset}");
        }
    }
    /**
     * parameter: 
     * controllerIndex - which controller is active
     * 
     * Tries to select an object by raycast from the a controller
     * 
     */
    void TryTeleportPull(int controllerIndex)
    {
        Ray ray = new Ray(controllerTransform[controllerIndex].position, controllerTransform[controllerIndex].forward);
        bool ifHit = Physics.SphereCast(ray, sphereRaycastRadius, out RaycastHit hitInfo, maxRayDistance, grabbableLayer);

        if (ifHit)
        {
            GameObject selectedGameObject = hitInfo.collider.gameObject;
            float forwardOffset = 0.3f; // distance obj teleport infront of controller
            Vector3 targetPos = controllerTransform[controllerIndex].position + controllerTransform[controllerIndex].forward * forwardOffset;

            selectedGameObject.transform.position = Vector3.Lerp(selectedGameObject.transform.position, targetPos,
                Time.deltaTime * ObjMoveSpeed * moveSpeedMultiplier); // higher speed for fast teleport feel

            Debug.Log($"Teleported object {selectedGameObject.name} toward controller {controllerIndex}");
        }
    }
    /**
     * 
     * parameter: 
     * controllerIndex - which controller is active
     * gripPressed - bool val
     *
     * Tries to select an object by raycast from the a controller
     *
     */
    void LineRenderer(int controllerIndex, bool gripPressed)
    {
        LineRenderer lr = lineRenderers[controllerIndex];
        lr.enabled = !gripPressed;

        if (!gripPressed)
        {
            Vector3 start = controllerTransform[controllerIndex].position;
            Vector3 end = start + controllerTransform[controllerIndex].forward * grabSnapDistance;

            Ray ray = new Ray(start, controllerTransform[controllerIndex].forward);
            bool ifHit = Physics.Raycast(ray, out RaycastHit hitInfo, maxRayDistance, grabbableLayer);

            if (!ifHit)
            {
                
                lr.material.color = Color.white;
            }
            else
            {
                end = hitInfo.point; // snap to hit point if something is hit
                lr.material.color = Color.red;
            }

            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
        }

    }

    /**
     * 
     * parameter: 
     * controllerIndex - which controller is active
     * 
     * Tries to select an object by raycast from the a controller
     * 
     */
    void TrySelect(int controllerIndex) 
    {
        Ray ray = new Ray(controllerTransform[controllerIndex].position, 
            controllerTransform[controllerIndex].forward); // shoot detection ray ahead
            bool ifHit = Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, grabbableLayer);
        if (ifHit) // if there is grabbableLayer exits on ray hit object
        {
            Debug.Log($"The ray {ifHit}");
            selectedObject = hit.collider.gameObject;
            selectedObject.GetComponent<Rigidbody>().isKinematic = true;

            selectionLocalOffset = selectedObject.transform.position - hit.point;
           
            //localHitOffset = selectedObject.transform.InverseTransformPoint(hit.point);
            controlToRayHitDistance = Vector3.Distance(hit.point, controllerTransform[controllerIndex].position);
            initialOtherControllerPos = controllerTransform[1 - controllerIndex].position;
            initialObjectPos = selectedObject.transform.position;

            snapToController = controlToRayHitDistance < grabSnapDistance; // bool

            prevRotation = controllerTransform[controllerIndex].rotation; //get controller position

            initialScale = selectedObject.transform.localScale; // get initial object scale
            initialDistance = Vector3.Distance(controllerTransform[0].position, 
                controllerTransform[1].position); // cal controller distance for two hand scaling
            activeControllerIndex = controllerIndex; //which controller is active
        }

    }

    /** 
     * 
     * Resets selected object and turn off hightlight
     * 
     */
    void DeSelect()
    {
        selectedObject.GetComponent<Rigidbody>().isKinematic = false;
        //Highlight(selectedObject, false); //reset selection highlight
        selectedObject = null; 
        activeControllerIndex = -1;
        
    }
    /**
     * parameter:
     * controllerIndex - the controller in action at the moment
     * 
     * Apply controller rotation to object rotation
     * 
     */
    void Rotation(int controllerIndex)
    {
        int otherController = 1 - controllerIndex;
        Quaternion currRoatation = controllerTransform[otherController].rotation; //change in rotation since last frame
        Quaternion deltaRoatation = currRoatation * Quaternion.Inverse(prevRotation);

        if (selectedObject != null)
        {
            Debug.Log($"Rotating: Applying delta rotation from controller {controllerIndex}");
            selectedObject.transform.rotation = deltaRoatation * selectedObject.transform.rotation; //Apply controller rotation change Quaternion to the object
        }
        else
        {
            Debug.LogWarning("No object selected to rotate.");
        }
        prevRotation = currRoatation; //update
    }
    /**
     * parameter:
     * index1 - the controller 1
     * index2 - the controller 2
     * 
     * Measure and apply object scale with two controller distance difference
     * 
     */
    void Scale(int index1, int index2)
    {
        float currDistance = Vector3.Distance(controllerTransform[index1].
            position, controllerTransform[index2].position); 
        float scaleFactor = currDistance / initialDistance; // how much to scale
        selectedObject.transform.localScale = initialScale * scaleFactor; //apply scaling factor
    }
    /**
     * parameter:
     * controllerIndex - the controller in action at the moment
     * 
     * Using other controller to push and pull on the object
     * 
     */
    void FollowOtherController(int otherControllerIndex)
    {
        /**Vector3 currentOtherPos = controllerTransform[otherControllerIndex].position;
        Vector3 controllerDelta = currentOtherPos - initialOtherControllerPos; // how much the second controller has moved since grip started
        Vector3 targetPos = initialObjectPos + controllerDelta * moveSpeedMultiplier; // apply that same delta to the object's initial position
        float dynamicSpeed = ObjMoveSpeed * Vector3.Distance(selectedObject.transform.position, targetPos);
        selectedObject.transform.position = Vector3.Lerp(selectedObject.transform.position,targetPos,
            Time.deltaTime * dynamicSpeed);

        */
    }
    /**
     * parameter:
     * controllerIndex - the controller in action at the moment
     * 
     * Smooth movement of the object following ray hit point
     * 
     */
    void Movement(int controllerIndex)
    {

        Vector3 controllerPos = controllerTransform[controllerIndex].position;
        Vector3 controllerForward = controllerTransform[controllerIndex].forward;

        if (snapToController)
        {
            float forwardOffset = 0.1f;
            Vector3 grabPoint = controllerPos + controllerForward * forwardOffset;
            Vector3 targetPos = controllerPos + selectionLocalOffset;

            selectedObject.transform.position = Vector3.Lerp(selectedObject.transform.position, targetPos, 
                Time.deltaTime * ObjMoveSpeed);
        }
        else
        {
            Vector3 rayHitPoint = controllerPos + controllerForward * controlToRayHitDistance;
            Vector3 targetPos = rayHitPoint + selectionLocalOffset;

            selectedObject.transform.position = Vector3.Lerp(selectedObject.transform.position, targetPos,
                Time.deltaTime * ObjMoveSpeed);
        }
        
    }


    /** 
     * parameters:
     * obj - selected object
     * tf - Highlight on or off
     * 
     * Controls Highlight
     * 
     */
    //void Highlight(GameObject obj, bool tf)
    //{
    //    Material rend = obj.GetComponent<Material>();
    //    if (rend == null) return;
    //    rend.color = tf ? Color.yellow : Color.white;
        
    //}
}
