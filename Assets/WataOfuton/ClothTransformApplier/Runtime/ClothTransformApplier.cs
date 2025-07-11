#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.SDKBase;
using System.Linq;
using System.Reflection;
using nadena.dev.modular_avatar.core;
using nadena.dev.modular_avatar.core.editor;
using VRC.SDK3.Dynamics.PhysBone.Components;
using System.IO;

namespace WataOfuton.Tools.ClothTransformApplier
{
    public class ClothTransformApplier : MonoBehaviour, IEditorOnly
    {
        [System.Serializable]
        public class BlendShapeSet
        {
            public string name;
            public float value;
            public BlendShapeSet(string name, float value)
            {
                this.name = name;
                this.value = value;
            }
        }

        [System.Serializable]
        public class BoneList
        {
            public string name;
            public int num;
            public Component[] components;
            public BoneList(string name, int num, Component[] components)
            {
                this.name = name;
                this.num = num;
                this.components = components;
            }
        }

        public class TransformDiff
        {
            public Vector3 pos; // LocalSpace
            public Quaternion rot; // WorldSpace
        }

        [System.Serializable]
        public class TransformGroup
        {
            public string Name = "New Preset";
            public GameObject presetPrefab;
            public bool isFoldoutOpen;
            public List<BlendShapeSet> blendShapeSet;
            public bool isDiffMode;
            public int lineCount;
        }

        [SerializeField] public List<TransformGroup> transformGroups = new List<TransformGroup>();
        [SerializeField] public List<string> skipComponentList = new List<string>() { "physbone" };
        [SerializeField] public bool isUseMergeArmature = true;
        [SerializeField] public bool isRemoveMeshSettings = true;
        [SerializeField] public bool isUseCopyScaleAdjuster = false;
        [SerializeField] public bool isRenameBreast;
        [SerializeField] public Transform breastLeft;
        [SerializeField] public Transform breastRight;
        [SerializeField] public bool isFixScaleAdjuster;
        private Vector3 armatureScale;
        private string prefix;
        private string suffix;
        private bool diffMode;


        /// <summary>
        /// 外部スクリプトから呼び出す API
        /// <summary>
        public static void ApplyClothWithAlterith(ClothTransformApplier applier, int targetIndex, Transform[] clothes)
        {
            TransformGroup group = applier.transformGroups[targetIndex];

            // Undo 対応
            Undo.RecordObject(applier, "Apply Cloth Alterith");

            applier.ApplyClothSettings(group, clothes);
        }

        /// <summary>
        /// ClothTransformApplier の設定状況をチェックする
        /// <summary>
        public static string[] CheckCTASettings(ClothTransformApplier applier, ref int targetIndex)
        {
            List<string> errors = new List<string>();
            if (applier == null)
            {
                errors.Add("[ClothTransformApplier] ClothTransformApplier not found");
                return errors.ToArray();
            }
            if (applier.transformGroups == null || applier.transformGroups.Count == 0)
            {
                errors.Add("[ClothTransformApplier] PresetGroups is Empty");
                return errors.ToArray();
            }
            if (applier.transformGroups[targetIndex].presetPrefab == null)
            {
                errors.Add("[ClothTransformApplier] PresetPrefab is Empty");
            }

            return errors.ToArray();
        }



