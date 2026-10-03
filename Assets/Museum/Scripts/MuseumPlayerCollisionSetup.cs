using UnityEngine;
namespace MuseumOfSound
{
    public sealed class MuseumPlayerCollisionSetup : MonoBehaviour
    {
        [SerializeField] private CharacterController player;
        [SerializeField] private Transform instruments;
        public void Configure(CharacterController controller,Transform collection){player=controller;instruments=collection;}
        private void Start()
        {
            if(player==null || instruments==null)return;
            foreach(var instrument in instruments.GetComponentsInChildren<MuseumInstrument>())
                foreach(var collider in instrument.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(player,collider);
        }
    }
}
