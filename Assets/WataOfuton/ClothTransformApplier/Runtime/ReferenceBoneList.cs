#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace WataOfuton.Tools.ClothTransformApplier
{
    public class ReferenceBoneList
    {
        public static bool IsMatchBoneName(string boneName, string[] boneList)
        {
            string pattern = ConvertToRegexPattern(boneName);
            foreach (var bone in boneList)
            {
                if (Regex.IsMatch(bone, pattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public static int IsMatchBoneNameID(string boneName, string[][] boneList)
        {
            string pattern = ConvertToRegexPattern(boneName);
            for (int i = 0; i < boneList.Length; i++)
            {
                foreach (var bone in boneList[i])
                {
                    if (Regex.IsMatch(bone, pattern, RegexOptions.IgnoreCase))
                    {
                        // UnityEngine.Debug.Log($"[CTA Debug] Bone: '{bone}' // Pattern: '{pattern}' // Num:'{i}'");
                        return i;
                    }
                }
            }
            return -1;
        }

        private static string ConvertToRegexPattern(string boneName)
        {
            string escapedBoneName = Regex.Escape(boneName).Replace("_", "[._]").Replace("\\.", "[._]");
            return $"^{escapedBoneName}$";
        }


        // Source 1: https://github.com/bdunderscore/modular-avatar/blob/main/Editor/HeuristicBoneMapper.cs
        //  - Copyright (c) 2022 bd_
        //  - Licensed under the MIT License
        //
        // Source 2: https://github.com/HhotateA/AvatarModifyTools/blob/d8ae75fed8577707253d6b63a64d6053eebbe78b/Assets/HhotateA/AvatarModifyTool/Editor/EnvironmentVariable.cs#L81-L139
        //  - Copyright (c) 2021 @HhotateA_xR
        //  - Licensed under the MIT License
        //
        // Source 3: https://github.com/Azukimochi/BoneRenamer/blob/6ec12b848830f467e35ddf7ff105aaa72be02908/BoneNames.xml
        //  - Copyright (c) 2023 Azukimochi
        //  - Licensed under the MIT License
        public static readonly string[][] humanoidBoneList = new[]
        {
            // boneNamePatterns
            new[] {"Hips", "Hip", "pelvis"},
            new[]
            {
                "LeftUpperLeg", "UpperLeg_Left", "UpperLeg_L", "Leg_Left", /*"Leg_L",*/ "ULeg_L", "Left leg", "LeftUpLeg", "UpLeg.L", "Thigh_L",
                // Add
                "Upper_leg.L"
            },
            new[]
            {
                "RightUpperLeg", "UpperLeg_Right", "UpperLeg_R", "Leg_Right", /*"Leg_R",*/ "ULeg_R", "Right leg", "RightUpLeg", "UpLeg.R", "Thigh_R",
                // Add
                "Upper_leg.R"
            },
            new[]
            {
                "LeftLowerLeg", "LowerLeg_Left", "LowerLeg_L", "Knee_Left", "Knee_L", "LLeg_L", "Left knee", "LeftLeg", "leg_L", "shin.L",
                // Add
                "Lower_leg.L"
            },
            new[]
            {
                "RightLowerLeg", "LowerLeg_Right", "LowerLeg_R", "Knee_Right", "Knee_R", "LLeg_R", "Right knee", "RightLeg", "leg_R", "shin.R",
                // Add
                "Lower_leg.R"
            },

            new[] {"LeftFoot", "Foot_Left", "Foot_L", "Ankle_L", "Foot.L.001", "Left ankle", "heel.L", "heel"},
            new[] {"RightFoot", "Foot_Right", "Foot_R", "Ankle_R", "Foot.R.001", "Right ankle", "heel.R", "heel"},
            new[] {"Spine", "spine01"},
            new[] {"Chest", "Bust", "spine02", "upper_chest"},
            new[] {"Neck"},
            new[] {"Head"},
            new[] {"LeftShoulder", "Shoulder_Left", "Shoulder_L"},
            new[] {"RightShoulder", "Shoulder_Right", "Shoulder_R"},
            new[]
            {
                "LeftUpperArm", "UpperArm_Left", "UpperArm_L", "Arm_Left", "Arm_L", "UArm_L", "Left arm", "UpperLeftArm",
                // Add
                "Upper_arm.L"
            },
            new[]
            {
                "RightUpperArm", "UpperArm_Right", "UpperArm_R", "Arm_Right", "Arm_R", "UArm_R", "Right arm", "UpperRightArm",
                // Add
                "Upper_arm.R"
            },
            new[]
            {
                "LeftLowerArm", "LowerArm_Left", "LowerArm_L", "LArm_L", "Left elbow", "LeftForeArm", "Elbow_L", "forearm_L", "ForArm_L",
                // Add
                "Lower_arm.L"
            },
            new[]
            {
                "RightLowerArm", "LowerArm_Right", "LowerArm_R", "LArm_R", "Right elbow", "RightForeArm", "Elbow_R", "forearm_R", "ForArm_R",
                // Add
                "Lower_arm.R"
            },
            new[] {"LeftHand", "Hand_Left", "Hand_L", "Left wrist", "Wrist_L"},
            new[] {"RightHand", "Hand_Right", "Hand_R", "Right wrist", "Wrist_R"},
            new[]
            {
                "LeftToes", "Toes_Left", "Toe_Left", "ToeIK_L", "Toes_L", "Toe_L", "Foot.L.002", "Left Toe", "LeftToeBase"
            },
            new[]
            {
                "RightToes", "Toes_Right", "Toe_Right", "ToeIK_R", "Toes_R", "Toe_R", "Foot.R.002", "Right Toe", "RightToeBase"
            },
            new[] {"LeftEye", "Eye_Left", "Eye_L"},
            new[] {"RightEye", "Eye_Right", "Eye_R"},
            new[] {"Jaw"},
            new[]
            {
                "LeftThumbProximal", "ProximalThumb_Left", "ProximalThumb_L", "Thumb1_L", "ThumbFinger1_L",
                "LeftHandThumb1", "Thumb Proximal.L", "Thunb1_L", "finger01_01_L",
                // Add
                "ThumbProximal.L"
            },
            new[]
            {
                "LeftThumbIntermediate", "IntermediateThumb_Left", "IntermediateThumb_L", "Thumb2_L", "ThumbFinger2_L",
                "LeftHandThumb2", "Thumb Intermediate.L", "Thunb2_L", "finger01_02_L",
                // Add
                "ThumbIntermediate.L"
            },
            new[]
            {
                "LeftThumbDistal", "DistalThumb_Left", "DistalThumb_L", "Thumb3_L", "ThumbFinger3_L", "LeftHandThumb3",
                "Thumb Distal.L", "Thunb3_L", "finger01_03_L",
                // Add
                "ThumbDistal.L"
            },
            new[]
            {
                "LeftIndexProximal", "ProximalIndex_Left", "ProximalIndex_L", "Index1_L", "IndexFinger1_L",
                "LeftHandIndex1", "Index Proximal.L", "finger02_01_L", "f_index.01.L",
                // Add
                "IndexProximal.L"
            },
            new[]
            {
                "LeftIndexIntermediate", "IntermediateIndex_Left", "IntermediateIndex_L", "Index2_L", "IndexFinger2_L",
                "LeftHandIndex2", "Index Intermediate.L", "finger02_02_L", "f_index.02.L",
                // Add
                "IndexIntermediate.L"
            },
            new[]
            {
                "LeftIndexDistal", "DistalIndex_Left", "DistalIndex_L", "Index3_L", "IndexFinger3_L", "LeftHandIndex3",
                "Index Distal.L", "finger02_03_L", "f_index.03.L",
                // Add
                "IndexDistal.L"
            },
            new[]
            {
                "LeftMiddleProximal", "ProximalMiddle_Left", "ProximalMiddle_L", "Middle1_L", "MiddleFinger1_L",
                "LeftHandMiddle1", "Middle Proximal.L", "finger03_01_L", "f_middle.01.L",
                // Add
                "MiddleProximal.L"
            },
            new[]
            {
                "LeftMiddleIntermediate", "IntermediateMiddle_Left", "IntermediateMiddle_L", "Middle2_L",
                "MiddleFinger2_L", "LeftHandMiddle2", "Middle Intermediate.L", "finger03_02_L", "f_middle.02.L",
                // Add
                "MiddleIntermediate.L"
            },
            new[]
            {
                "LeftMiddleDistal", "DistalMiddle_Left", "DistalMiddle_L", "Middle3_L", "MiddleFinger3_L",
                "LeftHandMiddle3", "Middle Distal.L", "finger03_03_L", "f_middle.03.L",
                // Add
                "MiddleDistal.L"
            },
            new[]
            {
                "LeftRingProximal", "ProximalRing_Left", "ProximalRing_L", "Ring1_L", "RingFinger1_L", "LeftHandRing1",
                "Ring Proximal.L", "finger04_01_L", "f_ring.01.L",
                // Add
                "RingProximal.L"
            },
            new[]
            {
                "LeftRingIntermediate", "IntermediateRing_Left", "IntermediateRing_L", "Ring2_L", "RingFinger2_L",
                "LeftHandRing2", "Ring Intermediate.L", "finger04_02_L", "f_ring.02.L",
                // Add
                "RingIntermediate.L"
            },
            new[]
            {
                "LeftRingDistal", "DistalRing_Left", "DistalRing_L", "Ring3_L", "RingFinger3_L", "LeftHandRing3",
                "Ring Distal.L", "finger04_03_L", "f_ring.03.L",
                // Add
                "RingDistal.L"
            },
            new[]
            {
                "LeftLittleProximal", "ProximalLittle_Left", "ProximalLittle_L", "Little1_L", "LittleFinger1_L",
                "LeftHandPinky1", "Little Proximal.L", "finger05_01_L", "f_pinky.01.L",
                // Add
                "LittleProximal.L"
            },
            new[]
            {
                "LeftLittleIntermediate", "IntermediateLittle_Left", "IntermediateLittle_L", "Little2_L",
                "LittleFinger2_L", "LeftHandPinky2", "Little Intermediate.L", "finger05_02_L", "f_pinky.02.L",
                // Add
                "LittleIntermediate.L"
            },
            new[]
            {
                "LeftLittleDistal", "DistalLittle_Left", "DistalLittle_L", "Little3_L", "LittleFinger3_L",
                "LeftHandPinky3", "Little Distal.L", "finger05_03_L", "f_pinky.03.L",
                // Add
                "LittleDistal.L"
            },
            new[]
            {
                "RightThumbProximal", "ProximalThumb_Right", "ProximalThumb_R", "Thumb1_R", "ThumbFinger1_R",
                "RightHandThumb1", "Thumb Proximal.R", "Thunb1_R", "finger01_01_R",
                // Add
                "ThumbProximal.R"
            },
            new[]
            {
                "RightThumbIntermediate", "IntermediateThumb_Right", "IntermediateThumb_R", "Thumb2_R",
                "ThumbFinger2_R", "RightHandThumb2", "Thumb Intermediate.R", "Thunb2_R", "finger01_02_R",
                // Add
                "ThumbIntermediate.R"
            },
            new[]
            {
                "RightThumbDistal", "DistalThumb_Right", "DistalThumb_R", "Thumb3_R", "ThumbFinger3_R",
                "RightHandThumb3", "Thumb Distal.R", "Thunb3_R", "finger01_03_R",
                // Add
                "ThumbDistal.R"
            },
            new[]
            {
                "RightIndexProximal", "ProximalIndex_Right", "ProximalIndex_R", "Index1_R", "IndexFinger1_R",
                "RightHandIndex1", "Index Proximal.R", "finger02_01_R", "f_index.01.R",
                // Add
                "IndexProximal.R"
            },
            new[]
            {
                "RightIndexIntermediate", "IntermediateIndex_Right", "IntermediateIndex_R", "Index2_R",
                "IndexFinger2_R", "RightHandIndex2", "Index Intermediate.R", "finger02_02_R", "f_index.02.R",
                // Add
                "IndexIntermediate.R"
            },
            new[]
            {
                "RightIndexDistal", "DistalIndex_Right", "DistalIndex_R", "Index3_R", "IndexFinger3_R",
                "RightHandIndex3", "Index Distal.R", "finger02_03_R", "f_index.03.R",
                // Add
                "IndexDistal.R"
            },
            new[]
            {
                "RightMiddleProximal", "ProximalMiddle_Right", "ProximalMiddle_R", "Middle1_R", "MiddleFinger1_R",
                "RightHandMiddle1", "Middle Proximal.R", "finger03_01_R", "f_middle.01.R",
                // Add
                "MiddleProximal.R"
            },
            new[]
            {
                "RightMiddleIntermediate", "IntermediateMiddle_Right", "IntermediateMiddle_R", "Middle2_R",
                "MiddleFinger2_R", "RightHandMiddle2", "Middle Intermediate.R", "finger03_02_R", "f_middle.02.R",
                // Add
                "MiddleIntermediate.R"
            },
            new[]
            {
                "RightMiddleDistal", "DistalMiddle_Right", "DistalMiddle_R", "Middle3_R", "MiddleFinger3_R",
                "RightHandMiddle3", "Middle Distal.R", "finger03_03_R", "f_middle.03.R",
                // Add
                "MiddleDistal.R"
            },
            new[]
            {
                "RightRingProximal", "ProximalRing_Right", "ProximalRing_R", "Ring1_R", "RingFinger1_R",
                "RightHandRing1", "Ring Proximal.R", "finger04_01_R", "f_ring.01.R",
                // Add
                "RingProximal.R"
            },
            new[]
            {
                "RightRingIntermediate", "IntermediateRing_Right", "IntermediateRing_R", "Ring2_R", "RingFinger2_R",
                "RightHandRing2", "Ring Intermediate.R", "finger04_02_R", "f_ring.02.R",
                // Add
                "RingIntermediate.R"
            },
            new[]
            {
                "RightRingDistal", "DistalRing_Right", "DistalRing_R", "Ring3_R", "RingFinger3_R", "RightHandRing3",
                "Ring Distal.R", "finger04_03_R", "f_ring.03.R",
                // Add
                "RingDistal.R"
            },
            new[]
            {
                "RightLittleProximal", "ProximalLittle_Right", "ProximalLittle_R", "Little1_R", "LittleFinger1_R",
                "RightHandPinky1", "Little Proximal.R", "finger05_01_R", "f_pinky.01.R",
                // Add
                "LittleProximal.R"
            },
            new[]
            {
                "RightLittleIntermediate", "IntermediateLittle_Right", "IntermediateLittle_R", "Little2_R",
                "LittleFinger2_R", "RightHandPinky2", "Little Intermediate.R", "finger05_02_R", "f_pinky.02.R",
                // Add
                "LittleIntermediate.R"
            },
            new[]
            {
                "RightLittleDistal", "DistalLittle_Right", "DistalLittle_R", "Little3_R", "LittleFinger3_R",
                "RightHandPinky3", "Little Distal.R", "finger05_03_R", "f_pinky.03.R",
                // Add
                "LittleDistal.R"
            },
            new[] {"UpperChest", "UChest"},

            // しなのちゃん用
            new[] {"Butt.L"},
            new[] {"Butt.R"},
            new[] {"Lower_arm_support.L"},
            new[] {"Lower_arm_support.R"},
            new[] {"Upper_arm_support.L"},
            new[] {"Upper_arm_support.R"}
        };
        public static readonly string[][] breastL
                 = new[]{new[] {"Breast_L","Breasts_L","Breast_root_L","Breast_L_Root"}
        };
        public static readonly string[][] breastR
                 = new[]{new[] {"Breast_R","Breasts_R","Breast_root_R","Breast_R_Root"}
        };
        public static readonly string[][] Armature
                 = new[]{new[] {"Armature", "Armature_1", "root", "root_1"}
        };

        public static string[][] TargetBoneList()
        {
            return humanoidBoneList.Concat(breastL).Concat(breastR).ToArray();
        }
        public static string[][] TargetBoneListALL()
        {
            return TargetBoneList().Concat(Armature).ToArray();
        }

    }
}
#endif