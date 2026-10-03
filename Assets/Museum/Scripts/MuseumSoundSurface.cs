using UnityEngine;
namespace MuseumOfSound
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class MuseumSoundSurface : MonoBehaviour
    {
        [SerializeField] private MuseumStriker.StrikerKind acceptedKind;
        [SerializeField] private AudioClip[] notes;
        [SerializeField] private MuseumInstrument requiredHeldInstrument;
        [SerializeField] private Renderer feedbackRenderer;
        [SerializeField] private Transform movingKey;
        [SerializeField] private float minimumSpeed=.12f;
        [SerializeField] private float cooldown=.12f;
        private AudioSource source;
        private float nextAllowed,flashUntil;
        private Vector3 keyRest;
        private Color baseColor;
        private MaterialPropertyBlock block;
        public int PlaybackCount {get; private set;}
        public void Configure(MuseumStriker.StrikerKind kind,AudioClip[] clips,Renderer visual,MuseumInstrument held=null,Transform key=null,float speed=.12f)
        {acceptedKind=kind;notes=clips;feedbackRenderer=visual;requiredHeldInstrument=held;movingKey=key;minimumSpeed=speed;}
        private void Awake()
        {
            source=GetComponent<AudioSource>();block=new MaterialPropertyBlock();
            if(movingKey!=null)keyRest=movingKey.localPosition;
            if(feedbackRenderer!=null)baseColor=feedbackRenderer.sharedMaterial.GetColor("_BaseColor");
        }
        private void OnTriggerEnter(Collider other)
        {
            if(other.TryGetComponent<MuseumStriker>(out var striker))TryStrike(striker.Kind,striker.Speed,striker.IsActive);
        }
        private void OnTriggerStay(Collider other)
        {
            // Bowing sustains only while the held bow moves across the held violin.
            if(acceptedKind==MuseumStriker.StrikerKind.Bow && other.TryGetComponent<MuseumStriker>(out var striker))
                TryStrike(striker.Kind,striker.Speed,striker.IsActive);
        }
        public bool TryStrike(MuseumStriker.StrikerKind kind,float speed,bool active)
        {
            if(!active || kind!=acceptedKind || speed<minimumSpeed || Time.time<nextAllowed)return false;
            if(requiredHeldInstrument!=null && !requiredHeldInstrument.IsHeld)return false;
            if(notes==null || notes.Length==0)return false;
            int index=requiredHeldInstrument==null?0:requiredHeldInstrument.NoteIndex%notes.Length;
            if(acceptedKind==MuseumStriker.StrikerKind.Bow)source.Stop();
            source.PlayOneShot(notes[index],Mathf.Clamp(speed*.35f,.22f,.7f));PlaybackCount++;
            nextAllowed=Time.time+(acceptedKind==MuseumStriker.StrikerKind.Bow?.22f:cooldown);flashUntil=Time.time+.1f;
            return true;
        }
        private void Update()
        {
            bool flashing=Time.time<flashUntil;
            if(feedbackRenderer!=null){block.SetColor("_BaseColor",flashing?new Color(1f,.68f,.2f):baseColor);feedbackRenderer.SetPropertyBlock(block);}
            if(movingKey!=null)movingKey.localPosition=keyRest+(flashing?Vector3.down*.012f:Vector3.zero);
        }
        private void OnDisable()
        {
            if(source!=null)source.Stop();
            if(movingKey!=null)movingKey.localPosition=keyRest;
            if(feedbackRenderer!=null)feedbackRenderer.SetPropertyBlock(null);
        }
    }
}

