using UnityEngine;
using Oculus.Interaction;

namespace MuseumOfSound
{
    [RequireComponent(typeof(Rigidbody), typeof(Grabbable), typeof(GrabInteractable))]
    [RequireComponent(typeof(AudioSource))]
    public sealed class MuseumInstrument : MonoBehaviour
    {
        public enum PlayStyle { Silent, Guitar, Flute }
        [SerializeField] private PlayStyle playStyle;
        [SerializeField] private AudioClip[] notes;
        [SerializeField] private GrabInteractor leftGrab, rightGrab;
        [SerializeField] private float returnDelay = 15f;
        private Rigidbody body;
        private Grabbable grabbable;
        private GrabInteractable interactable;
        private AudioSource audioSource;
        private Vector3 homePosition;
        private Quaternion homeRotation;
        private float returnAt, nextNote;
        private int noteIndex;
        private OVRInput.Controller holdingController;
        public bool IsHeld => grabbable != null && grabbable.GrabPoints.Count > 0;
        public int NoteIndex => noteIndex;
        public int PlaybackCount { get; private set; }
        public void Configure(PlayStyle style, AudioClip[] clips, GrabInteractor left, GrabInteractor right)
        { playStyle=style;notes=clips;leftGrab=left;rightGrab=right; }
        private void Awake()
        {
            body=GetComponent<Rigidbody>();grabbable=GetComponent<Grabbable>();
            interactable=GetComponent<GrabInteractable>();audioSource=GetComponent<AudioSource>();
            homePosition=transform.position;homeRotation=transform.rotation;returnAt=float.PositiveInfinity;
        }
        private void OnEnable() { if(interactable!=null)interactable.WhenPointerEventRaised+=OnPointer; }
        private void OnDisable() { if(interactable!=null)interactable.WhenPointerEventRaised-=OnPointer; if(audioSource!=null)audioSource.Stop(); }
        private void OnPointer(PointerEvent evt)
        {
            if(evt.Type==PointerEventType.Select) {
                holdingController=leftGrab!=null && evt.Identifier==leftGrab.Identifier?OVRInput.Controller.LTouch:OVRInput.Controller.RTouch;
                returnAt=float.PositiveInfinity;
            }
            if(evt.Type==PointerEventType.Unselect || evt.Type==PointerEventType.Cancel) {
                returnAt=Time.time+returnDelay;holdingController=OVRInput.Controller.None;
                if(playStyle==PlayStyle.Flute)audioSource.Stop();
            }
        }
        private void Update()
        {
            if(!IsHeld) {
                if(transform.position.y<-.5f || Time.time>=returnAt)ReturnHome();
                return;
            }
            if(notes==null || notes.Length==0 || holdingController==OVRInput.Controller.None)return;
            ApplyPlayInput(OVRInput.GetDown(OVRInput.Button.One,holdingController),OVRInput.GetDown(OVRInput.Button.Two,holdingController),OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger,holdingController),OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger,holdingController));
        }
        public void ApplyPlayInput(bool next,bool previous,bool triggerPressed,float triggerAmount)
        {
            if(!IsHeld || notes==null || notes.Length==0)return;
            if(next)noteIndex=(noteIndex+1)%notes.Length;
            if(previous)noteIndex=(noteIndex+notes.Length-1)%notes.Length;
            if(playStyle==PlayStyle.Guitar && triggerPressed)PlayNote();
            if(playStyle==PlayStyle.Flute) {
                bool blowing=triggerAmount>.2f;
                if(blowing && Time.time>=nextNote){PlayNote();nextNote=Time.time+.55f;}
                if(!blowing)audioSource.Stop();
            }
        }
        public void PlayNote()
        {
            if(notes==null || notes.Length==0)return;
            if(playStyle==PlayStyle.Flute)audioSource.Stop();
            audioSource.PlayOneShot(notes[noteIndex],.65f);PlaybackCount++;
        }
        public void ReturnHome()
        {
            if(IsHeld)return;
            if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
            body.isKinematic=true;
            body.position=homePosition;body.rotation=homeRotation;
            transform.SetPositionAndRotation(homePosition,homeRotation);
            audioSource.Stop();returnAt=float.PositiveInfinity;
        }
    }
}


