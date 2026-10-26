using UnityEngine;

namespace HSPE.AMModules
{
    // Read the animated body's rotation at the physics step, independently of
    // the Studio object's transform. Keep only the last valid horizontal heading.
    internal sealed class BodyHeadingReference
    {
        private readonly PoseController _owner;
        private readonly GenericOCITarget _target;
        private Quaternion _lastRotation = Quaternion.identity;
        private bool _hasRotation;

        public BodyHeadingReference(PoseController owner, GenericOCITarget target)
        {
            _owner = owner;
            _target = target;
        }

        public Quaternion Rotation
        {
            get
            {
                Quaternion fallback = _hasRotation ? _lastRotation :
                    ExtractHorizontalRotation(_owner.transform.rotation, Quaternion.identity);
                _lastRotation = ExtractHorizontalRotation(GetBone().rotation, fallback);
                _hasRotation = true;
                return _lastRotation;
            }
        }

        public bool UsesObjectTransform { get { return GetBone() == _owner.transform; } }

        public void Reset()
        {
            _hasRotation = false;
        }

        private Transform GetBone()
        {
            if (_target != null && _target.type == GenericOCITarget.Type.Character &&
                _target.ociChar != null && _target.ociChar.finalIK != null &&
                _target.ociChar.finalIK.references != null)
            {
                Transform pelvis = _target.ociChar.finalIK.references.pelvis;
                if (pelvis != null && IsOwnTransform(pelvis))
                    return pelvis;
            }
            return _owner.transform;
        }

        private bool IsOwnTransform(Transform candidate)
        {
            if (!candidate.IsChildOf(_owner.transform))
                return false;
            foreach (GameObject child in _owner._childObjects)
                if (child != null && candidate.IsChildOf(child.transform))
                    return false;
            return true;
        }

        private static Quaternion ExtractHorizontalRotation(Quaternion rotation, Quaternion fallback)
        {
            // Extract twist about world-up. Projecting forward onto the ground
            // would flip the force by 180 degrees when a bend crosses vertical.
            float lengthSquared = rotation.y * rotation.y + rotation.w * rotation.w;
            if (lengthSquared < 0.00000001f)
                return fallback; // A fully inverted swing has no unique heading.
            float inverseLength = 1f / Mathf.Sqrt(lengthSquared);
            return new Quaternion(0f, rotation.y * inverseLength, 0f, rotation.w * inverseLength);
        }
    }
}
