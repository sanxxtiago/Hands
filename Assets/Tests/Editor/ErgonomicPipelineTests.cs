#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class ErgonomicPipelineTests
{
    private const BindingFlags InstancePrivate =
        BindingFlags.Instance | BindingFlags.NonPublic;

    private GameObject _runnerObject;
    private MotionPipelineRunner _runner;

    [SetUp]
    public void SetUp()
    {
        _runnerObject = new GameObject("ErgonomicPipelineTests");
        _runnerObject.SetActive(false);
        _runner = _runnerObject.AddComponent<MotionPipelineRunner>();
        InvokePrivate(_runner, "Awake");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_runnerObject);
    }

    [Test]
    public void ProcessSnapshot_SecondSnapshotDispatchesMatchingPair()
    {
        List<string> dispatchOrder = new();
        FrameMotionData motionFrame = default;
        FrameErgonomicData ergonomicFrame = default;
        int motionCount = 0;
        int ergonomicCount = 0;

        void CaptureMotion(FrameMotionData frame)
        {
            motionFrame = frame;
            motionCount++;
            dispatchOrder.Add("motion");
        }

        void CaptureErgonomic(FrameErgonomicData frame)
        {
            ergonomicFrame = frame;
            ergonomicCount++;
            dispatchOrder.Add("ergonomic");
        }

        MotionEventBus.OnFrame += CaptureMotion;
        ErgonomicEventBus.OnFrame += CaptureErgonomic;

        try
        {
            InvokeProcessSnapshot(CreateSnapshot(1, HandType.LEFT));
            InvokeProcessSnapshot(CreateSnapshot(2, HandType.LEFT));

            Assert.That(motionCount, Is.EqualTo(1));
            Assert.That(ergonomicCount, Is.EqualTo(1));
            Assert.That(dispatchOrder, Is.EqualTo(new[] { "motion", "ergonomic" }));
            Assert.That(ergonomicFrame.frameId, Is.EqualTo(motionFrame.frameId));
            Assert.That(ergonomicFrame.handType, Is.EqualTo(motionFrame.handType));
            Assert.That(ergonomicFrame.timestamp, Is.EqualTo(motionFrame.timestamp));
            Assert.That(ergonomicFrame.isForearmPoseValid, Is.True);
            Assert.That(ergonomicFrame.wristFlexionExtension.isValid, Is.True);
            Assert.That(ergonomicFrame.wristRadialUlnarDeviation.isValid, Is.True);
            Assert.That(ergonomicFrame.wristPronationSupination.isValid, Is.True);
        }
        finally
        {
            MotionEventBus.OnFrame -= CaptureMotion;
            ErgonomicEventBus.OnFrame -= CaptureErgonomic;
        }
    }

    [Test]
    public void ProcessSnapshot_FirstAndDuplicateSnapshotsDoNotDispatch()
    {
        int motionCount = 0;
        int ergonomicCount = 0;

        void CaptureMotion(FrameMotionData frame) => motionCount++;
        void CaptureErgonomic(FrameErgonomicData frame) => ergonomicCount++;

        MotionEventBus.OnFrame += CaptureMotion;
        ErgonomicEventBus.OnFrame += CaptureErgonomic;

        try
        {
            HandDataSnapshot first = CreateSnapshot(1, HandType.RIGHT);
            InvokeProcessSnapshot(first);
            InvokeProcessSnapshot(first);

            Assert.That(motionCount, Is.Zero);
            Assert.That(ergonomicCount, Is.Zero);
        }
        finally
        {
            MotionEventBus.OnFrame -= CaptureMotion;
            ErgonomicEventBus.OnFrame -= CaptureErgonomic;
        }
    }

    [Test]
    public void ProcessSnapshot_LeftAndRightHandsDispatchIndependentPairs()
    {
        List<FrameMotionData> motionFrames = new();
        List<FrameErgonomicData> ergonomicFrames = new();

        void CaptureMotion(FrameMotionData frame) => motionFrames.Add(frame);
        void CaptureErgonomic(FrameErgonomicData frame) => ergonomicFrames.Add(frame);

        MotionEventBus.OnFrame += CaptureMotion;
        ErgonomicEventBus.OnFrame += CaptureErgonomic;

        try
        {
            InvokeProcessSnapshot(CreateSnapshot(1, HandType.LEFT));
            InvokeProcessSnapshot(CreateSnapshot(1, HandType.RIGHT));
            InvokeProcessSnapshot(CreateSnapshot(2, HandType.LEFT));
            InvokeProcessSnapshot(CreateSnapshot(2, HandType.RIGHT));

            Assert.That(motionFrames.Count, Is.EqualTo(2));
            Assert.That(ergonomicFrames.Count, Is.EqualTo(2));
            Assert.That(motionFrames[0].handType, Is.EqualTo(HandType.LEFT));
            Assert.That(motionFrames[1].handType, Is.EqualTo(HandType.RIGHT));

            for (int index = 0; index < motionFrames.Count; index++)
            {
                Assert.That(
                    ergonomicFrames[index].frameId,
                    Is.EqualTo(motionFrames[index].frameId));
                Assert.That(
                    ergonomicFrames[index].handType,
                    Is.EqualTo(motionFrames[index].handType));
                Assert.That(
                    ergonomicFrames[index].timestamp,
                    Is.EqualTo(motionFrames[index].timestamp));
            }
        }
        finally
        {
            MotionEventBus.OnFrame -= CaptureMotion;
            ErgonomicEventBus.OnFrame -= CaptureErgonomic;
        }
    }

    [Test]
    public void ProcessSnapshot_AggregatorDisagreementDropsBothFrames()
    {
        int motionCount = 0;
        int ergonomicCount = 0;

        void CaptureMotion(FrameMotionData frame) => motionCount++;
        void CaptureErgonomic(FrameErgonomicData frame) => ergonomicCount++;

        MotionEventBus.OnFrame += CaptureMotion;
        ErgonomicEventBus.OnFrame += CaptureErgonomic;

        try
        {
            InvokeProcessSnapshot(CreateSnapshot(1, HandType.LEFT));

            SetPrivateField(
                _runner,
                "_leftErgonomicAggregator",
                CreateErgonomicAggregator(HandType.LEFT));

            LogAssert.Expect(
                LogType.Error,
            "[MotionPipelineRunner] Desincronizacion entre MotionData y " +
                "ErgonomicData para LEFT en frame 2.");

            InvokeProcessSnapshot(CreateSnapshot(2, HandType.LEFT));

            Assert.That(motionCount, Is.Zero);
            Assert.That(ergonomicCount, Is.Zero);
        }
        finally
        {
            MotionEventBus.OnFrame -= CaptureMotion;
            ErgonomicEventBus.OnFrame -= CaptureErgonomic;
        }
    }

    private void InvokeProcessSnapshot(HandDataSnapshot snapshot)
    {
        InvokePrivate(_runner, "ProcessSnapshot", snapshot);
    }

    private static ErgonomicAggregator CreateErgonomicAggregator(HandType handType)
    {
        return new ErgonomicAggregator(
            handType,
            new List<IErgonomicDetector>
            {
                new ForearmPostureDetector(),
                new WristPostureDetector()
            });
    }

    private static HandDataSnapshot CreateSnapshot(long frameId, HandType handType)
    {
        return new HandDataSnapshot
        {
            frameId = frameId,
            timestamp = frameId * 0.01f,
            handType = handType,
            elbowPosition = Vector3.zero,
            wristPosition = Vector3.forward,
            palmPosition = Vector3.forward,
            palmNormal = Vector3.up,
            handDirection = Vector3.forward,
            forearmDirection = Vector3.forward,
            palmRotation = Quaternion.identity,
            forearmRotation = Quaternion.identity
        };
    }

    private static void InvokePrivate(
        MotionPipelineRunner target,
        string methodName,
        params object[] arguments)
    {
        MethodInfo method = typeof(MotionPipelineRunner).GetMethod(
            methodName,
            InstancePrivate);

        Assert.That(method, Is.Not.Null);
        method.Invoke(target, arguments);
    }

    private static void SetPrivateField<T>(
        MotionPipelineRunner target,
        string fieldName,
        T value)
    {
        FieldInfo field = typeof(MotionPipelineRunner).GetField(
            fieldName,
            InstancePrivate);

        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }
}
#endif
