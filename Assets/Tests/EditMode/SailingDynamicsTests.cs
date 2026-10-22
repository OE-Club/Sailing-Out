using NUnit.Framework;
using SailingOut.Sailing;
using UnityEngine;

public class SailingDynamicsTests
{
    static SailingDynamics SailAt(float heading, float sheetRate)
    {
        SailingDynamics m = new(); m.Reset(Vector2.zero, heading);
        for (int i = 0; i < 300; i++) m.Step(0.02f, 0, sheetRate);
        for (int i = 0; i < 1000; i++) m.Step(0.02f, 0, 0);
        return m;
    }
    [Test] public void BeamReachAcceleratesWhileHeadwindStops()
    {
        var reach = SailAt(335, 0); // 90 degrees from wind travel.
        var headwind = SailAt(245, 0);
        Assert.That(reach.Speed, Is.GreaterThan(2));
        Assert.That(headwind.Speed, Is.LessThan(0.01f));
        Assert.That(headwind.InIrons, Is.True);
    }
    [Test] public void LettingSailOutImprovesDownwindDrive()
    {
        Assert.That(SailAt(65, 1).Speed, Is.GreaterThan(SailAt(65, -1).Speed + 2));
    }
    [Test] public void PullingSailInImprovesCloseHauledDrive()
    {
        Assert.That(SailAt(190, -1).Speed, Is.GreaterThan(SailAt(190, 1).Speed + 1.5f));
    }
    [Test] public void InputsClampAndResetRestoresSpawn()
    {
        var m = SailAt(0, 20);
        Assert.That(m.Sheet, Is.EqualTo(1));
        for (int i = 0; i < 500; i++) m.Step(0.02f, 99, -20);
        Assert.That(m.Sheet, Is.EqualTo(0));
        Assert.That(m.Heading, Is.InRange(0, 360));
        m.Reset();
        Assert.That(m.Position, Is.EqualTo(Vector2.zero)); Assert.That(m.Speed, Is.Zero); Assert.That(m.Sheet, Is.EqualTo(0.55f));
    }
}
