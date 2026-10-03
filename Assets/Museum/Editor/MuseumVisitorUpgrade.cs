using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Oculus.Interaction;
using MuseumOfSound;
using Object=UnityEngine.Object;

public static class MuseumVisitorUpgrade
{
    [MenuItem("Tools/Museum of Sound/Add Teleport Stations and History")]
    public static void Install()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!="Assets/Scenes/MuseumOfSound.unity")throw new InvalidOperationException("Open MuseumOfSound in Edit mode.");
        var rig=Object.FindFirstObjectByType<OVRCameraRig>();
        if(rig==null)throw new InvalidOperationException("OVRCameraRig required.");
        Directory.CreateDirectory("Docs/AI");
        if(!File.Exists("Docs/AI/MuseumBeforeVisitorUpgrade.unity"))File.Copy(scene.path,"Docs/AI/MuseumBeforeVisitorUpgrade.unity");
        var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var textMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Museum/Materials/World Text Stereo.mat");
        if(textMaterial==null){textMaterial=new Material(Shader.Find("Museum/World Text Stereo"));textMaterial.enableInstancing=true;textMaterial.mainTexture=font.material.mainTexture;AssetDatabase.CreateAsset(textMaterial,"Assets/Museum/Materials/World Text Stereo.mat");}
        foreach(var tm in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            Undo.RecordObject(tm.GetComponent<MeshRenderer>(),"Fix world text stereo");
            tm.GetComponent<MeshRenderer>().sharedMaterial=textMaterial;
            if(tm.GetComponent<MuseumWorldText>()==null)Undo.AddComponent<MuseumWorldText>(tm.gameObject);
            if(tm.name=="Placeholder Label")tm.text="EXPLORE / LISTEN / PLAY";
        }
        var right=Object.FindObjectsByType<GrabInteractor>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(x=>x.name=="Right Controller Grab");
        var teleport=rig.GetComponent<MuseumTeleportation>();if(teleport==null)teleport=Undo.AddComponent<MuseumTeleportation>(rig.gameObject);
        var rayObject=rig.transform.Find("Teleport Aim Ray");
        if(rayObject==null){rayObject=new GameObject("Teleport Aim Ray").transform;rayObject.SetParent(rig.transform,false);}
        var ray=rayObject.GetComponent<LineRenderer>();if(ray==null)ray=rayObject.gameObject.AddComponent<LineRenderer>();
        var rayMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Museum/Materials/Teleport Ray.mat");
        if(rayMaterial==null){rayMaterial=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));rayMaterial.SetColor("_BaseColor",Color.white);AssetDatabase.CreateAsset(rayMaterial,"Assets/Museum/Materials/Teleport Ray.mat");}
        ray.sharedMaterial=rayMaterial;ray.useWorldSpace=true;ray.positionCount=2;ray.startWidth=.012f;ray.endWidth=.018f;ray.enabled=false;
        teleport.Configure(rig.centerEyeAnchor,rig.rightControllerAnchor,right,ray);
        string[] names={"01 PIANO","02 DRUMS","03 GUITAR","04 VIOLIN","05 FLUTE"};
        string[] descriptions={
            "Hammers strike strings inside\na piano. The soundboard helps\nthe vibrations fill the room.\n\nAround 1700, Bartolomeo\nCristofori developed the piano\nin Italy. Unlike a harpsichord,\nit could respond to a softer\nor stronger touch.\n\nTRY IT\nTouch the eight white keys\nwith either controller tip.",
            "A drum skin vibrates when\nit is struck. A cymbal makes\nsound through vibrating metal.\n\nDrums have long been used for\ncommunication and ceremonies.\nIn the late 1800s, drums and\ncymbals were combined so one\nplayer could play a drum kit.\n\nTRY IT\nGrip both sticks, then strike\nthe heads and brass cymbal.",
            "Plucked strings vibrate.\nThe bridge transfers that\nmotion to the wooden body,\nwhich amplifies the sound.\n\nThe six-string guitar became\nestablished around 1800.\nSpanish maker Antonio de\nTorres helped shape the modern\nclassical guitar in the 1800s.\n\nTRY IT\nGrip the guitar and strum\nwith the other controller.\nTrigger strums; A/B or X/Y\nchanges the chord.",
            "A bow draws across four\nstrings, setting them vibrating.\nThe bridge carries the sound\ninto the wooden body.\n\nThe violin developed in Italy\nin the 1500s. Cremona became\nfamous for makers such as\nAmati and Stradivari.\n\nTRY IT\nHold the violin and bow in\nseparate hands. Sweep the bow\nacross the strings. A/B or X/Y\nchanges the note.",
            "Air blown across the lip\nhole makes the air column\ninside the flute vibrate. Keys\nchange its effective length\nand therefore the pitch.\n\nIn 1847, Theobald Boehm\nintroduced a redesigned flute.\nHis key system became the\nbasis of the modern flute.\n\nTRY IT\nGrip the flute. Hold trigger\nto play; release to stop.\nA/B or X/Y changes the note."
        };
        var dark=AssetDatabase.LoadAssetAtPath<Material>("Assets/Museum/Materials/Charcoal.mat");
        if(dark==null)dark=AssetDatabase.LoadAssetAtPath<Material>("Assets/Museum/Materials/Instrument Ebony.mat");
        var brass=AssetDatabase.LoadAssetAtPath<Material>("Assets/Museum/Materials/Brass.mat");
        for(int i=0;i<names.Length;i++)
        {
            var bay=GameObject.Find("Museum of Sound - Building/03 Future Exhibit Stations/"+names[i]+" - Exhibit Bay").transform;
            if(bay.Find("Teleport Station")!=null)continue;
            var disk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);disk.name="Teleport Station";disk.transform.SetParent(bay,false);
            disk.transform.localPosition=new Vector3(0,.025f,-1.80f);disk.transform.localScale=new Vector3(1.15f,.025f,1.15f);disk.GetComponent<Renderer>().sharedMaterial=brass;
            Object.DestroyImmediate(disk.GetComponent<CapsuleCollider>());var diskCollider=disk.AddComponent<BoxCollider>();diskCollider.size=new Vector3(1,2,1);
            var landing=new GameObject("Landing - Face Exhibit").transform;landing.SetParent(bay,false);landing.localPosition=new Vector3(0,.055f,-1.80f);
            disk.AddComponent<MuseumTeleportStation>().Configure(landing);
            Label("Station Number",bay,(i+1).ToString("00"),new Vector3(0,.078f,-1.8f),.07f,textMaterial,TextAnchor.MiddleCenter).transform.localRotation=Quaternion.Euler(90,0,0);
            var panel=GameObject.CreatePrimitive(PrimitiveType.Cube);panel.name="Instrument History Plaque";panel.transform.SetParent(bay,false);panel.transform.localPosition=new Vector3(1.72f,1.90f,.20f);panel.transform.localScale=new Vector3(1.45f,1.85f,.055f);panel.GetComponent<Renderer>().sharedMaterial=dark;
            Label("History Heading",bay,names[i].Substring(3),new Vector3(1.10f,2.65f,.163f),.032f,textMaterial,TextAnchor.UpperLeft);
            Label("Instrument Description and History",bay,descriptions[i].Split(new[]{"\n\nTRY IT"},StringSplitOptions.None)[0],new Vector3(1.10f,2.40f,.163f),.014f,textMaterial,TextAnchor.UpperLeft);
            Label("History Source",bay,"Source: Yamaha Instrument Guide",new Vector3(1.10f,1.07f,.163f),.009f,textMaterial,TextAnchor.UpperLeft);
            Undo.RegisterCreatedObjectUndo(disk,"Add exhibit teleport station");Undo.RegisterCreatedObjectUndo(panel,"Add history plaque");
        }
        var signs=GameObject.Find("Museum of Sound - Building/04 Signage").transform;
        if(signs.Find("Teleport Instructions")==null)Label("Teleport Instructions",signs,"TELEPORT: push RIGHT stick forward / aim at a gold station / release\nGrip cancels. LEFT stick walks. RIGHT stick sideways turns.",new Vector3(0,2.65f,-10.81f),.02f,textMaterial,TextAnchor.MiddleCenter).transform.localRotation=Quaternion.Euler(0,180,0);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("Museum visitor upgrade: five teleport stations, five instrument history plaques, stereo world text installed.");
    }
    [MenuItem("Tools/Museum of Sound/Refine History Plaques")]
    public static void Refine()
    {
        foreach(var text in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(text.name=="Instrument Description and History"){Undo.RecordObject(text,"Fit history plaque");text.text=text.text.Split(new[]{"\n\nTRY IT"},StringSplitOptions.None)[0];text.characterSize=.014f;}
            if(text.name=="Placeholder Label")text.gameObject.SetActive(false);
            if(text.name=="Nearby Interaction Instructions")
            {
                Undo.RecordObject(text,"Fit interaction instructions");text.characterSize=.010f;text.transform.localPosition=new Vector3(0,2.4f,.49f);
                string exhibit=text.transform.parent.name;
                text.text=exhibit.Contains("PIANO")?"TOUCH THE WHITE KEYS\nUse either controller tip.\nPlay the notes from C to C.":
                    exhibit.Contains("DRUMS")?"GRIP BOTH DRUMSTICKS\nStrike the drum heads\nand brass cymbal.":
                    exhibit.Contains("GUITAR")?"GRIP TO HOLD THE GUITAR\nSweep the other hand over strings.\nTrigger strums; face buttons change chord.":
                    exhibit.Contains("VIOLIN")?"GRIP VIOLIN AND BOW\nHold one tool in each hand.\nBow the strings; face buttons change note.":
                    "GRIP TO HOLD THE FLUTE\nHold trigger to play; release to stop.\nFace buttons change the note.";
            }
        }
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
    static GameObject Label(string name,Transform parent,string content,Vector3 position,float size,Material material,TextAnchor anchor)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;
        var text=go.AddComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=64;text.characterSize=size;text.anchor=anchor;text.alignment=anchor==TextAnchor.UpperLeft?TextAlignment.Left:TextAlignment.Center;text.text=content;text.color=new Color(.98f,.94f,.80f);text.lineSpacing=1.12f;
        go.GetComponent<MeshRenderer>().sharedMaterial=material;go.AddComponent<MuseumWorldText>();return go;
    }
}
