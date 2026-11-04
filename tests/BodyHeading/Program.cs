using HSPE;
using HSPE.AMModules;
using UnityEngine;

// Run the production class with managed math doubles, without a game install.
// This does not exercise Unity's physics, animation callbacks, or scene loading.
// Run: dotnet run --project tests/BodyHeading/BodyHeading.csproj
internal static class Program
{
    private static int Main()
    {
        var tests = new (string Name, Action Run)[]
        {
            ("Combined upright pitch and roll preserve heading", UprightCombinations),
            ("Pure bends cross vertical without reversing force", BendAcrossVertical),
            ("Pure side rolls preserve heading", SideRolls),
            ("Combined tumbles remain continuous", CombinedTumbles),
            ("Turning through the angle wrap remains continuous", TurnAcrossWrap),
            ("An immediate half turn follows the body", ImmediateHalfTurn),
            ("Repeated reads and fresh references agree", RepeatAndReinitialize),
            ("Fully inverted poses keep the last valid heading", InvertedFallback),
            ("Reset clears the inverted-pose fallback", ResetFallback),
            ("Characters follow animated pelvis rotation", AnimatedPelvis),
            ("Invalid pelvis references fall back to object rotation", InvalidPelvis)
        };
        int failed = 0;
        foreach (var test in tests)
        {
            try { test.Run(); Console.WriteLine("PASS " + test.Name); }
            catch (Exception e) { failed++; Console.WriteLine("FAIL " + test.Name + ": " + e.Message); }
        }
        Console.WriteLine($"{tests.Length - failed}/{tests.Length} cases passed.");
        return failed == 0 ? 0 : 1;
    }

    private static (PoseController Owner, BodyHeadingReference Heading) Item()
    {
        var owner = new PoseController();
        return (owner, new BodyHeadingReference(owner, null));
    }

    private static void Expect(Quaternion heading, float yaw, string context)
    {
        Vector3 actual = heading * Vector3.forward;
        Vector3 expected = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
        float error = MathF.Sqrt(MathF.Pow(actual.x - expected.x, 2) + MathF.Pow(actual.y - expected.y, 2) + MathF.Pow(actual.z - expected.z, 2));
        if (!float.IsFinite(error) || error > 0.0002f)
            throw new Exception($"{context}: expected yaw {yaw}, got ({actual.x}, {actual.y}, {actual.z})");
    }

    private static void UprightCombinations()
    {
        foreach (float pitch in new[] { -60f, -45f, 0f, 45f, 60f })
        foreach (float roll in new[] { -60f, -45f, 0f, 45f, 60f })
        foreach (float yaw in new[] { -179f, -90f, 0f, 30f, 179f })
        {
            var item = Item();
            item.Owner.transform.rotation = Quaternion.Euler(pitch, yaw, roll);
            Expect(item.Heading.Rotation, yaw, $"pitch={pitch}, roll={roll}");
        }
    }

    private static void BendAcrossVertical()
    {
        foreach (float direction in new[] { -1f, 1f })
        {
            var item = Item();
            for (int step = 0; step <= 1440; step++)
            {
                item.Owner.transform.rotation = Quaternion.Euler(direction * step / 4, 35, 0);
                Expect(item.Heading.Rotation, 35, "bend step=" + step);
            }
        }
    }

    private static void SideRolls()
    {
        var item = Item();
        for (int step = -720; step <= 720; step++)
        {
            item.Owner.transform.rotation = Quaternion.Euler(0, -70, step / 2f);
            Expect(item.Heading.Rotation, -70, "side roll step=" + step);
        }
    }

