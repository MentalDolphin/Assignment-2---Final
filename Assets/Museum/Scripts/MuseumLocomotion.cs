using UnityEngine;
namespace MuseumOfSound
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class MuseumLocomotion : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField] private float moveSpeed=1.5f;
        [SerializeField] private float snapDegrees=30f;
        private CharacterController body;
        private bool turnReady=true;
        private MuseumTeleportation teleportation;
        public void Configure(Transform cameraHead){head=cameraHead;}
        private void Awake(){body=GetComponent<CharacterController>();teleportation=GetComponent<MuseumTeleportation>();}
        private void Update()
        {
            if(head==null || !OVRInput.IsControllerConnected(OVRInput.Controller.Touch))return;
            float height=Mathf.Clamp(head.localPosition.y,.9f,2.2f);
            // The head may be nested under TrackingSpace, so use the rig-relative position.
            Vector3 localHead=transform.InverseTransformPoint(head.position);
            height=Mathf.Clamp(localHead.y,.9f,2.2f);
            body.height=height;body.center=new Vector3(localHead.x,height/2f+.02f,localHead.z);
            if(teleportation!=null && (teleportation.IsAiming || OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick,OVRInput.Controller.RTouch).y>.65f))return;
            Vector2 axis=OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick,OVRInput.Controller.LTouch);
            Vector3 forward=Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized;
            Vector3 right=Vector3.Cross(Vector3.up,forward);
            Vector3 motion=(forward*axis.y+right*axis.x)*moveSpeed;
            if(axis.magnitude<.18f)motion=Vector3.zero;
            body.Move((motion+Vector3.down*.5f)*Time.deltaTime);
            float turn=OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick,OVRInput.Controller.RTouch).x;
            if(Mathf.Abs(turn)<.3f)turnReady=true;
            if(turnReady && Mathf.Abs(turn)>.7f){transform.RotateAround(head.position,Vector3.up,Mathf.Sign(turn)*snapDegrees);turnReady=false;}
        }
    }
}
