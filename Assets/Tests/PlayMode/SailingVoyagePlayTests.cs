using System.Collections;
using NUnit.Framework;
using SailingOut.Core;
using SailingOut.Sailing;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class SailingVoyagePlayTests
{
    SailingVoyage voyage;
    [UnitySetUp] public IEnumerator Load()
    {
        SceneManager.LoadScene("FreeSailing"); yield return null; yield return null;
        voyage = Object.FindAnyObjectByType<SailingVoyage>(); Assert.That(voyage, Is.Not.Null);
    }
    [UnityTest] public IEnumerator RepeatedStartResetPauseAndControlsRemainCoherent()
    {
        int count = voyage.GetComponentsInChildren<SpriteRenderer>().Length;
        for (int i = 0; i < 5; i++) { voyage.BeginVoyage(); voyage.BeginVoyage(); voyage.ResetVoyage(); }
        Assert.That(voyage.Session.State, Is.EqualTo(GameState.Ready));
        Assert.That(voyage.GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(count));
        Assert.That(Object.FindObjectsByType<GameSession>().Length, Is.EqualTo(1));
        voyage.BeginVoyage(); voyage.SetControlInput(true);
        for (int i = 0; i < 80; i++) yield return new WaitForFixedUpdate();
        Assert.That(voyage.Model.Speed, Is.GreaterThan(0.8f));
        voyage.SetControlInput(true, 1, -1);
        for (int i = 0; i < 100; i++) yield return new WaitForFixedUpdate();
        Assert.That(voyage.Model.Heading, Is.GreaterThan(30)); Assert.That(voyage.Model.Sheet, Is.LessThan(0.1f));
        voyage.TogglePause(); Vector2 position = voyage.Model.Position;
        for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
        Assert.That(voyage.Model.Position, Is.EqualTo(position));
        voyage.BeginVoyage(); Assert.That(voyage.Session.State, Is.EqualTo(GameState.Playing));
        voyage.ResetVoyage(); Assert.That(voyage.Model.Position, Is.EqualTo(Vector2.zero));
    }
    [UnityTest] public IEnumerator SeatedCrewAndFreeSceneReloadRemainIsolated()
    {
        Assert.That(voyage.transform.Find("Sloop/Crew seat"), Is.Not.Null);
        var crew = voyage.transform.Find("Sloop/Life-vest crew").GetComponent<SpriteRenderer>();
        for (int i = 0; i < 4; i++)
        {
            voyage.Model.Reset(Vector2.zero, i * 90); yield return null;
            Assert.That(crew.sprite.name, Does.StartWith("Seated"));
            Assert.That(crew.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(crew.sortingOrder, Is.GreaterThan(voyage.transform.Find("Sloop/Crew seat").GetComponent<SpriteRenderer>().sortingOrder));
        }
        foreach (Transform child in voyage.GetComponentsInChildren<Transform>())
            Assert.That(child.name, Does.Not.Contain("buoy").And.Not.Contain("Gate").And.Not.Contain("Target"));
        SceneManager.LoadScene("FreeSailing"); yield return null; yield return null;
        Assert.That(Object.FindObjectsByType<GameSession>().Length, Is.EqualTo(1));
        Assert.That(Object.FindObjectsByType<SailingVoyage>().Length, Is.EqualTo(1));
        Assert.That(Object.FindAnyObjectByType<SailingVoyage>().Session.State, Is.EqualTo(GameState.Ready));
    }
}
