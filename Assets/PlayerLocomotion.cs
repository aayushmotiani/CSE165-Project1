using UnityEditor;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEditor.ShaderKeywordFilter;

public class PlayerLocomotion : MonoBehaviour
{
    [SerializeField]private InputActionProperty moveReference;
    [SerializeField]private InputActionProperty L3;
    [SerializeField]private InputActionProperty snapReference;
    //[SerializeField]private InputActionAsset playerMap;
    [SerializeField]private GameObject cameraObj;
    [SerializeField]private GameObject rayEndPos;
    [SerializeField]private GameObject leftController;
    [SerializeField]private GameObject bottomArrow;
    [SerializeField]private GameObject leftArrow;
    [SerializeField]private GameObject rightArrow;
    private Vector2 moveValue;
    private Vector2 snapValue;
    private Rigidbody rb;
    [SerializeField]private float moveForce = 40f;
    [SerializeField]private float snapForce = 15f;
    private bool canRotate = true;
    private bool toggleMovementMode = false;
    private float L3Press;
    private Vector3 P3;
    private Vector3 P2;
    private Vector3 P1;
    private Vector3 P0;
    private LineRenderer lr;

    void Awake()
    {
        moveReference.action.Enable();
        snapReference.action.Enable();
        L3.action.Enable();
    }
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearDamping = 6f;
        rb.angularDamping = 10f;
        lr = leftController.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = 0.02f;
        lr.endWidth = 0.02f;
        lr.useWorldSpace = true;
        BottomArrowInactive();
        LeftArrowInactive();
        RightArrowInactive();
    }

    void Update()
    {        
        L3Press = L3.action.ReadValue<float>();
        moveValue = moveReference.action.ReadValue<Vector2>();
        snapValue = snapReference.action.ReadValue<Vector2>();
        //check for L3 input to switch movement type
        if(L3Press>0.5f){
            Debug.Log("L3 Press");
            RenderLine();
            Vector3 forward = leftController.transform.forward;
            forward.y = 0;
            forward.Normalize();
        }
        if(moveValue!=Vector2.zero){
            Debug.Log("Arrow Spawn");
            bottomArrow.SetActive(true);
            //bottomArrow.transform.forward = cameraObj.transform.forward;
        }else{
            Invoke("BottomArrowInactive",0.1f);
        }
    }
    void FixedUpdate()
    {
        Vector3 moveDir = cameraObj.transform.forward * moveValue.y + cameraObj.transform.right * moveValue.x;  //combined movt direction, moveValue is y & x values, positive or negative from joystick
        rb.AddForce(moveDir*moveForce, ForceMode.Impulse);

        //Rotation
        
        if(snapValue.x>0f && canRotate){ 
            Debug.Log("Rotation stick moved");
            rb.AddTorque(Vector3.up*snapForce, ForceMode.Impulse);
            //transform.Rotate(0f, snapAngle, 0f);
            canRotate = false;
            Invoke("EnableRotation",0.15f);
            //right arrow spawn
            rightArrow.SetActive(true);
        }
        else if(snapValue.x < 0f && canRotate){
            Debug.Log("Rotation stick moved");
            rb.AddTorque(Vector3.up*-snapForce, ForceMode.Impulse);
            //transform.Rotate(0f, -snapAngle, 0f);
            canRotate = false;
            Invoke("EnableRotation",0.15f);
            //left arrow spawn
            leftArrow.SetActive(true);
        }
        if(snapValue.x == 0f){
            LeftArrowInactive();
            RightArrowInactive();
        }
    }
    void RenderLine(){
        //line renderer, render line from Left Controller Position
        //Bezier Equation: B(t)=(1−t)^3*P0 + 3(1−t)^2*t*P1 + 3*(1−t)*(t^2)*P2 + t^3*P3
        Vector3 forward = leftController.transform.forward;
        forward.y = 0;
        forward.Normalize();

        P3 = leftController.transform.position;
        P0 = P3 + (forward * 3f); // end point in forward direction
        P1 = P0 + (Vector3.up * 2f) + forward * 3f;
        P2 = P3 + (Vector3.up * 2f) - forward * 1f;
        P0.y = 0f;

        Vector3[] positions = new Vector3[12];
        positions[0] = P0;
        positions[11] = P3;

        for(int i = 0; i<10; i+=1){
            float t = i/10f;
            Vector3 newPoint = (Mathf.Pow((1-t),3))*P0 + 3*(Mathf.Pow((1-t),2))*t*P1 + 3*(1-t)*(Mathf.Pow(t,2))*P2 + (Mathf.Pow(t,3))*P3;
            positions[i+1] = newPoint;
        }
        Instantiate(rayEndPos, leftController.transform.position + (forward * 3f), Quaternion.identity);
        
        lr.positionCount = positions.Length;
        lr.SetPositions(positions);
    }
    void EnableRotation(){
        canRotate=true;
    }
    void BottomArrowInactive(){
        bottomArrow.SetActive(false);
    }
    void RightArrowInactive(){
        rightArrow.SetActive(false);
    }
    void LeftArrowInactive(){
        leftArrow.SetActive(false);
    }
}