        public void ApplyClothSettings(TransformGroup transformGroup, Transform[] clothes)
        {
            if (transformGroup.presetPrefab == null)
            {
                Debug.LogWarning("[Cloth Transform Applier] No preset has been assigned.");
                return;
            }

            if (transformGroup.isDiffMode)
            {
                diffMode = PrefabUtility.GetCorrespondingObjectFromSource(transformGroup.presetPrefab) != null;
                if (diffMode == false) Debug.Log("[Cloth Transform Applier] No difference information found in the preset.");
            }
            else
            {
                diffMode = false;
            }

            // 自前で処理するときは clothes が null で渡される
            // Alterith からは 衣装が渡される
            if (clothes == null || clothes.Length == 0)
            {
                clothes = new Transform[this.transform.childCount];
                for (int i = 0; i < clothes.Length; i++)
                {
                    clothes[i] = this.transform.GetChild(i);
                }
            }
            if (clothes == null || clothes.Length == 0)
            {
                Debug.LogWarning("[Cloth Transform Applier] No Clothes Detected.");
                return;
            }

            var targetBoneList = ReferenceBoneList.TargetBoneList();
            var boneList = new List<BoneList>();
            CollectBones(transformGroup.presetPrefab.transform, boneList, targetBoneList, false, "", "");

            // Numeira.CopyScaleAdjuster をインポートしている場合、それを使用する.
            // https://github.com/Rerigferl/modular-avatar-copy-scale-adjuster
            // Copyright (c) 2024 Rinna Koharu
            // Licensed under the MIT License
            bool isImportCopyScaleAdjuster = false;
            Assembly targetAssembly = null;
            MethodInfo methodA = null;
            if (isUseCopyScaleAdjuster)
            {
                try // アセンブリのロードを試みる
                {
                    targetAssembly = Assembly.Load("numeira.modular-avatar-copy-scale-adjuster.editor");
                }
                catch (FileNotFoundException)
                {
                    Debug.LogWarning("[Cloth Transform Applier] The specified assembly 'numeira.modular-avatar-copy-scale-adjuster.editor' was not found.");
                }
                catch (Exception ex)
                {
                    Debug.LogError("[Cloth Transform Applier] An unexpected error occurred while loading the assembly: " + ex.Message);
                }
                if (targetAssembly != null)
                {
                    // "Numeira.CopyScaleAdjuster" クラスの型を取得
                    Type targetType = targetAssembly.GetType("Numeira.CopyScaleAdjuster");
                    if (targetType != null)
                    {
                        // 存在する場合は、その関数を実行
                        methodA = targetType.GetMethod("Run", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        if (methodA != null)
                        {
                            isImportCopyScaleAdjuster = true;
                        }
                    }
                }
            }

            for (int i = 0; i < clothes.Length; i++)
            {
                var cloth = clothes[i];
                Transform armature = null;

                Undo.RegisterCompleteObjectUndo(cloth.gameObject, "Cloth Transform Applier" + i.ToString());

                if (isUseMergeArmature)
                {
                    SetupOutfit.SetupOutfitUI(cloth.gameObject);

                    var merge = cloth.GetComponentInChildren<ModularAvatarMergeArmature>();
                    if (merge == null) break;

                    if (isImportCopyScaleAdjuster)
                    {
                        object[] parameters = { merge };
                        methodA.Invoke(null, parameters); // 処理を実行
                        // Debug.Log("[Cloth Transform Applier] Numeira.CopyScaleAdjuster.");
                    }

                    prefix = merge.prefix;
                    suffix = merge.suffix;
                    armature = merge.transform;

                    if (isRemoveMeshSettings)
                    {
                        // MA Mesh Setting が生成されるが不要な場合は削除する.
                        var MAMeshSettings = cloth.GetComponent<ModularAvatarMeshSettings>();
                        if (MAMeshSettings != null)
                            DestroyImmediate(MAMeshSettings);
                    }
                }

                cloth.localPosition = transformGroup.presetPrefab.transform.position;
                cloth.localScale = transformGroup.presetPrefab.transform.localScale;

                // SetupOutfit を実行しない場合に取得をチャレンジ
                if (armature == null)
                {
                    armature = FindArmatureInChildren(cloth);
                }
                // Armature で Scale を調整している衣装の対応
                // ex:FBX出力で Armature の Scale が100とかになっているときの対応...
                if (armature != null)
                {
                    armatureScale = armature.localScale;
                    armatureScale.x = 1f / armatureScale.x;
                    armatureScale.y = 1f / armatureScale.y;
                    armatureScale.z = 1f / armatureScale.z;
                }
                else
                {
                    armatureScale = Vector3.one;
                }

                if (boneList == null || boneList.Count == 0)
                {
                    if (String.IsNullOrEmpty(prefix)) prefix = "bone_";

                    CollectBones(transformGroup.presetPrefab.transform, boneList, targetBoneList, true, prefix, suffix);
                    if (boneList == null || boneList.Count == 0)
                    {
                        Debug.LogWarning("[Cloth Transform Applier] BoneList is Empty in spite of after SetupOutfit.");
                        continue;
                    }
                }

                // for (int k = 0; k < boneList.Count; k++)
                //     Debug.Log($"[CTA Debug] boneList BoneName : '{boneList[k].name}' // ID : {k}");
                // return;

                SetBoneComponents(cloth, boneList, targetBoneList, cloth, armature);
                ApplyBlendShapeValues(cloth, transformGroup.blendShapeSet);
            }

            Debug.Log("[Cloth Transform Applier] Apply Cloth Transform.");
        }


        public static void CollectBones(Transform _parent, List<BoneList> _boneList, string[][] targetBoneList, bool useTrim, string prefix, string suffix)
        {
            if (_parent == null) return;
            if (_parent.gameObject.CompareTag("EditorOnly")) return;

            string boneName = _parent.name;
            if (useTrim)
            {
                boneName = TrimAffix(boneName, prefix, suffix);
            }

            // 自分自身をチェック
            int num = ReferenceBoneList.IsMatchBoneNameID(boneName, targetBoneList);
            if (num >= 0)
            {
                _boneList.Add(new BoneList(_parent.name, num, _parent.GetComponents<Component>()));
            }

            if (_parent.childCount == 0) return;

            // 子オブジェクトを再帰的に処理
            foreach (Transform child in _parent)
            {
                CollectBones(child, _boneList, targetBoneList, useTrim, prefix, suffix);
            }
        }

        private static Transform FindArmatureInChildren(Transform parent)
        {
            foreach (Transform child in parent)
            {
                // 1階層目の子オブジェクトを探索
                if (ReferenceBoneList.IsMatchBoneName(child.name, ReferenceBoneList.Armature[0]))
                {
                    return child;
                }
                // 1階層目の子オブジェクトを探索
                foreach (Transform grandChild in child)
                {
                    // 2階層目の子オブジェクト（孫オブジェクト）を探索
                    if (ReferenceBoneList.IsMatchBoneName(grandChild.name, ReferenceBoneList.Armature[0]))
                    {
                        return grandChild;
                    }
                }
            }

            return null;
        }

        private void SetBoneComponents(Transform _parent, List<BoneList> _boneList, string[][] targetBoneList, Transform cloth, Transform armature)
        {
            if (_parent == null) return;

            string boneName = TrimAffix(_parent.name, prefix, suffix);

            // 自分自身をチェック
            foreach (var bone in _boneList)
            {
                if (ReferenceBoneList.IsMatchBoneName(boneName, targetBoneList[bone.num]))
                {
                    // Debug.Log($"[CTA Debug] SetBoneComponents ::: Bone: '{bone.name}' // Pattern: '{boneName}' // Num:'{bone.num}'");

                    if (isRenameBreast)
                    {
                        if (ReferenceBoneList.IsMatchBoneNameID(boneName, ReferenceBoneList.breastR) >= 0)
                        {
                            CheckBreast(_parent, breastRight, false);
                        }
                        else if (ReferenceBoneList.IsMatchBoneNameID(boneName, ReferenceBoneList.breastL) >= 0)
                        {
                            CheckBreast(_parent, breastLeft, true);
                        }
                    }

                    foreach (var component in bone.components)
                    {
                        CopyComponent(component, _parent, armatureScale, skipComponentList, isFixScaleAdjuster, diffMode, cloth, armature);
                    }
                    break;
                }
            }

            if (_parent.childCount == 0) return;

            // 子オブジェクトを再帰的に処理
            foreach (Transform child in _parent)
            {
                SetBoneComponents(child, _boneList, targetBoneList, cloth, armature);
            }
        }

        private static void CheckBreast(Transform bone, Transform breast, bool isLeft)
        {
            if (breast == null)
            {
                string l = isLeft ? "Left" : "Right";
                Debug.LogWarning($"[Cloth Transform Applier] {l} Breast Bone is Empty.");
                return;
            }

            if (bone.name == breast.name) return;

            bone.name = breast.name;
            if (bone.parent.name != breast.parent.name) // 胸ボーンが同じ階層(Chestの子)にない場合
            {
                // Set BoneProxy
                Undo.AddComponent<ModularAvatarBoneProxy>(bone.gameObject);
                var bp = bone.gameObject.GetComponent<ModularAvatarBoneProxy>();
                bp.target = breast;
                bp.attachmentMode = BoneProxyAttachmentMode.AsChildKeepWorldPose;
                bp.boneReference = HumanBodyBones.Chest;
                bp.subPath = breast.name;

                var physbone = bone.parent.GetComponent<VRCPhysBone>();
                if (physbone != null)
                    physbone.enabled = false;
            }
        }

        private static void CopyComponent(Component source, Transform target, Vector3 armatureScale, List<string> skipList, bool isFixScaleAdjuster, bool diffMode,
                                          Transform sourceRoot, Transform targetRoot)
        {
            // Transform をコピー
            if (source is Transform sourceTransform)
            {
                Transform targetTransform = target.transform;

                if (diffMode)
                {
                    // https://light11.hatenadiary.com/entry/2019/04/18/202742
                    var origTransform = PrefabUtility.GetCorrespondingObjectFromSource(sourceTransform);
                    TransformDiff diff = new TransformDiff
                    {
                        pos = sourceTransform.localPosition - origTransform.localPosition,
                        rot = sourceTransform.rotation * Quaternion.Inverse(origTransform.rotation),
                    };

                    targetTransform.localPosition += diff.pos;
                    targetTransform.localPosition = Vector3.Scale(targetTransform.localPosition, armatureScale);
                    targetTransform.rotation = diff.rot * targetTransform.rotation;
                    targetTransform.localScale = sourceTransform.localScale;
                }
                else
                {
                    if (ReferenceBoneList.IsMatchBoneName(targetTransform.name, ReferenceBoneList.humanoidBoneList[0]) &&
                        Mathf.Abs(targetTransform.localEulerAngles.x - sourceTransform.localEulerAngles.x) > 70f)
                    {
                        var tp = Vector3.Scale(sourceTransform.localPosition, armatureScale);
                        (tp.y, tp.z) = (tp.z, tp.y);
                        targetTransform.localPosition = tp;

                        var tr = sourceTransform.localEulerAngles;
                        tr = new Vector3(targetTransform.localEulerAngles.x, tr.z, tr.y);
                        targetTransform.localEulerAngles = tr;

                        var ts = sourceTransform.localScale;
                        (ts.y, ts.z) = (ts.z, ts.y);
                        targetTransform.localScale = ts;
                    }
                    else
                    {
                        targetTransform.localPosition = Vector3.Scale(sourceTransform.localPosition, armatureScale);
                        targetTransform.localRotation = sourceTransform.localRotation;
                        targetTransform.localScale = sourceTransform.localScale;
                    }
                }
                return;
            }

            // Skip List をスキップ
            var type = source.GetType();
            foreach (var skip in skipList)
            {
                if (type.Name.IndexOf(skip, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Debug.Log($"[Cloth Transform Applier] Skip Copy '{type.Name}' in '{target.name}'.");
                    return;
                }
            }

            // MA ScaleAdjuster
            if (isFixScaleAdjuster)
            {
                if (source is ModularAvatarScaleAdjuster scaleAdjuster)
                {
                    ModularAvatarScaleAdjuster targetComponent = target.GetComponent<ModularAvatarScaleAdjuster>();

                    if (targetComponent != null) // すでにコンポーネントが存在する場合
                    {
                        // targetのScaleにsourceのScaleを乗算
                        targetComponent.Scale = Vector3.Scale(targetComponent.Scale, scaleAdjuster.Scale);
                    }
                    else
                    {
                        // コンポーネントがない場合、新規に追加してから値をコピー
                        targetComponent = Undo.AddComponent<ModularAvatarScaleAdjuster>(target.gameObject);
                        targetComponent.Scale = scaleAdjuster.Scale;
                    }
                    return;
                }
            }

            // Transform 以外のComponent をコピー
            if (target.GetComponent(type) == null)
            {
                Undo.AddComponent(target.gameObject, type); // AddComponentの代わりにUndo.AddComponentを使用
            }
            Component copy = target.GetComponent(type);

            //シリアライズ済みフィールドを丸ごとコピー
            EditorUtility.CopySerialized(source, copy);

            // Object 参照をターゲット階層に置き換え
            var so = new SerializedObject(copy);
            var prop = so.GetIterator();

            while (prop.Next(true))
            {
                if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;

                var obj = prop.objectReferenceValue;
                if (obj == null) continue;

                var newObj = RemapSingleReference(obj, sourceRoot, targetRoot);
                if (newObj != obj)
                    prop.objectReferenceValue = newObj;
            }
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// 参照がソースアバター内に存在する場合、ターゲット側の同一パス
        /// 　— 見つからなければ「同名」オブジェクト — に置き換える。
        /// 見つからなければ元の参照を返す。
        /// </summary>
        public static UnityEngine.Object RemapSingleReference(UnityEngine.Object srcObj, Transform sourceRoot, Transform targetRoot)
        {
            if (srcObj == null) return null;

            // Transform / GameObject / Component 以外（マテリアル等）はそのまま返す
            if (!(srcObj is Component) && !(srcObj is GameObject) && !(srcObj is Transform))
                return srcObj;

            // 参照先 Transform を取得
            Transform srcTr = srcObj switch
            {
                Transform t => t,
                GameObject go => go.transform,
                Component c => c.transform,
                _ => null
            };
            if (srcTr == null) return srcObj;

            // 1) 相対パス一致で検索
            string relPath = AnimationUtility.CalculateTransformPath(srcTr, sourceRoot);
            Transform dstTr = targetRoot.Find(relPath);

            // 2) 見つからなければ同名検索
            if (dstTr == null)
                dstTr = FindTransformByName(targetRoot, srcTr.name);   // 深さ優先

            if (dstTr == null) return srcObj;                          // それでも無ければ諦める

            // 型に応じて返却
            return srcObj switch
            {
                Transform _ => dstTr,
                GameObject _ => dstTr.gameObject,
                Component c => dstTr.GetComponent(c.GetType()) ?? srcObj,
                _ => srcObj
            };
        }

        /// <summary>
        /// targetRoot 以下を深さ優先で走査し、名前が一致する Transform を返す
        /// </summary>
        private static Transform FindTransformByName(Transform root, string name)
        {
            if (root.name == name) return root;

            foreach (Transform child in root)
            {
                var found = FindTransformByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static void ApplyBlendShapeValues(Transform target, List<BlendShapeSet> blendShapeSet)
        {
            if (blendShapeSet == null || blendShapeSet.Count == 0)
            {
                return;
            }

            // 対象Transformのすべての子孫からSkinnedMeshRendererを取得
            SkinnedMeshRenderer[] skinnedMeshRenderers = target.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (var smr in skinnedMeshRenderers)
            {
                if (smr.sharedMesh != null)
                {
                    foreach (var set in blendShapeSet)
                    {
                        int blendShapeIndex = GetBlendShapeIndexIgnoreCase(smr.sharedMesh, set.name); // 大文字小文字を無視して BlendShape を検索

                        // BlendShapeが存在する場合にのみ値を設定
                        if (blendShapeIndex >= 0)
                        {
                            smr.SetBlendShapeWeight(blendShapeIndex, set.value);
                        }
                    }
                }
            }
        }
        private static int GetBlendShapeIndexIgnoreCase(Mesh mesh, string blendShapeName)
        {
            int blendShapeCount = mesh.blendShapeCount;
            for (int i = 0; i < blendShapeCount; i++)
            {
                string currentBlendShapeName = mesh.GetBlendShapeName(i);

                if (string.Equals(currentBlendShapeName, blendShapeName, StringComparison.OrdinalIgnoreCase)) // 大文字小文字を無視して比較
                {
                    return i;
                }
            }
            return -1;
        }



        /**************************************************************************************************************/
        public static string TrimAffix(string name, string prefix, string suffix)
        {
            string boneName = name;

            if (!string.IsNullOrEmpty(prefix) && boneName.StartsWith(prefix))
            {
                boneName = boneName.Substring(prefix.Length); // prefixを除去
            }
            if (!string.IsNullOrEmpty(suffix) && boneName.EndsWith(suffix))
            {
                boneName = boneName.Substring(0, boneName.Length - suffix.Length); // suffixを除去
            }

            return boneName;
        }

        public static Vector3 DivideVector3(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
        }

    }
}
#endif