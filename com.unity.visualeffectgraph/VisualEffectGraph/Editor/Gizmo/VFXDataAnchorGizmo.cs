using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;
using Type = System.Type;
using Delegate = System.Delegate;
using Action = System.Action;

namespace UnityEditor.VFX.UI
{
    public class VFXDataAnchorGizmo
    {
        static Dictionary<System.Type, System.Action<VFXDataAnchorPresenter, VFXComponent>> s_DrawFunctions;

        static VFXDataAnchorGizmo()
        {
            s_DrawFunctions = new Dictionary<System.Type, System.Action<VFXDataAnchorPresenter, VFXComponent>>();

            s_DrawFunctions[typeof(Sphere)] = OnDrawSphereDataAnchorGizmo;
            s_DrawFunctions[typeof(Position)] = OnDrawPositionDataAnchorGizmo;
            s_DrawFunctions[typeof(AABox)] = OnDrawAABoxDataAnchorGizmo;
            s_DrawFunctions[typeof(OrientedBox)] = OnDrawOrientedBoxDataAnchorGizmo;
            s_DrawFunctions[typeof(Plane)] = OnDrawPlaneDataAnchorGizmo;
            s_DrawFunctions[typeof(Cylinder)] = OnDrawCylinderDataAnchorGizmo;

            foreach (Type type in typeof(VFXDataAnchorGizmo).Assembly.GetTypes())
            {
                Type gizmoedType = GetGizmoType(type);

                if (gizmoedType != null)
                {
                    MethodInfo info = type.GetMethod("OnDrawGizmo", BindingFlags.Static | BindingFlags.Public);

                    if (info != null)
                    {
                        s_DrawFunctions[gizmoedType] = (System.Action<VFXDataAnchorPresenter, VFXComponent>)Delegate.CreateDelegate(typeof(System.Action<VFXDataAnchorPresenter, VFXComponent>), info);
                    }
                }
            }
        }