    private static void Continuous(Func<float, Quaternion> pose, string context)
    {
        var item = Item();
        Quaternion previous = Quaternion.identity;
        for (int step = 0; step <= 1440; step++)
        {
            item.Owner.transform.rotation = pose(step / 4f);
            Quaternion heading = item.Heading.Rotation;
            Vector3 forward = heading * Vector3.forward;
            float length = MathF.Sqrt(forward.x * forward.x + forward.y * forward.y + forward.z * forward.z);
            if (!float.IsFinite(length) || MathF.Abs(length - 1) > 0.0002f || MathF.Abs(forward.y) > 0.0002f)
                throw new Exception(context + ": invalid horizontal rotation");
            if (step > 0 && Quaternion.Angle(previous, heading) > 5f)
                throw new Exception(context + ": discontinuity at " + step / 4f);
            previous = heading;
        }
    }

    private static void CombinedTumbles()
    {
        foreach (float tilt in new[] { -60f, -45f, 30f, 45f, 60f })
        {
            Continuous(angle => Quaternion.Euler(angle, 25, tilt), "pitch sweep with roll=" + tilt);
            Continuous(angle => Quaternion.Euler(tilt, 25, angle), "roll sweep with pitch=" + tilt);
        }
    }

    private static void TurnAcrossWrap() => Continuous(angle => Quaternion.Euler(45, angle, 45), "yaw wrap");

    private static void ImmediateHalfTurn()
    {
        var item = Item();
        Expect(item.Heading.Rotation, 0, "initial");
        item.Owner.transform.rotation = Quaternion.Euler(45, 180, 45);
        Expect(item.Heading.Rotation, 180, "half turn");
    }

    private static void RepeatAndReinitialize()
    {
        var item = Item();
        foreach (float pitch in new[] { 45f, 80f, 90f, 100f, 135f })
        {
            item.Owner.transform.rotation = Quaternion.Euler(pitch, 25, 30);
            Vector3 expected = item.Heading.Rotation * Vector3.forward;
            float yaw = MathF.Atan2(expected.x, expected.z) * 180 / MathF.PI;
            var fresh = new BodyHeadingReference(item.Owner, null);
            for (int read = 0; read < 10; read++)
            {
                Expect(item.Heading.Rotation, yaw, "repeated read");
                Expect(fresh.Rotation, yaw, "fresh reference");
            }
        }
    }

    private static (PoseController Owner, GenericOCITarget Target, BodyHeadingReference Heading) Character()
    {
        var owner = new PoseController();
        var target = new GenericOCITarget();
        target.ociChar.finalIK.references.pelvis = new Transform { parent = owner.transform };
        return (owner, target, new BodyHeadingReference(owner, target));
    }

    private static void InvertedFallback()
    {
        var character = Character();
        var pelvis = character.Target.ociChar.finalIK.references.pelvis;
        pelvis.rotation = Quaternion.Euler(0, 65, 0);
        Expect(character.Heading.Rotation, 65, "initial");
        pelvis.rotation = Quaternion.Euler(180, 0, 0);
        Expect(character.Heading.Rotation, 65, "inverted");
    }

    private static void ResetFallback()
    {
        var character = Character();
        character.Target.ociChar.finalIK.references.pelvis.rotation = Quaternion.Euler(0, 65, 0);
        Expect(character.Heading.Rotation, 65, "initial");
        character.Owner.transform.rotation = Quaternion.Euler(0, 20, 0);
        character.Target.ociChar.finalIK.references.pelvis.rotation = Quaternion.Euler(180, 0, 0);
        character.Heading.Reset();
        Expect(character.Heading.Rotation, 20, "reset");
    }

    private static void AnimatedPelvis()
    {
        var character = Character();
        character.Target.ociChar.finalIK.references.pelvis.rotation = Quaternion.Euler(45, 110, 45);
        Expect(character.Heading.Rotation, 110, "animated pelvis");
        if (character.Heading.UsesObjectTransform) throw new Exception("ignored valid pelvis");
    }

    private static void InvalidPelvis()
    {
        var character = Character();
        character.Owner.transform.rotation = Quaternion.Euler(45, -30, 45);
        character.Target.ociChar.finalIK.references.pelvis = new Transform();
        Expect(character.Heading.Rotation, -30, "unrelated pelvis");
        if (!character.Heading.UsesObjectTransform) throw new Exception("accepted unrelated pelvis");
    }
}
