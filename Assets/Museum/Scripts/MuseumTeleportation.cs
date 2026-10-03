using UnityEngine;
using Oculus.Interaction;
namespace MuseumOfSound
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class MuseumTeleportation : MonoBehaviour
    {
        [SerializeField] private Transform head,controller;
        [SerializeField] private GrabInteractor rightGrab;
        [SerializeField] private LineRenderer ray;
        private CharacterController body;
        private MuseumTeleportStation candidate;
        public bool IsAiming {get;private set;}
        public int TeleportCount {get;private set;}
        public void Configure(Transform eye,Transform hand,GrabInteractor grab,LineRenderer line)
        {head=eye;controller=hand;rightGrab=grab;ray=line;}
        private void Awake(){body=GetComponent<CharacterController>();}
        private void Update()
        {
            bool connected=OVRInput.IsControllerConnected(OVRInput.Controller.RTouch);
            Vector2 axis=OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick,OVRInput.Controller.RTouch);
            bool blocked=rightGrab!=null && rightGrab.SelectedInteractable!=null;
            bool cancel=OVRInput.Get(OVRInput.Button.PrimaryHandTrigger,OVRInput.Controller.RTouch);
            if(!connected || blocked || cancel){EndAim(false);return;}
            if(!IsAiming && axis.y>.65f && Mathf.Abs(axis.x)<.5f)IsAiming=true;
            if(!IsAiming)return;
            if(axis.magnitude<.25f){EndAim(true);return;}
            if(axis.y<-.5f){EndAim(false);return;}
            UpdateAim();
        }
        private void UpdateAim()
        {
            candidate=null;
            Vector3 start=controller.position+controller.forward*.08f;
            Vector3 end=start+controller.forward*15f;
            if(Physics.Raycast(start,controller.forward,out RaycastHit hit,15f,~0,QueryTriggerInteraction.Ignore))
            {end=hit.point;candidate=hit.collider.GetComponentInParent<MuseumTeleportStation>();}
            ray.enabled=true;ray.SetPosition(0,start);ray.SetPosition(1,end);
            ray.startColor=ray.endColor=candidate!=null?new Color(.15f,1f,.65f):new Color(1f,.35f,.15f);
        }
        private void EndAim(bool commit)
        {
            if(commit && IsAiming && candidate!=null)TryTeleport(candidate);
            IsAiming=false;candidate=null;if(ray!=null)ray.enabled=false;
        }
        public bool TryTeleport(MuseumTeleportStation station)
        {
            if(station==null || station.LandingPoint==null || head==null)return false;
            Vector3 target=station.LandingPoint.position;
            // Refuse a landing occupied by scenery or a dropped solid instrument.
            Vector3 bottom=target+Vector3.up*.30f,top=target+Vector3.up*1.55f;
            if(Physics.CheckCapsule(bottom,top,.22f,~0,QueryTriggerInteraction.Ignore))return false;
            bool wasEnabled=body.enabled;body.enabled=false;
            Vector3 forward=Vector3.ProjectOnPlane(head.forward,Vector3.up);
            if(forward.sqrMagnitude>.001f)
                transform.RotateAround(head.position,Vector3.up,Vector3.SignedAngle(forward,station.LandingPoint.forward,Vector3.up));
            // Room-scale head offset must land on the marker, rather than the rig origin.
            Vector3 offset=target-head.position;offset.y=target.y-transform.position.y;
            transform.position+=offset;Physics.SyncTransforms();body.enabled=wasEnabled;
            TeleportCount++;return true;
        }
        private void OnDisable(){EndAim(false);}
    }
}