        static Type GetGizmoType(Type type)
        {
            Type baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType && !baseType.IsGenericTypeDefinition && baseType.GetGenericTypeDefinition() == typeof(VFXGizmo<>))
                {
                    return baseType.GetGenericArguments()[0];
                }
                baseType = baseType.BaseType;
            }
            return null;
        }

        static internal void Draw(VFXDataAnchorPresenter anchor, VFXComponent component)
        {
            System.Action<VFXDataAnchorPresenter, VFXComponent> func;
            if (s_DrawFunctions.TryGetValue(anchor.anchorType, out func))
            {
                func(anchor, component);
            }
        }

        static bool PositionGizmo(VFXComponent component, CoordinateSpace space, ref Vector3 position)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 worldPosition = position;
            if (space == CoordinateSpace.Local)
            {
                worldPosition = component.transform.localToWorldMatrix.MultiplyPoint(position);
            }

            Vector3 modifiedPosition = Handles.PositionHandle(worldPosition, space == CoordinateSpace.Local ? component.transform.rotation : Quaternion.identity);
            if (space == CoordinateSpace.Local)
            {
                modifiedPosition = component.transform.worldToLocalMatrix.MultiplyPoint(modifiedPosition);
            }
            bool changed = GUI.changed;
            EditorGUI.EndChangeCheck();
            if (changed)
            {
                position = modifiedPosition;
                return true;
            }
            return false;
        }

        static bool RotationGizmo(VFXComponent component, CoordinateSpace space, Vector3 position, ref Vector3 rotation)
        {
            EditorGUI.BeginChangeCheck();
            if (space == CoordinateSpace.Local)
            {
                position = component.transform.worldToLocalMatrix.MultiplyPoint(position);
            }

            Quaternion modifiedRotation = Handles.RotationHandle(Quaternion.Euler(rotation), position);

            bool changed = GUI.changed;
            EditorGUI.EndChangeCheck();
            if (changed)
            {
                rotation = modifiedRotation.eulerAngles;
                return true;
            }
            return false;
        }

        static bool RotationGizmo(VFXComponent component, CoordinateSpace space, Vector3 position, ref Quaternion rotation)
        {
            EditorGUI.BeginChangeCheck();
            if (space == CoordinateSpace.Local)
            {
                position = component.transform.worldToLocalMatrix.MultiplyPoint(position);
            }

            Quaternion modifiedRotation = Handles.RotationHandle(rotation, position);

            bool changed = GUI.changed;
            EditorGUI.EndChangeCheck();
            if (changed)
            {
                rotation = modifiedRotation;
                return true;
            }
            return false;
        }

        static void OnDrawPositionDataAnchorGizmo(VFXDataAnchorPresenter anchor, VFXComponent component)
        {
            Position pos = (Position)anchor.value;

            if (PositionGizmo(component, pos.space, ref pos.position))
            {
                anchor.value = pos;
            }
        }

        static void OnDrawSphereDataAnchorGizmo(VFXDataAnchorPresenter anchor, VFXComponent component)
        {
            Sphere sphere = (Sphere)anchor.value;

            Vector3 center = sphere.center;
            float radius = sphere.radius;
            if (sphere.space == CoordinateSpace.Local)
            {
                center = component.transform.localToWorldMatrix.MultiplyPoint(center);
            }

            Handles.DrawWireArc(center, Vector3.forward, Vector3.up, 360f, radius);
            Handles.DrawWireArc(center, Vector3.up, Vector3.right, 360f, radius);
            Handles.DrawWireArc(center, Vector3.right, Vector3.forward, 360f, radius);

            if (PositionGizmo(component, sphere.space, ref sphere.center))
            {
                anchor.value = sphere;
            }

            foreach (var dist in new Vector3[] { Vector3.left, Vector3.up, Vector3.forward })
            {
                EditorGUI.BeginChangeCheck();
                Vector3 sliderPos = center + dist * radius;
                Vector3 result = Handles.Slider(sliderPos, dist, 0.1f, Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    sphere.radius = (result - center).magnitude;

                    if (float.IsNaN(sphere.radius))
                    {
                        sphere.radius = 0;
                    }

                    anchor.value = sphere;
                }
                EditorGUI.EndChangeCheck();
            }
        }

        static void OnDrawAABoxDataAnchorGizmo(VFXDataAnchorPresenter anchor, VFXComponent component)
        {
            AABox box = (AABox)anchor.value;

            if (OnDrawBoxDataAnchorGizmo(anchor, component, box.space, ref box.center, ref box.size, Vector3.zero))
            {
                anchor.value = box;
            }
        }

        static void OnDrawOrientedBoxDataAnchorGizmo(VFXDataAnchorPresenter anchor, VFXComponent component)
        {
            OrientedBox box = (OrientedBox)anchor.value;

            if (OnDrawBoxDataAnchorGizmo(anchor, component, box.space, ref box.center, ref box.size, box.angles))
            {
                anchor.value = box;
            }
            if (RotationGizmo(component, box.space, box.center, ref box.angles))
            {
                anchor.value = box;
            }
        }

        static void OnDrawPlaneDataAnchorGizmo(VFXDataAnchorPresenter anchor, VFXComponent component)
        {
            Plane plane = (Plane)anchor.value;

            Quaternion normalQuat = Quaternion.FromToRotation(Vector3.forward, plane.normal);
            Handles.DrawRectangle(0, plane.position, normalQuat, 10);

            Handles.DrawArrow(0, plane.position, normalQuat, 5);

            if (PositionGizmo(component, plane.space, ref plane.position))
            {
                anchor.value = plane;
            }

            Vector3 normal = plane.normal.normalized;

            Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, normal);

            EditorGUI.BeginChangeCheck();
            Quaternion result = Handles.RotationHandle(rotation, plane.position);


            //Quaternion result = UnityEditorInternal.Disc.Do(0, rotation, plane.position, Vector3.left, 3, true, 0, false, true, Color.yellow);

            if (GUI.changed)
            {
                normal = result * Vector3.forward;
                plane.normal = normal;
                anchor.value = plane;
            }
            EditorGUI.EndChangeCheck();
        }

        static void OnDrawCylinderDataAnchorGizmo(VFXDataAnchorPresenter anchor, VFXComponent component)
        {
            Cylinder cylinder = (Cylinder)anchor.value;


            Vector3 center = cylinder.position;
            Vector3 normal = cylinder.direction.normalized;
            Vector3 topCap = center + cylinder.height * 0.5f * normal;
            Vector3 bottomCap = center - cylinder.height * 0.5f * normal;

            Vector3[] extremities = new Vector3[8];

            extremities[0] = topCap + Vector3.forward * cylinder.radius;
            extremities[1] = topCap - Vector3.forward * cylinder.radius;

            extremities[2] = topCap + Vector3.left * cylinder.radius;
            extremities[3] = topCap - Vector3.left * cylinder.radius;

            extremities[4] = bottomCap + Vector3.forward * cylinder.radius;
            extremities[5] = bottomCap - Vector3.forward * cylinder.radius;

            extremities[6] = bottomCap + Vector3.left * cylinder.radius;
            extremities[7] = bottomCap - Vector3.left * cylinder.radius;

            if (cylinder.space == CoordinateSpace.Local)
            {
                Matrix4x4 mat = component.transform.localToWorldMatrix;

                center = mat.MultiplyPoint(center);
                topCap = mat.MultiplyPoint(topCap);
                bottomCap = mat.MultiplyPoint(bottomCap);

                normal = mat.MultiplyVector(normal).normalized;

                for (int i = 0; i < extremities.Length; ++i)
                {
                    extremities[i] = mat.MultiplyPoint(extremities[i]);
                }
            }

            Handles.DrawWireDisc(topCap, normal, cylinder.radius);
            Handles.DrawWireDisc(bottomCap, normal, cylinder.radius);

            for (int i = 0; i < extremities.Length / 2; ++i)
            {
                Handles.DrawLine(extremities[i], extremities[i + extremities.Length / 2]);
            }

            if (PositionGizmo(component, cylinder.space, ref cylinder.position))
            {
                anchor.value = cylinder;
            }

            Vector3 result;
            for (int i = 0; i < extremities.Length / 2; ++i)
            {
                EditorGUI.BeginChangeCheck();

                Vector3 pos = (extremities[i] + extremities[i + +extremities.Length / 2]) * 0.5f;
                result = Handles.Slider(pos, pos - center, 0.1f, Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    cylinder.radius = (result - center).magnitude;
                    anchor.value = cylinder;
                }

                EditorGUI.EndChangeCheck();
            }

            EditorGUI.BeginChangeCheck();

            result = Handles.Slider(topCap, topCap - center, 0.1f, Handles.CubeHandleCap, 0);

            if (GUI.changed)
            {
                cylinder.height = (result - center).magnitude * 2;
                anchor.value = cylinder;
            }

            EditorGUI.EndChangeCheck();

            EditorGUI.BeginChangeCheck();

            result = Handles.Slider(bottomCap, bottomCap - center, 0.1f, Handles.CubeHandleCap, 0);

            if (GUI.changed)
            {
                cylinder.height = (result - center).magnitude * 2;
                anchor.value = cylinder;
            }

            EditorGUI.EndChangeCheck();

            /*
            Vector3 normal = plane.normal.normalized;

            Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, normal);

            EditorGUI.BeginChangeCheck();
            Quaternion result = Handles.RotationHandle(rotation, plane.position);

            //Quaternion result = UnityEditorInternal.Disc.Do(0, rotation, plane.position, Vector3.left, 3, true, 0, false, true, Color.yellow);

            if (GUI.changed)
            {
                normal = result * Vector3.forward;
                plane.normal = normal;
                anchor.value = plane;
            }
            EditorGUI.EndChangeCheck();
    */
        }

        static bool OnDrawBoxDataAnchorGizmo(VFXDataAnchorPresenter anchor, VFXComponent component, CoordinateSpace space, ref Vector3 center, ref Vector3 size, Vector3 additionnalRotation)
        {
            Vector3 worldCenter = center;
            if (space == CoordinateSpace.Local)
            {
                worldCenter = component.transform.localToWorldMatrix.MultiplyPoint(center);
            }
            Vector3[] points = new Vector3[8];

            Matrix4x4 addMat = Matrix4x4.Rotate(Quaternion.Euler(additionnalRotation));

            points[0] = center + addMat.MultiplyPoint(new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f));
            points[1] = center + addMat.MultiplyPoint(new Vector3(size.x * 0.5f, -size.y * 0.5f, size.z * 0.5f));

            points[2] = center + addMat.MultiplyPoint(new Vector3(-size.x * 0.5f, size.y * 0.5f, size.z * 0.5f));
            points[3] = center + addMat.MultiplyPoint(new Vector3(-size.x * 0.5f, -size.y * 0.5f, size.z * 0.5f));

            points[4] = center + addMat.MultiplyPoint(new Vector3(size.x * 0.5f, size.y * 0.5f, -size.z * 0.5f));
            points[5] = center + addMat.MultiplyPoint(new Vector3(size.x * 0.5f, -size.y * 0.5f, -size.z * 0.5f));

            points[6] = center + addMat.MultiplyPoint(new Vector3(-size.x * 0.5f, size.y * 0.5f, -size.z * 0.5f));
            points[7] = center + addMat.MultiplyPoint(new Vector3(-size.x * 0.5f, -size.y * 0.5f, -size.z * 0.5f));


            Matrix4x4 mat = Matrix4x4.identity;

            if (space == CoordinateSpace.Local)
            {
                mat = component.transform.localToWorldMatrix;
                for (int i = 0; i < points.Length; ++i)
                {
                    points[i] = mat.MultiplyPoint(points[i]);
                }
            }


            Handles.DrawLine(points[0], points[1]);
            Handles.DrawLine(points[2], points[3]);
            Handles.DrawLine(points[4], points[5]);
            Handles.DrawLine(points[6], points[7]);

            Handles.DrawLine(points[0], points[2]);
            Handles.DrawLine(points[0], points[4]);
            Handles.DrawLine(points[1], points[3]);
            Handles.DrawLine(points[1], points[5]);

            Handles.DrawLine(points[2], points[6]);
            Handles.DrawLine(points[3], points[7]);
            Handles.DrawLine(points[4], points[6]);
            Handles.DrawLine(points[5], points[7]);

            bool changed = false;

            EditorGUI.BeginChangeCheck();

            {
                // axis +Z
                Vector3 middle = (points[0] + points[1] + points[2] + points[3]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), 0.1f, Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.z = (middleResult - worldCenter).magnitude * 2;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            {
                // axis -Z
                Vector3 middle = (points[4] + points[5] + points[6] + points[7]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), 0.1f, Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.z = (middleResult - worldCenter).magnitude * 2;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            {
                // axis +X
                Vector3 middle = (points[0] + points[1] + points[4] + points[5]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), 0.1f, Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.x = (middleResult - worldCenter).magnitude * 2;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            {
                // axis -X
                Vector3 middle = (points[2] + points[3] + points[6] + points[7]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), 0.1f, Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.x = (middleResult - worldCenter).magnitude * 2;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            {
                // axis +Y
                Vector3 middle = (points[0] + points[2] + points[4] + points[6]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), 0.1f, Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.y = (middleResult - worldCenter).magnitude * 2;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            {
                // axis -Y
                Vector3 middle = (points[1] + points[3] + points[5] + points[7]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), 0.1f, Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.y = (middleResult - worldCenter).magnitude * 2;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();


            if (PositionGizmo(component, space, ref center))
            {
                changed = true;
            }


            return changed;
        }
    }

    class Gizmo
    {
    }

    class VFXGizmo<T> : VFXGizmo
    {
        public static bool PositionGizmo(VFXComponent component, CoordinateSpace space, ref Vector3 position)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 worldPosition = position;
            if (space == CoordinateSpace.Local)
            {
                worldPosition = component.transform.localToWorldMatrix.MultiplyPoint(position);
            }

            Vector3 modifiedPosition = Handles.PositionHandle(worldPosition, space == CoordinateSpace.Local ? component.transform.rotation : Quaternion.identity);
            if (space == CoordinateSpace.Local)
            {
                modifiedPosition = component.transform.worldToLocalMatrix.MultiplyPoint(modifiedPosition);
            }
            bool changed = GUI.changed;
            EditorGUI.EndChangeCheck();
            if (changed)
            {
                position = modifiedPosition;
                return true;
            }
            return false;
        }
    }

    class VFXPositionGizmo : VFXGizmo<Position>
    {
        public static void OnDrawGizmo(VFXDataAnchorPresenter anchor, VFXComponent component)
        {
            Position pos = (Position)anchor.value;

            if (PositionGizmo(component, pos.space, ref pos.position))
            {
                anchor.value = pos;
            }
        }
    }
}
