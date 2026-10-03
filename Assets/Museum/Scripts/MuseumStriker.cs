using UnityEngine;
namespace MuseumOfSound
{
    public sealed class MuseumStriker : MonoBehaviour
    {
        public enum StrikerKind { Controller, Drumstick, Bow }
        [SerializeField] private StrikerKind kind;
        [SerializeField] private MuseumInstrument instrument;
        [SerializeField] private OVRInput.Controller controller;
        private Vector3 previousPosition;
        public StrikerKind Kind => kind;
        public float Speed { get; private set; }
        public bool IsActive => instrument!=null?instrument.IsHeld:OVRInput.IsControllerConnected(controller);
        public void Configure(StrikerKind value,MuseumInstrument owner,OVRInput.Controller input=OVRInput.Controller.None)
        {kind=value;instrument=owner;controller=input;}
        private void OnEnable(){previousPosition=transform.position;Speed=0;}
        private void FixedUpdate(){Speed=(transform.position-previousPosition).magnitude/Time.fixedDeltaTime;previousPosition=transform.position;}
    }
}
