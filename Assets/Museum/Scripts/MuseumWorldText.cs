using UnityEngine;
namespace MuseumOfSound
{
    [ExecuteAlways,RequireComponent(typeof(TextMesh),typeof(MeshRenderer))]
    public sealed class MuseumWorldText : MonoBehaviour
    {
        private TextMesh text;
        private MeshRenderer mesh;
        private void OnEnable(){text=GetComponent<TextMesh>();mesh=GetComponent<MeshRenderer>();SyncAtlas();Font.textureRebuilt+=Rebuilt;}
        private void OnDisable(){Font.textureRebuilt-=Rebuilt;}
        private void Rebuilt(Font font){if(text!=null && text.font==font)SyncAtlas();}
        private void LateUpdate(){SyncAtlas();}
        private void SyncAtlas()
        {
            if(text==null || text.font==null || mesh==null || mesh.sharedMaterial==null)return;
            var atlas=text.font.material.mainTexture;
            if(mesh.sharedMaterial.mainTexture!=atlas)mesh.sharedMaterial.mainTexture=atlas;
        }
    }
}
