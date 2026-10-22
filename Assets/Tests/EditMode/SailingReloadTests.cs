using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SailingOut.Core;
using SailingOut.Sailing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class SailingReloadTests
{
    const string SnapshotKey = "SailingOut.Tests.ReloadSnapshot";
    [Serializable] class Snapshot
    {
        public Vector2 position;
        public float heading, speed, sheet, wind;
        public int transforms, renderers;
        public string sessionId;
    }

    [UnityTest]
    public IEnumerator ActualRepeatedScriptReloadPreservesVoyageAndRestoresReferences()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/FreeSailing.unity");
        yield return new EnterPlayMode();
        yield return null;
        PrepareVoyage();
        yield return new ReloadScriptsWhilePlaying();
        AssertRestoredVoyage();
        ResumeSteerAndPause();
        yield return new ReloadScriptsWhilePlaying();
        AssertRestoredVoyage();
        VerifyResetAfterReload();
        yield return new ExitPlayMode();
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        SessionState.EraseString(SnapshotKey);
        if (EditorApplication.isPlaying) yield return new ExitPlayMode();
    }

    static SailingVoyage Voyage => UnityEngine.Object.FindAnyObjectByType<SailingVoyage>();
    static object Field(SailingVoyage v, string name) => typeof(SailingVoyage)
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(v);

    static void PrepareVoyage()
    {
        var v = Voyage;
        v.BeginVoyage();
        v.SetControlInput(true);
        v.Model.Reset(new Vector2(8, 3), 90, 0.8f);
        v.Model.WindHeading = 72;
        for (int i = 0; i < 20; i++) v.Model.Step(0.02f, 0, 0);
        v.TogglePause();
        Save(v);
    }

    static void Save(SailingVoyage v)
    {
        SessionState.SetString(SnapshotKey, JsonUtility.ToJson(new Snapshot
        {
            position = v.Model.Position, heading = v.Model.Heading, speed = v.Model.Speed,
            sheet = v.Model.Sheet, wind = v.Model.WindHeading,
            transforms = v.GetComponentsInChildren<Transform>().Length,
            renderers = v.GetComponentsInChildren<SpriteRenderer>().Length,
            sessionId = v.Session.GetEntityId().ToString()
        }));
    }

    static void AssertRestoredVoyage()
    {
        var v = Voyage;
        Assert.That(v, Is.Not.Null);
        var expected = JsonUtility.FromJson<Snapshot>(SessionState.GetString(SnapshotKey, ""));
        Assert.That(v.Model.Position, Is.EqualTo(expected.position));
        Assert.That(v.Model.Heading, Is.EqualTo(expected.heading));
        Assert.That(v.Model.Speed, Is.EqualTo(expected.speed).And.GreaterThan(0));
        Assert.That(v.Model.Sheet, Is.EqualTo(expected.sheet));
        Assert.That(v.Model.WindHeading, Is.EqualTo(expected.wind));
        Assert.That(v.Session, Is.SameAs(GameSession.Instance));
        Assert.That(v.Session.GetEntityId().ToString(), Is.EqualTo(expected.sessionId));
        Assert.That(v.Session.State, Is.EqualTo(GameState.Paused));
        Assert.That(UnityEngine.Object.FindObjectsByType<GameSession>().Length, Is.EqualTo(1));
        Assert.That(v.GetComponentsInChildren<Transform>().Length, Is.EqualTo(expected.transforms));
        Assert.That(v.GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(expected.renderers));
        Assert.That(Field(v, "art"), Is.Not.Null);
        foreach (var name in new[] { "vessel", "sail", "crew", "wake", "crewRenderer", "wakeRenderer", "view" })
            Assert.That((UnityEngine.Object)Field(v, name) != null, Is.True, name);
        var foam = (Transform[])Field(v, "foam");
        Assert.That(foam.Length, Is.EqualTo(60));
        foreach (var f in foam) Assert.That(f != null, Is.True, "Foam reference lost");
        foreach (var r in v.GetComponentsInChildren<SpriteRenderer>())
        {
            Assert.That(r.sprite != null, Is.True, r.name);
            Assert.That(r.sprite.texture != null, Is.True, r.name);
        }
        for (int facing = 0; facing < 4; facing++)
        {
            v.Model.Reset(expected.position, facing * 90, expected.sheet);
            v.SendMessage("LateUpdate"); // Exercises art selection, wake, camera and every foam entry.
            Assert.That(v.transform.Find("Sloop/Life-vest crew").GetComponent<SpriteRenderer>().sprite.name,
                Is.EqualTo(new[] { "Seated North", "Seated East", "Seated South", "Seated West" }[facing]));
        }
        LogAssert.NoUnexpectedReceived();
    }

    static void ResumeSteerAndPause()
    {
        var v = Voyage;
        v.BeginVoyage();
        Assert.That(v.Session.State, Is.EqualTo(GameState.Playing));
        v.Model.Reset(v.Model.Position, 90, 0.8f);
        for (int i = 0; i < 40; i++) v.Model.Step(0.02f, 1, -1);
        Assert.That(v.Model.Heading, Is.GreaterThan(90));
        Assert.That(v.Model.Sheet, Is.LessThan(0.8f));
        v.TogglePause();
        Save(v);
    }

    static void VerifyResetAfterReload()
    {
        var v = Voyage;
        v.ResetVoyage();
        Assert.That(v.Model.Position, Is.EqualTo(Vector2.zero));
        Assert.That(v.Model.Speed, Is.Zero);
        Assert.That(v.Model.Sheet, Is.EqualTo(0.55f));
        Assert.That(v.Session.State, Is.EqualTo(GameState.Ready));
        v.BeginVoyage();
        Assert.That(v.Session.State, Is.EqualTo(GameState.Playing));
    }

    sealed class ReloadScriptsWhilePlaying : IEditModeTestYieldInstruction
    {
        public bool ExpectDomainReload => true;
        public bool ExpectedPlaymodeState => true;
        public IEnumerator Perform()
        {
            yield return null; // Let the test runner persist its continuation first.
            EditorUtility.RequestScriptReload();
            EditorApplication.UnlockReloadAssemblies();
            double deadline = EditorApplication.timeSinceStartup + 30;
            while (EditorApplication.timeSinceStartup < deadline) yield return null;
            throw new TimeoutException("Play Mode script reload did not occur.");
        }
    }
}
