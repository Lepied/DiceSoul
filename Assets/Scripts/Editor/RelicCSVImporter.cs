#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;

// CSV 파일에서 RelicData ScriptableObject를 자동 생성하는 에디터 스크립트
public class RelicCSVImporter : EditorWindow
{
    private string csvPath = "Assets/Data/Relic_Plan.csv";
    private string outputFolder = "Assets/Resources/Relics";
    private string iconFolder = "Assets/Resources/RelicIcons";
    
    [MenuItem("Tools/DiceSoul/CSV에서 유물 생성")]
    public static void ShowWindow()
    {
        GetWindow<RelicCSVImporter>("유물 CSV 임포터");
    }
    
    void OnGUI()
    {
        GUILayout.Label("CSV → ScriptableObject 변환", EditorStyles.boldLabel);
        
        EditorGUILayout.Space(10);
        
        csvPath = EditorGUILayout.TextField("CSV 파일 경로", csvPath);
        outputFolder = EditorGUILayout.TextField("출력 폴더", outputFolder);
        iconFolder = EditorGUILayout.TextField("아이콘 폴더", iconFolder);
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "CSV 형식:\n" +
            "ID,이름,등급,획득경로,효과,최대개수,비고,구현여부\n\n" +
            "아이콘 파일명: {RelicID}.png", 
            MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("CSV에서 유물 생성", GUILayout.Height(40)))
        {
            ImportFromCSV();
        }
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("기존 유물 모두 삭제", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("확인", "기존 유물 ScriptableObject를 모두 삭제하시겠습니까?", "삭제", "취소"))
            {
                DeleteAllRelics();
            }
        }
    }
    
    private void ImportFromCSV()
    {
        if (!File.Exists(csvPath))
        {
            EditorUtility.DisplayDialog("오류", $"CSV 파일을 찾을 수 없습니다:\n{csvPath}", "확인");
            return;
        }
        
        // 출력 폴더 생성
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }
        
        string[] lines = File.ReadAllLines(csvPath);
        int created = 0;
        int updated = 0;
        int failed = 0;
        
        // 첫 줄은 헤더, 스킵
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            try
            {
                string[] values = ParseCSVLine(line);
                if (values.Length < 8)
                {
                    Debug.LogWarning($"라인 {i + 1}: 컬럼 부족 - {line}");
                    failed++;
                    continue;
                }
                
                string relicID = values[0].Trim();
                string relicName = values[1].Trim();
                string rarityStr = values[2].Trim();
                string dropPoolStr = values[3].Trim();
                string description = values[4].Trim().Replace(";", ""); // 세미콜론 제거
                string maxCountStr = values[5].Trim();
                string categoryStr = values[6].Trim();
                // values[7] = 구현여부
                
                // 새 컬럼들
                string effectTypeStr = values.Length > 8 ? values[8].Trim() : "";
                string intValueStr = values.Length > 9 ? values[9].Trim() : "0";
                string floatValueStr = values.Length > 10 ? values[10].Trim() : "0";
                string stringValue = values.Length > 11 ? values[11].Trim() : "";
                
                // 기존 에셋 찾기 또는 새로 생성
                string assetPath = $"{outputFolder}/{relicID}.asset";
                RelicData relicData = AssetDatabase.LoadAssetAtPath<RelicData>(assetPath);
                
                bool isNew = relicData == null;
                if (isNew)
                {
                    relicData = ScriptableObject.CreateInstance<RelicData>();
                }
                
                // 데이터 설정
                relicData.relicID = relicID;
                relicData.relicName = relicName;
                relicData.description = description;
                relicData.rarity = ParseRarity(rarityStr);
                relicData.dropPool = ParseDropPool(dropPoolStr);
                relicData.category = ParseCategory(categoryStr);
                relicData.maxCount = ParseMaxCount(maxCountStr);
                
                // 효과 값 설정 (CSV에서 직접 파싱)
                if (!string.IsNullOrEmpty(effectTypeStr))
                {
                    relicData.effectType = ParseEffectType(effectTypeStr);
                }
                else
                {
                    relicData.effectType = InferEffectType(relicID, description);
                }
                
                // IntValue, FloatValue, StringValue 파싱
                if (int.TryParse(intValueStr, out int intVal))
                    relicData.intValue = intVal;
                if (float.TryParse(floatValueStr, System.Globalization.NumberStyles.Float, 
                    System.Globalization.CultureInfo.InvariantCulture, out float floatVal))
                    relicData.floatValue = floatVal;
                relicData.stringValue = stringValue;
                
                relicData.triggerTiming = InferTriggerTiming(relicData.effectType);
                
                // 아이콘 로드
                string iconPath = $"{iconFolder}/{relicID}.png";
                Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                if (icon != null)
                {
                    relicData.icon = icon;
                }
                
                // 저장
                if (isNew)
                {
                    AssetDatabase.CreateAsset(relicData, assetPath);
                    created++;
                }
                else
                {
                    EditorUtility.SetDirty(relicData);
                    updated++;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"라인 {i + 1} 처리 오류: {e.Message}\n{lines[i]}");
                failed++;
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("완료", 
            $"유물 임포트 완료!\n\n생성: {created}개\n업데이트: {updated}개\n실패: {failed}개", 
            "확인");
    }
    
    private string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string current = "";
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }
        result.Add(current);
        
        return result.ToArray();
    }
    
    private RelicRarity ParseRarity(string str)
    {
        return str.ToLower() switch
        {
            "common" => RelicRarity.Common,
            "uncommon" => RelicRarity.Uncommon,
            "rare" => RelicRarity.Rare,
            "epic" => RelicRarity.Epic,
            _ => RelicRarity.Common
        };
    }
    
    private RelicDropPool ParseDropPool(string str)
    {
        return str switch
        {
            "WaveReward" => RelicDropPool.WaveReward,
            "ShopOnly" => RelicDropPool.ShopOnly,
            "MaintenanceReward" => RelicDropPool.MaintenanceReward,
            _ => RelicDropPool.WaveReward
        };
    }
    
    private RelicCategory ParseCategory(string str)
    {
        return str switch
        {
            "유틸리티" => RelicCategory.Utility,
            "캐릭터 스탯" => RelicCategory.CharacterStat,
            "족보 특화" => RelicCategory.HandSpecific,
            "주사위 관련" => RelicCategory.DiceRelated,
            "경제 관련" => RelicCategory.Economy,
            "생존 유틸리티" => RelicCategory.Survival,
            _ => RelicCategory.Utility
        };
    }
    
    private int ParseMaxCount(string str)
    {
        if (str == "무제한" || str == "0") return 0;
        if (int.TryParse(str, out int result)) return result;
        return 1;
    }
    
    // CSV의 EffectType 문자열을 enum으로 변환
    private RelicEffectType ParseEffectType(string str)
    {
        if (System.Enum.TryParse<RelicEffectType>(str, true, out var result))
        {
            return result;
        }
        return RelicEffectType.None;
    }

    // RelicID를 기반으로 EffectType 추론 (fallback용)

    private RelicEffectType InferEffectType(string relicID, string description)
    {
        // 기존 구현된 유물들 매핑
        return relicID switch
        {
            "RLC_CLOVER" => RelicEffectType.AddMaxRolls,
            "RLC_WHETSTONE" => RelicEffectType.AddBaseDamage,
            "RLC_RUSTY_GEAR" => RelicEffectType.HandGoldAdd,
            "RLC_EXTRA_DICE" => RelicEffectType.AddDice,
            "RLC_TINY_DICE" => RelicEffectType.AddDice,
            "RLC_FOUNDATION" => RelicEffectType.HandDamageAdd,
            "RLC_GOLD_DICE" => RelicEffectType.AddGoldMultiplier,
            "RLC_GLASS_CANNON" => RelicEffectType.AddBaseDamage,
            "RLC_PLUTOCRACY" => RelicEffectType.DynamicDamage_Gold,
            "RLC_BLOODLUST" => RelicEffectType.DynamicDamage_LostHealth,
            "RLC_FOCUS" => RelicEffectType.AddBaseDamage,
            "RLC_ALCHEMY" => RelicEffectType.ModifyDiceValue,
            "RLC_LODESTONE" => RelicEffectType.RerollOdds,
            "RLC_TANZANITE" => RelicEffectType.RerollEvens,
            "RLC_FEATHER" => RelicEffectType.RerollSixes,
            "RLC_COUNTERWEIGHT" => RelicEffectType.BonusOnLowD20,
            "RLC_PERFECTIONIST" => RelicEffectType.DisableHand,
            "RLC_SWORD_BOOK" => RelicEffectType.HandDamageAdd,
            "RLC_SPIKE_GLOVE" => RelicEffectType.HandDamageAdd,
            "RLC_WHITEBOOK" => RelicEffectType.HandGoldMultiplier,
            "RLC_DARKBOOK" => RelicEffectType.HandGoldMultiplier,
            "RLC_GEM_CROWN" => RelicEffectType.HandGoldMultiplier,
            "RLC_SQUARE_PEG" => RelicEffectType.HandDamageAdd,
            "RLC_TRIPOD" => RelicEffectType.HandGoldMultiplier,
            "RLC_HOUSE" => RelicEffectType.HandDamageAdd,
            "RLC_ROYAL_SEAL" => RelicEffectType.HandGoldMultiplier,
            "RLC_HEAVY_DICE" => RelicEffectType.HandDamageAdd,
            "RLC_BUSINESS_CARD" => RelicEffectType.RollCountBonus,
            // 새로 추가할 유물들
            "RLC_VAMPIRE_FANG" => RelicEffectType.HealOnHand,
            "RLC_REGENERATION" => RelicEffectType.HealOnRoll,
            "RLC_PHOENIX_FEATHER" => RelicEffectType.ReviveOnDeath,
            "RLC_SMALL_SHIELD" => RelicEffectType.DamageImmuneLowHP,
            "RLC_SWIFT_HANDS" => RelicEffectType.FreeRollOnZero,
            "RLC_DICE_CUP" => RelicEffectType.FixDiceBeforeRoll,
            "RLC_DOUBLE_DICE" => RelicEffectType.DoubleDiceValue,
            "RLC_FATE_DICE" => RelicEffectType.SetAllToMax,
            "RLC_LUCKY_CHARM" => RelicEffectType.ShopDiscount,
            "RLC_MERCHANT_CARD" => RelicEffectType.ShopRefreshFreeze,
            "RLC_TIME_RIFT" => RelicEffectType.SaveRollChance,
            "RLC_HOURGLASS" => RelicEffectType.DynamicDamage_LowRolls,
            _ => RelicEffectType.None
        };
    }
    
    private RelicTriggerTiming InferTriggerTiming(RelicEffectType effectType)
    {
        return effectType switch
        {
            RelicEffectType.AddMaxRolls => RelicTriggerTiming.Passive,
            RelicEffectType.AddBaseDamage => RelicTriggerTiming.Passive,
            RelicEffectType.AddBaseGold => RelicTriggerTiming.Passive,
            RelicEffectType.AddGoldMultiplier => RelicTriggerTiming.Passive,
            RelicEffectType.AddDice => RelicTriggerTiming.OnAcquire,
            RelicEffectType.RemoveDice => RelicTriggerTiming.OnAcquire,
            RelicEffectType.ModifyDiceValue => RelicTriggerTiming.OnRoll,
            RelicEffectType.RerollOdds => RelicTriggerTiming.OnRoll,
            RelicEffectType.RerollEvens => RelicTriggerTiming.OnRoll,
            RelicEffectType.RerollSixes => RelicTriggerTiming.OnRoll,
            RelicEffectType.HandDamageAdd => RelicTriggerTiming.OnBeforeAttack,
            RelicEffectType.HandGoldMultiplier => RelicTriggerTiming.OnBeforeAttack,
            RelicEffectType.HealOnHand => RelicTriggerTiming.OnHandComplete,
            RelicEffectType.ReviveOnDeath => RelicTriggerTiming.OnPlayerDeath,
            RelicEffectType.DamageImmuneLowHP => RelicTriggerTiming.OnTakeDamage,
            RelicEffectType.FixDiceBeforeRoll => RelicTriggerTiming.Manual,
            RelicEffectType.DoubleDiceValue => RelicTriggerTiming.Manual,
            RelicEffectType.SetAllToMax => RelicTriggerTiming.Manual,
            _ => RelicTriggerTiming.Passive
        };
    }
    
    private void DeleteAllRelics()
    {
        if (!Directory.Exists(outputFolder)) return;
        
        string[] files = Directory.GetFiles(outputFolder, "*.asset");
        foreach (string file in files)
        {
            AssetDatabase.DeleteAsset(file);
        }
        
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료", $"{files.Length}개 유물 삭제됨", "확인");
    }
}
#endif
