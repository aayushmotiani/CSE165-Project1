using UnityEditor;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;

public class ObjectSpawning : MonoBehaviour
{
    [SerializeField]private InputActionProperty X;
    [SerializeField]private InputActionProperty Y;
    [SerializeField]private InputActionAsset playerMap;
    private GameObject playerObj;
    [SerializeField]private GameObject syringe;
    [SerializeField]private GameObject mask;
    [SerializeField] private Transform spawnPos;

    //private bool xPress;
    //private bool yPress;
    private Rigidbody rb;

    void Awake()
    {
        X.action.Enable();
        Y.action.Enable();
    }
    void Start()
    {
        playerObj = this.gameObject;
    }

    void Update()
    {
        if(Input.GetKey(KeyCode.C)){
            mask.GetComponent<Renderer>().sharedMaterial.color = Color.white * 20f;
        }
        if(X.action.triggered){
            Instantiate(syringe, spawnPos.position, Quaternion.identity);
        } 
        if(Y.action.triggered){
            Instantiate(mask, spawnPos.position, Quaternion.identity);
        }
    }
}