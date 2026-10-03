using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using System.IO;
[InitializeOnLoad]
public static class MuseumTestReport
{
    private static TestRunnerApi api;
    static MuseumTestReport(){EditorApplication.delayCall+=Register;}
    private static void Register(){api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RegisterCallbacks(new ReportCallback());}
    [MenuItem("Tools/Museum of Sound/Run Interaction Tests")]
    public static void Run()
    {
        if(api==null)Register();
        api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.PlayMode,testNames=new[]{"MuseumInteractionTests"}}));
    }
    private class ReportCallback : ICallbacks
    {
        public void RunStarted(ITestAdaptor tests){}
        public void TestStarted(ITestAdaptor test){}
        public void TestFinished(ITestResultAdaptor result){}
        public void RunFinished(ITestResultAdaptor result)
        {
            Directory.CreateDirectory("Docs/AI");
            TestRunnerApi.SaveResultToFile(result,"Docs/AI/MuseumTestResults.xml");
            Debug.Log("MUSEUM TEST RESULT: "+result.ResultState+"; passed "+result.PassCount+", failed "+result.FailCount);
        }
    }
    [MenuItem("Tools/Museum of Sound/Copy Latest Test Report")]
    public static void CopyReport()
    {
        string source=Path.Combine(Application.persistentDataPath,"TestResults.xml");
        string destination=Path.GetFullPath("Docs/AI/MuseumTestResults.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(source,destination,true);
        Debug.Log("Museum test report copied to "+destination);
    }
}
