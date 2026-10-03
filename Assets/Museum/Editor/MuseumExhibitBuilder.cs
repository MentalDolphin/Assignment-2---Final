using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using MuseumOfSound;
using Object = UnityEngine.Object;

public static class MuseumExhibitBuilder
{
    const string OvrPackage="Packages/com.meta.xr.sdk.interaction.ovr";
    const string IsdkPackage="Packages/com.meta.xr.sdk.interaction";
    static Material ebony,ivory,oak,red,silver,gold,stringMat;
    static GrabInteractor leftGrab,rightGrab;
    static OVRCameraRig rig;
    static Transform exhibits;
    [MenuItem("Tools/Museum of Sound/Add Interactive Instruments")]
    public static void Build()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!="Assets/Scenes/MuseumOfSound.unity")throw new InvalidOperationException("Open MuseumOfSound in Edit mode.");
        if(GameObject.Find("Museum of Sound - Instruments")!=null)throw new InvalidOperationException("Instrument collection already exists; no objects were overwritten.");
        rig=Object.FindFirstObjectByType<OVRCameraRig>();
        if(rig==null)throw new InvalidOperationException("Existing OVRCameraRig required.");
        Directory.CreateDirectory("Assets/Museum/Materials");AssetDatabase.Refresh();
        ebony=Mat("Instrument Ebony",new Color(.025f,.03f,.035f),.15f,.4f);
        ivory=Mat("Instrument Ivory",new Color(.92f,.88f,.75f),0,.3f);
        oak=Mat("Instrument Wood",new Color(.55f,.27f,.08f),0,.4f);
        red=Mat("Drum Burgundy",new Color(.42f,.035f,.055f),.2f,.3f);
        silver=Mat("Instrument Silver",new Color(.65f,.70f,.73f),.7f,.6f);
        gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/Museum/Materials/Brass.mat");
        stringMat=Mat("Strings",new Color(.9f,.77f,.48f),.3f,.2f);
        SetupControllers();
        exhibits=new GameObject("Museum of Sound - Instruments").transform;
        Piano(Bay("01 PIANO"));Drums(Bay("02 DRUMS"));Guitar(Bay("03 GUITAR"));Violin(Bay("04 VIOLIN"));Flute(Bay("05 FLUTE"));
        Undo.RegisterCreatedObjectUndo(exhibits.gameObject,"Add interactive museum instruments");
        var character=rig.GetComponent<CharacterController>();
        if(character==null)character=Undo.AddComponent<CharacterController>(rig.gameObject);
        character.radius=.22f;character.height=1.7f;character.center=new Vector3(0,.87f,0);character.stepOffset=.1f;character.skinWidth=.02f;
        var locomotion=rig.GetComponent<MuseumLocomotion>();if(locomotion==null)locomotion=Undo.AddComponent<MuseumLocomotion>(rig.gameObject);
        locomotion.Configure(rig.centerEyeAnchor);
        foreach(var item in exhibits.GetComponentsInChildren<MuseumInstrument>())
            foreach(var collider in item.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(character,collider);
        // Ignore collisions are re-established at runtime by the instrument setup below.
        var collisionSetup=rig.gameObject.AddComponent<MuseumPlayerCollisionSetup>();collisionSetup.Configure(character,exhibits);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Selection.activeGameObject=exhibits.gameObject;
        Debug.Log("Museum: five modeled exhibits, six grabbable objects, piano touch keys, strike/strum/bow surfaces, flute trigger notes, and controller locomotion installed.");
    }
    [MenuItem("Tools/Museum of Sound/Refine Instrument Colliders")]
    public static void RefineColliders()
    {
        var root=GameObject.Find("Museum of Sound - Instruments");
        if(root==null || EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Open the museum in Edit mode.");
        foreach(var item in root.GetComponentsInChildren<MuseumInstrument>()) {
            Undo.RecordObject(item.GetComponent<Rigidbody>(),"Refine instrument physics");
            item.GetComponent<Rigidbody>().isKinematic=true;
            item.GetComponent<Rigidbody>().interpolation=RigidbodyInterpolation.None;
            item.GetComponent<Rigidbody>().collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            item.GetComponent<Grabbable>().ForceKinematicDisabled=true;
            foreach(var sphere in item.GetComponentsInChildren<SphereCollider>()) {
                if(sphere.isTrigger)continue;
                var go=sphere.gameObject;Undo.DestroyObjectImmediate(sphere);Undo.AddComponent<BoxCollider>(go);
            }
        }
        foreach(var surface in root.GetComponentsInChildren<MuseumSoundSurface>()) {
            var sphere=surface.GetComponent<SphereCollider>();
            if(sphere!=null){Undo.DestroyObjectImmediate(sphere);var box=Undo.AddComponent<BoxCollider>(surface.gameObject);box.isTrigger=true;box.size=new Vector3(.95f,2f,.95f);}
        }
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
    static Transform Bay(string prefix) {
        var holder=GameObject.Find("Museum of Sound - Building/03 Future Exhibit Stations/"+prefix+" - Exhibit Bay");
        if(holder==null)throw new InvalidOperationException("Missing bay: "+prefix);
        var t=new GameObject(prefix+" - Interactive Exhibit").transform;t.SetParent(exhibits,false);
        t.SetPositionAndRotation(holder.transform.position,holder.transform.rotation);
        return t;
    }
    static void SetupControllers()
    {
        if(Object.FindObjectsByType<GrabInteractor>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length>0)
            throw new InvalidOperationException("Existing grab interactors detected; inspect them before running this installer.");
        var interaction=Instantiate(OvrPackage+"/Runtime/Prefabs/OVRInteraction.prefab",rig.transform);
        interaction.name="Museum Controller Interaction";
        var rigRef=interaction.GetComponent<OVRCameraRigRef>();rigRef.InjectAllOVRCameraRigRef(rig,false);
        var world=interaction.GetComponent<TrackingToWorldTransformerOVR>();
        var controllers=Instantiate(OvrPackage+"/Runtime/Prefabs/OVRControllers.prefab",interaction.transform);
        foreach(var source in controllers.GetComponentsInChildren<FromOVRControllerDataSource>(true)){
            source.InjectCameraRigRef(rigRef);source.InjectTrackingToWorldTransformer(world);
        }
        foreach(var controller in controllers.GetComponentsInChildren<Controller>(true)) {
            var so=new SerializedObject(controller);
            // Controller handedness is available through its serialized data source before Start.
            var handed=so.FindProperty("_handedness");
            bool left=handed!=null?handed.enumValueIndex==0:controller.name.ToLowerInvariant().Contains("left");
            // Parent names in the shipped OVRControllers prefab identify LeftController and RightController.
            for(var p=controller.transform;p!=controllers.transform && p!=null;p=p.parent)
                if(p.name.ToLowerInvariant().Contains("left"))left=true;else if(p.name.ToLowerInvariant().Contains("right"))left=false;
            var template=Instantiate(IsdkPackage+"/Editor/QuickActions/Templates/Template_ControllerGrabInteractor.prefab",controller.transform);
            template.name=left?"Left Controller Grab":"Right Controller Grab";
            foreach(var reference in template.GetComponentsInChildren<ControllerRef>(true))reference.InjectController(controller);
            var grab=template.GetComponent<GrabInteractor>();
            if(left)leftGrab=grab;else rightGrab=grab;
        }
        if(leftGrab==null || rightGrab==null)throw new InvalidOperationException("Both controller grab interactors are required.");
        Probe(rig.leftControllerAnchor,OVRInput.Controller.LTouch);
        Probe(rig.rightControllerAnchor,OVRInput.Controller.RTouch);
        Undo.RegisterCreatedObjectUndo(interaction,"Add Meta controller interactions");
    }
    static GameObject Instantiate(string path,Transform parent){var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(asset==null)throw new FileNotFoundException(path);return (GameObject)PrefabUtility.InstantiatePrefab(asset,parent);}
    static void Probe(Transform parent,OVRInput.Controller controller) {
        var go=Part("Museum Touch Tip",parent,PrimitiveType.Sphere,new Vector3(0,0,.10f),Vector3.one*.065f,silver,false);
        var col=go.AddComponent<SphereCollider>();col.radius=.5f;col.isTrigger=true;
        var body=go.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
        go.AddComponent<MuseumStriker>().Configure(MuseumStriker.StrikerKind.Controller,null,controller);
    }
    static Material Mat(string name,Color color,float metallic,float smooth) {
        var path="Assets/Museum/Materials/"+name+".mat";
        var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing!=null)return existing;
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);AssetDatabase.CreateAsset(m,path);return m;
    }
    static Transform Root(string name,Transform parent,Vector3 position) {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
    static GameObject Part(string name,Transform parent,PrimitiveType type,Vector3 p,Vector3 size,Material mat,bool solid=true,Vector3 rotation=default) {
        var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;go.transform.localEulerAngles=rotation;
        go.GetComponent<Renderer>().sharedMaterial=mat;
        if(!solid)Object.DestroyImmediate(go.GetComponent<Collider>());
        else if(type==PrimitiveType.Sphere){Object.DestroyImmediate(go.GetComponent<SphereCollider>());go.AddComponent<BoxCollider>();}
        return go;
    }
    static AudioClip[] Clips(string prefix,int count){return Enumerable.Range(0,count).Select(i=>Clip(prefix+i)).ToArray();}
    static AudioClip Clip(string name){var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Museum/Audio/"+name+".wav");if(clip==null)throw new FileNotFoundException(name);return clip;}
    static AudioSource Audio(GameObject go){var a=go.AddComponent<AudioSource>();a.playOnAwake=false;a.spatialBlend=1;a.rolloffMode=AudioRolloffMode.Linear;a.minDistance=.5f;a.maxDistance=8;a.volume=.8f;a.dopplerLevel=0;return a;}
    static MuseumInstrument Grab(Transform t,MuseumInstrument.PlayStyle style,AudioClip[] notes,float mass=.5f) {
        var rb=t.gameObject.AddComponent<Rigidbody>();rb.mass=mass;rb.interpolation=RigidbodyInterpolation.None;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;rb.isKinematic=true;
        var g=t.gameObject.AddComponent<Grabbable>();g.MaxGrabPoints=1;g.ForceKinematicDisabled=true;g.InjectOptionalRigidbody(rb);g.InjectOptionalTargetTransform(t);g.InjectOptionalThrowWhenUnselected(true);
        var interactable=t.gameObject.AddComponent<GrabInteractable>();interactable.InjectAllGrabInteractable(rb);interactable.InjectOptionalPointableElement(g);interactable.UseClosestPointAsGrabSource=true;
        Audio(t.gameObject);var instrument=t.gameObject.AddComponent<MuseumInstrument>();instrument.Configure(style,notes,leftGrab,rightGrab);return instrument;
    }
    static void Surface(GameObject go,MuseumStriker.StrikerKind kind,AudioClip[] notes,MuseumInstrument held=null,Transform key=null,float speed=.12f) {
        var trigger=go.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=Vector3.one*1.06f;
        Audio(go);go.AddComponent<MuseumSoundSurface>().Configure(kind,notes,go.GetComponent<Renderer>(),held,key,speed);
    }
    static void Tip(Transform tool,MuseumInstrument owner,MuseumStriker.StrikerKind kind,Vector3 pos,Vector3 size) {
        var t=Root("Playing Contact",tool,pos);var col=t.gameObject.AddComponent<BoxCollider>();col.size=size;col.isTrigger=true;t.gameObject.AddComponent<MuseumStriker>().Configure(kind,owner);
    }
    static void Instructions(Transform bay,string text) {
        var go=new GameObject("Nearby Interaction Instructions");go.transform.SetParent(bay,false);go.transform.localPosition=new Vector3(0,2.06f,.49f);
        var tm=go.AddComponent<TextMesh>();tm.text=text;tm.fontSize=64;tm.characterSize=.012f;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=new Color(.95f,.92f,.8f);tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");go.GetComponent<MeshRenderer>().sharedMaterial=tm.font.material;
        bay.gameObject.AddComponent<MuseumExhibitProximity>().Configure(rig.centerEyeAnchor,go);
    }
    static void Piano(Transform bay) {
        var t=Root("Playable Upright Piano",bay,new Vector3(0,.95f,-.45f));
        Part("Piano Case",t,PrimitiveType.Cube,new Vector3(0,.30f,.22f),new Vector3(1.75f,.60f,.27f),ebony);
        Part("Keyboard Bed",t,PrimitiveType.Cube,new Vector3(0,.16f,-.10f),new Vector3(1.85f,.15f,.54f),ebony);
        for(int i=0;i<8;i++) {
            var k=Part("Key "+new[]{"C","D","E","F","G","A","B","C5"}[i],t,PrimitiveType.Cube,new Vector3(-.70f+i*.20f,.25f,-.18f),new Vector3(.185f,.055f,.42f),ivory,false);
            Surface(k,MuseumStriker.StrikerKind.Controller,new[]{Clip("Piano_"+i)},null,k.transform,0);
        }
        foreach(int i in new[]{0,1,3,4,5})Part("Black Key - Decorative",t,PrimitiveType.Cube,new Vector3(-.60f+i*.20f,.30f,-.04f),new Vector3(.095f,.06f,.22f),ebony,false);
        Part("Music Rest",t,PrimitiveType.Cube,new Vector3(0,.49f,.03f),new Vector3(.85f,.23f,.025f),oak,false,new Vector3(-12,0,0));
        Instructions(bay,"TOUCH THE WHITE KEYS\nUse either controller tip to play C to C.");
    }
    static void Drums(Transform bay) {
        var t=Root("Playable Drum Kit",bay,new Vector3(0,.95f,-.45f));
        Drum(t,"Snare",new Vector3(-.5f,.23f,0),.43f,.20f,"snare");Drum(t,"Tom",new Vector3(.38f,.24f,.08f),.40f,.28f,"tom");
        Drum(t,"Bass",new Vector3(0,.12f,.36f),.46f,.24f,"kick");
        var cymbal=Part("Cymbal",t,PrimitiveType.Cylinder,new Vector3(.78f,.44f,.1f),new Vector3(.43f,.014f,.43f),gold,false);Surface(cymbal,MuseumStriker.StrikerKind.Drumstick,new[]{Clip("Drum_cymbal")});
        Part("Cymbal Stand",t,PrimitiveType.Cylinder,new Vector3(.78f,.22f,.1f),new Vector3(.022f,.22f,.022f),silver);
        for(int i=0;i<2;i++) {
            var stick=Root(i==0?"Left Drumstick":"Right Drumstick",bay,new Vector3(-.3f+i*.60f,1.005f,-.90f));
            Part("Stick",stick,PrimitiveType.Cylinder,Vector3.zero,new Vector3(.022f,.22f,.022f),oak,true,new Vector3(90,0,0));
            Part("Tip",stick,PrimitiveType.Sphere,new Vector3(0,0,.22f),Vector3.one*.035f,oak,false);
            var owner=Grab(stick,MuseumInstrument.PlayStyle.Silent,Array.Empty<AudioClip>(),.08f);
            Tip(stick,owner,MuseumStriker.StrikerKind.Drumstick,new Vector3(0,0,.22f),new Vector3(.055f,.055f,.065f));
        }
        Instructions(bay,"GRIP TO PICK UP THE DRUMSTICKS\nStrike the drum heads or brass cymbal.");
    }
    static void Drum(Transform parent,string name,Vector3 p,float diameter,float height,string sound) {
        Part(name+" Shell",parent,PrimitiveType.Cylinder,p,new Vector3(diameter,height/2,diameter),red);
        var head=Part(name+" Head",parent,PrimitiveType.Cylinder,p+Vector3.up*(height/2+.012f),new Vector3(diameter+.025f,.012f,diameter+.025f),ivory,false);
        // Thin hit zone above the drum skin; avoid non-uniform SphereCollider scaling.
        Audio(head);var trigger=head.AddComponent<BoxCollider>();trigger.size=new Vector3(.95f,2f,.95f);trigger.isTrigger=true;
        head.AddComponent<MuseumSoundSurface>().Configure(MuseumStriker.StrikerKind.Drumstick,new[]{Clip("Drum_"+sound)},head.GetComponent<Renderer>());
    }
    static void Guitar(Transform bay) {
        var t=Root("Grabbable Acoustic Guitar",bay,new Vector3(0,.99f,-.45f));
        Part("Lower Bout",t,PrimitiveType.Sphere,new Vector3(0,.25f,0),new Vector3(.42f,.48f,.15f),oak);
        Part("Upper Bout",t,PrimitiveType.Sphere,new Vector3(0,.48f,0),new Vector3(.32f,.29f,.14f),oak);
        Part("Neck",t,PrimitiveType.Cube,new Vector3(0,.81f,0),new Vector3(.075f,.52f,.075f),ebony);
        Part("Headstock",t,PrimitiveType.Cube,new Vector3(0,1.12f,0),new Vector3(.12f,.17f,.065f),oak);
        Part("Sound Hole",t,PrimitiveType.Cylinder,new Vector3(0,.37f,-.077f),new Vector3(.115f,.008f,.115f),ebony,false,new Vector3(90,0,0));
        Part("Bridge",t,PrimitiveType.Cube,new Vector3(0,.20f,-.085f),new Vector3(.18f,.035f,.022f),ebony,false);
        for(int i=0;i<6;i++)Part("String "+i,t,PrimitiveType.Cylinder,new Vector3(-.0225f+i*.009f,.64f,-.092f),new Vector3(.0025f,.44f,.0025f),stringMat,false);
        for(int i=0;i<6;i++)Part("Tuning Peg",t,PrimitiveType.Sphere,new Vector3((i%2==0?-.08f:.08f),1.06f+(i/2)*.05f,0),Vector3.one*.035f,silver,false);
        var owner=Grab(t,MuseumInstrument.PlayStyle.Guitar,Clips("Guitar_",4),.8f);
        var strum=Part("Strum Area",t,PrimitiveType.Cube,new Vector3(0,.34f,-.105f),new Vector3(.20f,.26f,.025f),stringMat,false);strum.GetComponent<Renderer>().enabled=false;
        Surface(strum,MuseumStriker.StrikerKind.Controller,Clips("Guitar_",4),owner,null,.1f);
        Instructions(bay,"GRIP TO HOLD / SWEEP OTHER CONTROLLER OVER STRINGS\nTrigger also strums. A/B or X/Y changes chord.");
    }
    static void Violin(Transform bay) {
        var t=Root("Grabbable Violin",bay,new Vector3(-.24f,.99f,-.40f));
        Part("Lower Bout",t,PrimitiveType.Sphere,new Vector3(0,.18f,0),new Vector3(.25f,.33f,.09f),oak);
        Part("Upper Bout",t,PrimitiveType.Sphere,new Vector3(0,.36f,0),new Vector3(.21f,.23f,.085f),oak);
        Part("Neck",t,PrimitiveType.Cube,new Vector3(0,.55f,0),new Vector3(.045f,.29f,.045f),ebony);
        Part("Scroll",t,PrimitiveType.Sphere,new Vector3(0,.74f,0),new Vector3(.07f,.10f,.05f),oak);
        Part("Bridge",t,PrimitiveType.Cube,new Vector3(0,.27f,-.065f),new Vector3(.08f,.04f,.018f),ivory,false);
        for(int i=0;i<4;i++)Part("String "+i,t,PrimitiveType.Cylinder,new Vector3(-.012f+i*.008f,.43f,-.060f),new Vector3(.002f,.25f,.002f),stringMat,false);
        var owner=Grab(t,MuseumInstrument.PlayStyle.Silent,Clips("Violin_",4),.3f);
        var surface=Part("Bowing Area",t,PrimitiveType.Cube,new Vector3(0,.28f,-.085f),new Vector3(.18f,.14f,.035f),stringMat,false);surface.GetComponent<Renderer>().enabled=false;
        Surface(surface,MuseumStriker.StrikerKind.Bow,Clips("Violin_",4),owner,null,.08f);
        var bow=Root("Grabbable Violin Bow",bay,new Vector3(.42f,1.015f,-.45f));
        Part("Bow Stick",bow,PrimitiveType.Cylinder,Vector3.zero,new Vector3(.016f,.32f,.016f),oak,true,new Vector3(0,0,90));
        Part("Bow Hair",bow,PrimitiveType.Cube,new Vector3(0,-.025f,0),new Vector3(.58f,.006f,.018f),ivory,false);
        Part("Bow Frog",bow,PrimitiveType.Cube,new Vector3(-.28f,-.02f,0),new Vector3(.065f,.05f,.035f),ebony);
        var bowOwner=Grab(bow,MuseumInstrument.PlayStyle.Silent,Array.Empty<AudioClip>(),.08f);
        Tip(bow,bowOwner,MuseumStriker.StrikerKind.Bow,new Vector3(0,-.025f,0),new Vector3(.60f,.05f,.05f));
        Instructions(bay,"GRIP THE VIOLIN AND BOW WITH SEPARATE HANDS\nMove bow across strings. A/B or X/Y changes note.");
    }
    static void Flute(Transform bay) {
        var t=Root("Grabbable Flute",bay,new Vector3(0,1.04f,-.45f));
        Part("Silver Tube",t,PrimitiveType.Cylinder,Vector3.zero,new Vector3(.034f,.34f,.034f),silver,true,new Vector3(0,0,90));
        for(int i=0;i<7;i++) {
            Part("Finger Key "+i,t,PrimitiveType.Cylinder,new Vector3(-.19f+i*.065f,.024f,0),new Vector3(.041f,.006f,.041f),silver,false);
            Part("Key Centre",t,PrimitiveType.Cylinder,new Vector3(-.19f+i*.065f,.031f,0),new Vector3(.021f,.002f,.021f),ebony,false);
        }
        Part("Embouchure",t,PrimitiveType.Sphere,new Vector3(.27f,.02f,0),new Vector3(.065f,.015f,.045f),silver,false);
        Part("Blow Hole",t,PrimitiveType.Sphere,new Vector3(.27f,.03f,0),new Vector3(.023f,.006f,.013f),ebony,false);
        Grab(t,MuseumInstrument.PlayStyle.Flute,Clips("Flute_",4),.2f);
        Instructions(bay,"GRIP TO HOLD / HOLD TRIGGER TO PLAY\nA/B or X/Y changes note. Release trigger to stop.");
    }
}


