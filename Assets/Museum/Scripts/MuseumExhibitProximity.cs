using UnityEngine;
namespace MuseumOfSound
{
    public sealed class MuseumExhibitProximity : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField] private GameObject instructions;
        [SerializeField] private float radius=3.2f;
        public void Configure(Transform cameraHead,GameObject panel){head=cameraHead;instructions=panel;}
        private void Update()
        {
            if(head==null || instructions==null)return;
            bool near=(head.position-transform.position).sqrMagnitude<radius*radius;
            if(instructions.activeSelf!=near)instructions.SetActive(near);
        }
    }
}
