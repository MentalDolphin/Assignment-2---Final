using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor-only construction tool. Nothing executes in the VR player.
public static class MuseumShellBuilder
{
    const string ScenePath = "Assets/Scenes/MuseumOfSound.unity";
    const string MaterialFolder = "Assets/Museum/Materials";
    static Transform root;
    static Material wall, floor, dark, wood, brass, accent, glow;

    [MenuItem("Tools/Museum of Sound/Build Basic Shell")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Build the shell in Edit mode.");
        var scene = SceneManager.GetActiveScene();
        if (GameObject.Find("Museum of Sound - Building") != null || File.Exists(ScenePath))
            throw new System.InvalidOperationException("Museum shell already exists. No existing objects or scenes were overwritten.");
        if (Shader.Find("Universal Render Pipeline/Lit") == null)
            throw new System.InvalidOperationException("URP Lit shader is required.");
        Directory.CreateDirectory(MaterialFolder);
        AssetDatabase.Refresh();
        wall = Mat("Warm Plaster", new Color(.78f,.73f,.63f), 0f, .12f);
        floor = Mat("Oak Floor", new Color(.30f,.19f,.11f), 0f, .28f);
        dark = Mat("Charcoal", new Color(.055f,.071f,.075f), 0f, .15f);
        wood = Mat("Oak Joinery", new Color(.43f,.27f,.13f), 0f, .25f);
        brass = Mat("Brass", new Color(.72f,.48f,.18f), .65f, .45f);
        accent = Mat("Deep Teal", new Color(.035f,.24f,.25f), 0f, .15f);
        glow = Mat("Light Diffuser", new Color(1f,.90f,.70f), 0f, .2f);
        glow.EnableKeyword("_EMISSION");
        glow.SetColor("_EmissionColor", new Color(1f,.84f,.57f)*2f);
        root = new GameObject("Museum of Sound - Building").transform;
        var structure = Group("01 Architecture");
        Box("Floor - Walkable", structure, new Vector3(0,-.15f,0), new Vector3(18,.3f,22), floor);
        Box("Ceiling", structure, new Vector3(0,4.65f,0), new Vector3(18,.3f,22), wall);
        Box("West Wall", structure, new Vector3(-9.15f,2.25f,0), new Vector3(.3f,4.5f,22.6f), wall);
        Box("East Wall", structure, new Vector3(9.15f,2.25f,0), new Vector3(.3f,4.5f,22.6f), wall);
        Box("North Wall", structure, new Vector3(0,2.25f,11.15f), new Vector3(18,4.5f,.3f), wall);
        Box("South Wall", structure, new Vector3(0,2.25f,-11.15f), new Vector3(18,4.5f,.3f), wall);
        var details = Group("02 Interior Details");
        for(int i=0;i<=18;i++)
            Box("Floor Seam " + i,details,new Vector3(-8.95f+i*.994f,.002f,0),new Vector3(.012f,.003f,21.8f),wood,false);
        foreach(float x in new[]{-8.96f,8.96f}) {
            Box("Side Skirting",details,new Vector3(x,.12f,0),new Vector3(.08f,.24f,22),wood,false);
            Box("Side Cornice",details,new Vector3(x,4.25f,0),new Vector3(.12f,.18f,22),dark,false);
        }
        foreach(float z in new[]{-10.96f,10.96f}) {
            Box("End Skirting",details,new Vector3(0,.12f,z),new Vector3(18,.24f,.08f),wood,false);
            Box("End Cornice",details,new Vector3(0,4.25f,z),new Vector3(18,.18f,.12f),dark,false);
        }
        foreach(float z in new[]{-7f,0f,7f})
            Box("Ceiling Acoustic Beam",details,new Vector3(0,4.36f,z),new Vector3(18,.22f,.26f),wood,false);
        Box("Arrival Door - Decorative",details,new Vector3(0,1.4f,-10.95f),new Vector3(2.4f,2.8f,.08f),dark,false);
        Box("Door Centre Seam",details,new Vector3(0,1.4f,-10.88f),new Vector3(.025f,2.7f,.025f),brass,false);
        foreach(float x in new[]{-.18f,.18f})
            Box("Door Handle",details,new Vector3(x,1.15f,-10.79f),new Vector3(.035f,.36f,.055f),brass,false);
        var stations = Group("03 Future Exhibit Stations");
        Station(stations,"01 PIANO",new Vector3(0,0,8.5f),0);
        Station(stations,"02 DRUMS",new Vector3(-7.2f,0,3.6f),-90);
        Station(stations,"03 GUITAR",new Vector3(7.2f,0,3.6f),90);
        Station(stations,"04 VIOLIN",new Vector3(-7.2f,0,-4.2f),-90);
        Station(stations,"05 FLUTE",new Vector3(7.2f,0,-4.2f),90);
        var signs = Group("04 Signage");
        Text("Museum Title",signs,"MUSEUM OF SOUND",new Vector3(0,3.35f,10.93f),.38f,brass);
        Text("Museum Subtitle",signs,"FIVE INSTRUMENTS / A WORLD OF SOUND",new Vector3(0,2.8f,10.91f),.10f,dark);
        Text("Arrival Label",signs,"MUSEUM OF SOUND",new Vector3(0,3.15f,-10.83f),.19f,brass,180);
        var lights = Group("05 Lighting");
        foreach(float x in new[]{-4.5f,4.5f}) foreach(float z in new[]{-6f,6f}) {
            Box("Ceiling Light Diffuser",lights,new Vector3(x,4.40f,z),new Vector3(2.2f,.05f,.7f),glow,false);
            var lamp=new GameObject("Warm Gallery Light");lamp.transform.SetParent(lights,false);lamp.transform.localPosition=new Vector3(x,3.8f,z);
            var l=lamp.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1f,.88f,.70f);l.intensity=4f;l.range=13f;l.shadows=LightShadows.None;
        }
        Undo.RegisterCreatedObjectUndo(root.gameObject,"Create Museum of Sound shell");
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene,ScenePath)) throw new System.IO.IOException("Could not save museum scene.");
        AssetDatabase.SaveAssets();
        Selection.activeGameObject=root.gameObject;
        if(SceneView.lastActiveSceneView!=null)
            SceneView.lastActiveSceneView.LookAt(new Vector3(0,1.8f,4f),Quaternion.Euler(12,0,0),13f);
        Debug.Log("Museum shell saved to " + ScenePath + ". Five future exhibit stations; interactions and locomotion remain to be implemented.");
    }
    [MenuItem("Tools/Museum of Sound/Refine Shell Labels")]
    public static void Refine()
    {
        var building=GameObject.Find("Museum of Sound - Building");
        if(building==null || SceneManager.GetActiveScene().path!=ScenePath)
            throw new System.InvalidOperationException("Open the museum scene first.");
        foreach(var text in building.GetComponentsInChildren<TextMesh>()) {
            Undo.RecordObject(text,"Refine museum labels");
            float size=text.name=="Museum Title"?.38f:text.name=="Museum Subtitle"?.10f:text.name=="Arrival Label"?.19f:text.name=="Station Label"?.17f:.055f;
            text.characterSize=size/5f;
        }
        foreach(var t in building.GetComponentsInChildren<Transform>()) {
            if(t.name.EndsWith(" - Exhibit Bay") && t.localPosition.x!=0) {
                Undo.RecordObject(t,"Orient exhibit toward gallery");
                t.localRotation=Quaternion.Euler(0,t.localPosition.x<0?-90:90,0);
            }
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Museum labels and bay orientation refined.");
    }
    static Material Mat(string name,Color color,float metal,float smooth) {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);
        AssetDatabase.CreateAsset(m,MaterialFolder+"/"+name+".mat");return m;
    }
    static Transform Group(string name) {var t=new GameObject(name).transform;t.SetParent(root,false);return t;}
    static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,Material mat,bool solid=true) {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;
        go.GetComponent<Renderer>().sharedMaterial=mat;
        if(!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.isStatic=true;return go;
    }
    static void Station(Transform parent,string name,Vector3 position,float yaw) {
        var t=new GameObject(name+" - Exhibit Bay").transform;t.SetParent(parent,false);t.localPosition=position;t.localRotation=Quaternion.Euler(0,yaw,0);
        Box("Backdrop",t,new Vector3(0,1.6f,.6f),new Vector3(3.8f,3.2f,.16f),accent);
        Box("Display Plinth",t,new Vector3(0,.45f,-.45f),new Vector3(2.4f,.9f,1.2f),dark);
        Box("Plinth Top",t,new Vector3(0,.925f,-.45f),new Vector3(2.5f,.05f,1.3f),wood);
        Text("Station Label",t,name,new Vector3(0,2.8f,.50f),.17f,brass);
        Text("Placeholder Label",t,"INSTRUMENT HISTORY + INTERACTION",new Vector3(0,2.46f,.50f),.055f,wall);
        var anchor=new GameObject("Instrument Placement Anchor").transform;anchor.SetParent(t,false);anchor.localPosition=new Vector3(0,.95f,-.45f);
        var panel=new GameObject("Future Information Panel Anchor").transform;panel.SetParent(t,false);panel.localPosition=new Vector3(0,1.8f,.48f);
        Box("Approach Marker",t,new Vector3(0,.006f,-2.15f),new Vector3(2.4f,.012f,.06f),brass,false);
    }
    static void Text(string name,Transform parent,string value,Vector3 position,float size,Material colorSource,float yaw=0) {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
        var text=go.AddComponent<TextMesh>();text.text=value;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=80;text.characterSize=size / 5f;
        text.color=colorSource.GetColor("_BaseColor");text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");go.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
    }
}


