using UnityEngine;
namespace MuseumOfSound
{
    public sealed class MuseumTeleportStation : MonoBehaviour
    {
        [SerializeField] private Transform landingPoint;
        public Transform LandingPoint => landingPoint;
        public void Configure(Transform point){landingPoint=point;}
    }
}
