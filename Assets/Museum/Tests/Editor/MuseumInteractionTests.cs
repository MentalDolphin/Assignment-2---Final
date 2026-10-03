#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Oculus.Interaction;
using MuseumOfSound;

public class MuseumInteractionTests
{
    [UnityTest]
    public IEnumerator ExhibitAudioGrabPhysicsAndReset()
    {
        // Known platform/service errors are captured explicitly; no XR hardware is required for the simulated tests.
        var errors=new List<string>();
        Application.LogCallback capture=(message,stack,type)=> {
            if((type==LogType.Error || type==LogType.Exception || type==LogType.Assert)
                && !message.Contains("NoSubscription") && !message.Contains("ErrorFormFactorUnavailable")
                && !message.Contains("[AgentBridge] Remote")) errors.Add(message);
        };
        Application.logMessageReceived+=capture;
        LogAssert.ignoreFailingMessages=true;
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/MuseumOfSound.unity",new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return new WaitForFixedUpdate();
        var collection=GameObject.Find("Museum of Sound - Instruments");Assert.That(collection,Is.Not.Null);
        var instruments=collection.GetComponentsInChildren<MuseumInstrument>();Assert.That(instruments.Length,Is.EqualTo(6));
        foreach(var item in instruments) {
            Assert.That(item.GetComponent<Rigidbody>().isKinematic,Is.True,"Objects should remain on display until grabbed.");
            Assert.That(item.GetComponent<GrabInteractable>().Rigidbody,Is.EqualTo(item.GetComponent<Rigidbody>()));
            var grab=item.GetComponent<Grabbable>();var pose=new Pose(item.transform.position,item.transform.rotation);int id=item.GetInstanceID();
            grab.ProcessPointerEvent(new PointerEvent(id,PointerEventType.Hover,pose));
            grab.ProcessPointerEvent(new PointerEvent(id,PointerEventType.Select,pose));
            Assert.That(item.IsHeld,Is.True,item.name+" must be selectable.");
            grab.ProcessPointerEvent(new PointerEvent(id,PointerEventType.Move,new Pose(pose.position+Vector3.up*.15f,pose.rotation)));
            Physics.SyncTransforms();
            Assert.That(Vector3.Distance(item.transform.position,pose.position),Is.GreaterThan(.12f),item.name+" must follow its grab point.");
            if(item.name.Contains("Guitar") || item.name.Contains("Flute")){
                int count=item.PlaybackCount;item.ApplyPlayInput(true,false,true,1);
                Assert.That(item.NoteIndex,Is.EqualTo(1));Assert.That(item.PlaybackCount,Is.EqualTo(count+1));Assert.That(item.GetComponent<AudioSource>().isPlaying,Is.True);
                item.ApplyPlayInput(false,true,false,0);Assert.That(item.NoteIndex,Is.EqualTo(0));
                if(item.name.Contains("Flute"))Assert.That(item.GetComponent<AudioSource>().isPlaying,Is.False);
            }
            grab.ProcessPointerEvent(new PointerEvent(id,PointerEventType.Unselect,pose));grab.ProcessPointerEvent(new PointerEvent(id,PointerEventType.Unhover,pose));
            item.ReturnHome();Assert.That(Vector3.Distance(item.transform.position,pose.position),Is.LessThan(.01f));
        }
        var surfaces=collection.GetComponentsInChildren<MuseumSoundSurface>();Assert.That(surfaces.Length,Is.EqualTo(14));
        var key=surfaces.First(x=>x.name=="Key C");
        Assert.That(key.TryStrike(MuseumStriker.StrikerKind.Controller,1,false),Is.False);
        Assert.That(key.TryStrike(MuseumStriker.StrikerKind.Bow,1,true),Is.False);
        Assert.That(key.TryStrike(MuseumStriker.StrikerKind.Controller,.1f,true),Is.True);
        Assert.That(key.GetComponent<AudioSource>().isPlaying,Is.True);
        Assert.That(key.TryStrike(MuseumStriker.StrikerKind.Controller,1,true),Is.False,"Duplicate contact should be debounced.");
        yield return new WaitForSeconds(.15f);
        Assert.That(key.TryStrike(MuseumStriker.StrikerKind.Controller,1,true),Is.True);
        Assert.That(key.PlaybackCount,Is.EqualTo(2));
        foreach(string name in new[]{"Grabbable Acoustic Guitar","Grabbable Violin"}) {
            var owner=instruments.First(x=>x.name==name);var surface=owner.GetComponentInChildren<MuseumSoundSurface>();
            var kind=name.Contains("Guitar")?MuseumStriker.StrikerKind.Controller:MuseumStriker.StrikerKind.Bow;
            Assert.That(surface.TryStrike(kind,1,true),Is.False,"Instrument must be held to play its surface.");
            var g=owner.GetComponent<Grabbable>();int id=name.Contains("Guitar")?9001:9002;
            Pose pose=new Pose(owner.transform.position,owner.transform.rotation);
            g.ProcessPointerEvent(new PointerEvent(id,PointerEventType.Hover,pose));
            g.ProcessPointerEvent(new PointerEvent(id,PointerEventType.Select,pose));
            Assert.That(owner.IsHeld,Is.True);
            g.ProcessPointerEvent(new PointerEvent(id,PointerEventType.Move,new Pose(pose.position+Vector3.up*.1f,pose.rotation)));
            Physics.SyncTransforms();
            Assert.That(Vector3.Distance(owner.transform.position,pose.position),Is.GreaterThan(.08f),"Meta grab transformer should move the instrument.");
            Assert.That(surface.TryStrike(kind,1,true),Is.True);
            Assert.That(surface.GetComponent<AudioSource>().isPlaying,Is.True);
            g.ProcessPointerEvent(new PointerEvent(id,PointerEventType.Unselect,pose));
            g.ProcessPointerEvent(new PointerEvent(id,PointerEventType.Unhover,pose));
            Assert.That(owner.IsHeld,Is.False);owner.ReturnHome();
        }
        var stick=instruments.First(x=>x.name=="Left Drumstick");
        var drum=surfaces.First(x=>x.name=="Snare Head");
        var striker=stick.GetComponentInChildren<MuseumStriker>();var stickGrab=stick.GetComponent<Grabbable>();
        stickGrab.ProcessPointerEvent(new PointerEvent(9003,PointerEventType.Select,new Pose(stick.transform.position,stick.transform.rotation)));
        Assert.That(striker.IsActive,Is.True);
        stick.transform.rotation=Quaternion.Euler(90,0,0);
        Vector3 contact=drum.GetComponent<Collider>().bounds.center;
        for(int i=0;i<20;i++) {
            stick.transform.position=contact+Vector3.up*(.25f-i*.02f)-stick.transform.TransformVector(striker.transform.localPosition);
            Physics.SyncTransforms();yield return new WaitForFixedUpdate();
        }
        Assert.That(drum.PlaybackCount,Is.GreaterThan(0),"Physical drumstick contact should generate a sound.");
        stickGrab.ProcessPointerEvent(new PointerEvent(9003,PointerEventType.Unselect,new Pose(stick.transform.position,stick.transform.rotation)));
        stick.ReturnHome();
        var flute=instruments.First(x=>x.name=="Grabbable Flute");flute.PlayNote();
        Assert.That(flute.PlaybackCount,Is.GreaterThanOrEqualTo(1));Assert.That(flute.GetComponent<AudioSource>().isPlaying,Is.True);
        Vector3 home=flute.transform.position;flute.GetComponent<Rigidbody>().position=Vector3.down;flute.transform.position=Vector3.down;
        Physics.SyncTransforms();yield return new WaitForSeconds(.05f);
        Assert.That(Vector3.Distance(flute.transform.position,home),Is.LessThan(.01f),"Dropped objects should return to display.");
        Assert.That(flute.GetComponent<AudioSource>().isPlaying,Is.False);
        var stations=Object.FindObjectsByType<MuseumTeleportStation>(FindObjectsSortMode.None);Assert.That(stations.Length,Is.EqualTo(5));
        var rig=Object.FindFirstObjectByType<OVRCameraRig>();var teleport=rig.GetComponent<MuseumTeleportation>();Assert.That(teleport,Is.Not.Null);
        Assert.That(teleport.TryTeleport(null),Is.False);
        foreach(var station in stations){
            Assert.That(teleport.TryTeleport(station),Is.True,"Landing should be clear: "+station.name);
            var delta=rig.centerEyeAnchor.position-station.LandingPoint.position;delta.y=0;Assert.That(delta.magnitude,Is.LessThan(.01f));
            var facing=Vector3.ProjectOnPlane(rig.centerEyeAnchor.forward,Vector3.up).normalized;
            Assert.That(Vector3.Angle(facing,station.LandingPoint.forward),Is.LessThan(.1f));
        }
        Assert.That(teleport.TeleportCount,Is.EqualTo(5));
        var blocked=GameObject.CreatePrimitive(PrimitiveType.Cube);blocked.transform.position=stations[0].LandingPoint.position+Vector3.up;Physics.SyncTransforms();
        Assert.That(teleport.TryTeleport(stations[0]),Is.False,"Occupied landing must be rejected.");Object.Destroy(blocked);
        var labels=Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        Assert.That(labels.Count(x=>x.name=="Instrument Description and History"),Is.EqualTo(5));
        foreach(var label in labels){Assert.That(label.GetComponent<MeshRenderer>().sharedMaterial.shader.name,Is.EqualTo("Museum/World Text Stereo"));Assert.That(label.transform.IsChildOf(rig.transform),Is.False,"World text cannot be parented to the XR rig.");}
        Vector3[] fixedPositions=labels.Select(x=>x.transform.position).ToArray();rig.transform.position+=Vector3.right;rig.transform.Rotate(0,30,0);
        for(int i=0;i<labels.Length;i++)Assert.That(labels[i].transform.position,Is.EqualTo(fixedPositions[i]),"Labels must stay fixed when the headset rig moves.");
        Assert.That(errors,Is.Empty,"Unexpected runtime errors: "+string.Join("; ",errors));

        Application.logMessageReceived-=capture;LogAssert.ignoreFailingMessages=false;
    }
    [TearDown]
    public void RestoreLogPolicy(){LogAssert.ignoreFailingMessages=false;}
}

#endif



