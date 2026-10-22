using System;
using System.Reflection;
using NUnit.Framework;
using SailingOut.Sailing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VoyageQASafetyTests
{
    const string Key = "SailingOut.FreeSailingQA";
    const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;
    static Type Driver => Type.GetType("SailingOut.Editor.VoyageQA, Assembly-CSharp-Editor", true);
    static FieldInfo Field(string name) => Driver.GetField(name, PrivateStatic);
    static void Changed(PlayModeStateChange state) => Driver.GetMethod("Changed", PrivateStatic).Invoke(null, new object[] { state });

    [Test]
    public void CancelledSceneSaveDoesNotSwitchScenesOrStartQA()
    {
        Scene scene = SceneManager.GetActiveScene();
        int sceneCount = SceneManager.sceneCount, rootCount = scene.rootCount;
        bool wasDirty = scene.isDirty;
        var original = Field("confirmSceneSave").GetValue(null);
        bool oldFlag = SessionState.GetBool(Key, false), asked = false;
        try
        {
            SessionState.SetBool(Key, false);
            Field("confirmSceneSave").SetValue(null, (Func<bool>)(() => { asked = true; return false; }));
            Driver.GetMethod("Start", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
            Assert.That(asked, Is.True);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(scene));
            Assert.That(scene.isDirty, Is.EqualTo(wasDirty));
            Assert.That(scene.rootCount, Is.EqualTo(rootCount));
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCount));
            Assert.That(SessionState.GetBool(Key, false), Is.False);
            Assert.That(EditorApplication.isPlayingOrWillChangePlaymode, Is.False);
        }
        finally
        {
            Field("confirmSceneSave").SetValue(null, original);
            SessionState.SetBool(Key, oldFlag);
        }
    }

    [Test]
    public void RepeatedPlayEntryClearsDestroyedWrappersAndPendingCapture()
    {
        bool oldFlag = SessionState.GetBool(Key, false);
        try
        {
            for (int run = 0; run < 2; run++)
            {
                var obj = new GameObject("Previous QA run");
                var stale = obj.AddComponent<SailingVoyage>();
                UnityEngine.Object.DestroyImmediate(obj);
                Assert.That(ReferenceEquals(stale, null), Is.False);
                Assert.That(stale == null, Is.True);
                Field("voyage").SetValue(null, stale);
                Field("pendingCapture").SetValue(null, "previous-run-capture.png");
                Field("orientationApplied").SetValue(null, true);
                Field("phase").SetValue(null, 9);
                SessionState.SetBool(Key, true);
                Changed(PlayModeStateChange.EnteredPlayMode);
                Assert.That(Field("voyage").GetValue(null), Is.Null);
                Assert.That(Field("pendingCapture").GetValue(null), Is.Null);
                Assert.That(Field("orientationApplied").GetValue(null), Is.False);
                Assert.That(Field("phase").GetValue(null), Is.EqualTo(0));
                Changed(PlayModeStateChange.ExitingPlayMode);
            }
        }
        finally
        {
            Changed(PlayModeStateChange.ExitingPlayMode);
            SessionState.SetBool(Key, oldFlag);
        }
    }
}
