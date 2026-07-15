using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;

namespace MahjongPrototype.Tests
{
    public sealed class ProductionYakuCatalogTests
    {
        private const string CatalogPath =
            "Assets/Scripts/MahjongPrototype/ScriptableObjects/YakuDefinitionCatalog.asset";
        private const string YakuDefinitionCatalogTypeName =
            "MahjongPrototype.Definitions.YakuDefinitionCatalog, Assembly-CSharp";

        [Test]
        public void ProductionCatalog_LoadsFromAssetPath()
        {
            Assert.That(
                LoadCatalog(),
                Is.Not.Null,
                $"Production yaku catalog was not found at {CatalogPath}.");
        }

        [Test]
        public void ProductionCatalog_DefinitionCountIsExpected()
        {
            Assert.That(
                LoadDefinitions().Count,
                Is.EqualTo(ExpectedDefinitions.Length),
                $"Production yaku catalog at {CatalogPath} must contain exactly {ExpectedDefinitions.Length} definitions.");
        }

        [Test]
        public void ProductionCatalog_DefinitionsDoNotContainNull()
        {
            var definitions = LoadDefinitions();
            var nullIndexes = definitions
                .Select((definition, index) => new { definition, index })
                .Where(item => item.definition == null)
                .Select(item => item.index)
                .ToArray();

            Assert.That(
                nullIndexes,
                Is.Empty,
                $"Production yaku catalog at {CatalogPath} contains null definitions at indexes: {string.Join(", ", nullIndexes)}.");
        }

        [Test]
        public void ProductionCatalog_DoesNotContainDuplicateKinds()
        {
            var duplicateSummaries = LoadDefinitions()
                .Where(definition => definition != null)
                .GroupBy(DefinitionKindName)
                .Where(group => group.Count() > 1)
                .Select(group => $"{group.Key}:{group.Count()}")
                .ToArray();

            Assert.That(
                duplicateSummaries,
                Is.Empty,
                $"Production yaku catalog at {CatalogPath} contains duplicate YakuKind entries: {string.Join(", ", duplicateSummaries)}.");
        }

        [Test]
        public void ProductionCatalog_KindsExactlyMatchExpectedImplementedKinds()
        {
            var actualKinds = LoadDefinitions()
                .Where(definition => definition != null)
                .Select(DefinitionKindName)
                .Distinct()
                .OrderBy(kind => kind)
                .ToArray();
            var expectedKinds = ExpectedDefinitions
                .Select(definition => definition.KindName)
                .OrderBy(kind => kind)
                .ToArray();
            var missingKinds = expectedKinds.Except(actualKinds).OrderBy(kind => kind).ToArray();
            var unexpectedKinds = actualKinds.Except(expectedKinds).OrderBy(kind => kind).ToArray();

            Assert.That(
                missingKinds,
                Is.Empty,
                $"Production yaku catalog at {CatalogPath} is missing expected kinds: {JoinKinds(missingKinds)}.");
            Assert.That(
                unexpectedKinds,
                Is.Empty,
                $"Production yaku catalog at {CatalogPath} contains unexpected kinds: {JoinKinds(unexpectedKinds)}.");
            Assert.That(
                actualKinds,
                Does.Not.Contain("None"),
                $"Production yaku catalog at {CatalogPath} must not register None.");
        }

        [TestCaseSource(nameof(ExpectedDefinitionCases))]
        public void ProductionCatalog_DefinitionMatchesExpectedValue(
            string kindName,
            string displayName,
            string closedHanName,
            string openHanName,
            bool isYakuman)
        {
            var definition = LoadDefinitions()
                .SingleOrDefault(candidate => candidate != null && DefinitionKindName(candidate) == kindName);

            Assert.That(
                definition,
                Is.Not.Null,
                $"Production yaku catalog at {CatalogPath} does not contain expected kind {kindName}.");

            Assert.That(DefinitionKindName(definition), Is.EqualTo(kindName), $"{kindName} Kind does not match.");
            Assert.That(DefinitionDisplayName(definition), Is.EqualTo(displayName), $"{kindName} DisplayName does not match.");
            Assert.That(DefinitionClosedHanName(definition), Is.EqualTo(closedHanName), $"{kindName} ClosedHan does not match.");
            Assert.That(DefinitionOpenHanName(definition), Is.EqualTo(openHanName), $"{kindName} OpenHan does not match.");
            Assert.That(DefinitionIsYakuman(definition), Is.EqualTo(isYakuman), $"{kindName} IsYakuman does not match.");
            Assert.That(DefinitionIsEnabled(definition), Is.True, $"{kindName} IsEnabled must be true in the production catalog.");
        }

        [TestCase("Renhou")]
        public void ProductionCatalog_CurrentlyUnimplementedKinds_AreNotRegistered(string kindName)
        {
            Assert.That(
                LoadDefinitions().Where(definition => definition != null).Select(DefinitionKindName),
                Does.Not.Contain(kindName),
                $"{kindName} is currently enum-only and must not be registered in the production catalog yet.");
        }

        private static IEnumerable<TestCaseData> ExpectedDefinitionCases()
        {
            foreach (ExpectedDefinition definition in ExpectedDefinitions)
            {
                yield return new TestCaseData(
                        definition.KindName,
                        definition.DisplayName,
                        definition.ClosedHanName,
                        definition.OpenHanName,
                        definition.IsYakuman)
                    .SetName($"ProductionCatalog_DefinitionMatchesExpectedValue_{definition.KindName}");
            }
        }

        private static object LoadCatalog()
        {
            return AssetDatabase.LoadAssetAtPath(CatalogPath, RequireType(YakuDefinitionCatalogTypeName));
        }

        private static IReadOnlyList<object> LoadDefinitions()
        {
            var catalog = LoadCatalog();
            Assert.That(
                catalog,
                Is.Not.Null,
                $"Production yaku catalog was not found at {CatalogPath}.");
            object definitions = GetProperty(catalog, "Definitions");
            Assert.That(
                definitions,
                Is.Not.Null,
                $"Production yaku catalog at {CatalogPath} has null Definitions.");
            Assert.That(
                definitions,
                Is.AssignableTo<IEnumerable>(),
                $"Production yaku catalog at {CatalogPath} has non-enumerable Definitions.");

            return ((IEnumerable)definitions).Cast<object>().ToArray();
        }

        private static string JoinKinds(IEnumerable<string> kinds)
        {
            return string.Join(", ", kinds);
        }

        private static readonly ExpectedDefinition[] ExpectedDefinitions =
        {
            new ExpectedDefinition("MenzenTsumo", "門前清自摸和", "One", "None", false),
            new ExpectedDefinition("Reach", "リーチ", "One", "None", false),
            new ExpectedDefinition("Ippatsu", "一発", "One", "None", false),
            new ExpectedDefinition("HaiteiRaoyue", "海底撈月", "One", "One", false),
            new ExpectedDefinition("HouteiRaoyui", "河底撈魚", "One", "One", false),
            new ExpectedDefinition("Tanyao", "断么九", "One", "One", false),
            new ExpectedDefinition("Pinfu", "平和", "One", "None", false),
            new ExpectedDefinition("Iipeikou", "一盃口", "One", "None", false),
            new ExpectedDefinition("Ryanpeikou", "二盃口", "Three", "None", false),
            new ExpectedDefinition("SevenPairs", "七対子", "Two", "None", false),
            new ExpectedDefinition("KokushiMusou", "国士無双", "None", "None", true),
            new ExpectedDefinition("KokushiMusouThirteenWait", "国士無双　十三面待ち", "None", "None", true),
            new ExpectedDefinition("YakuhaiSeatWind", "役牌・自風", "One", "One", false),
            new ExpectedDefinition("YakuhaiRoundWind", "役牌・場風", "One", "One", false),
            new ExpectedDefinition("YakuhaiWhiteDragon", "白", "One", "One", false),
            new ExpectedDefinition("YakuhaiGreenDragon", "發", "One", "One", false),
            new ExpectedDefinition("YakuhaiRedDragon", "中", "One", "One", false),
            new ExpectedDefinition("DoubleReach", "ダブルリーチ", "Two", "None", false),
            new ExpectedDefinition("SanshokuDoukou", "三色同刻", "Two", "One", false),
            new ExpectedDefinition("SanshokuDoujun", "三色同順", "Two", "One", false),
            new ExpectedDefinition("Ittsuu", "一気通貫", "Two", "One", false),
            new ExpectedDefinition("Chanta", "混全帯么九", "Two", "One", false),
            new ExpectedDefinition("Junchan", "純全帯幺九", "Three", "Two", false),
            new ExpectedDefinition("Shousangen", "小三元", "Two", "Two", false),
            new ExpectedDefinition("Daisangen", "大三元", "None", "None", true),
            new ExpectedDefinition("Honitsu", "混一色", "Three", "Two", false),
            new ExpectedDefinition("Chinitsu", "清一色", "Six", "Five", false),
            new ExpectedDefinition("Ryuuiisou", "緑一色", "None", "None", true),
            new ExpectedDefinition("Sanankou", "三暗刻", "Two", "Two", false),
            new ExpectedDefinition("Suuankou", "四暗刻", "None", "None", true),
            new ExpectedDefinition("SuuankouTanki", "四暗刻　単騎", "None", "None", true),
            new ExpectedDefinition("Shousuushii", "小四喜", "None", "None", true),
            new ExpectedDefinition("Daisuushii", "大四喜", "None", "None", true),
            new ExpectedDefinition("Tsuuiisou", "字一色", "None", "None", true),
            new ExpectedDefinition("Honroutou", "混老頭", "Two", "Two", false),
            new ExpectedDefinition("Chinroutou", "清老頭", "None", "None", true),
            new ExpectedDefinition("ChuurenPoutou", "九蓮宝燈", "None", "None", true),
            new ExpectedDefinition("JunseiChuurenPoutou", "純正九蓮宝燈", "None", "None", true),
            new ExpectedDefinition("RinshanKaihou", "嶺上開花", "One", "One", false),
            new ExpectedDefinition("Chankan", "槍槓", "One", "One", false),
            new ExpectedDefinition("Toitoi", "対々和", "Two", "Two", false),
            new ExpectedDefinition("Sankantsu", "三槓子", "Two", "Two", false),
            new ExpectedDefinition("Suukantsu", "四槓子", "None", "None", true),
            new ExpectedDefinition("Tenhou", "天和", "None", "None", true),
            new ExpectedDefinition("Chiihou", "地和", "None", "None", true)
        };

        private static string DefinitionKindName(object definition)
        {
            return GetProperty(definition, "Kind").ToString();
        }

        private static string DefinitionDisplayName(object definition)
        {
            return (string)GetProperty(definition, "DisplayName");
        }

        private static string DefinitionClosedHanName(object definition)
        {
            return GetProperty(definition, "ClosedHan").ToString();
        }

        private static string DefinitionOpenHanName(object definition)
        {
            return GetProperty(definition, "OpenHan").ToString();
        }

        private static bool DefinitionIsYakuman(object definition)
        {
            return (bool)GetProperty(definition, "IsYakuman");
        }

        private static bool DefinitionIsEnabled(object definition)
        {
            return (bool)GetProperty(definition, "IsEnabled");
        }

        private static object GetProperty(object target, string propertyName)
        {
            Assert.That(target, Is.Not.Null, $"Cannot read {propertyName} from a null target.");

            var property = target.GetType().GetProperty(propertyName);
            Assert.That(
                property,
                Is.Not.Null,
                $"Property not found: {target.GetType().FullName}.{propertyName}");

            return property.GetValue(target);
        }

        private static Type RequireType(string assemblyQualifiedName)
        {
            Type type = Type.GetType(assemblyQualifiedName, false);
            Assert.That(type, Is.Not.Null, $"Type not found: {assemblyQualifiedName}");
            return type;
        }

        private readonly struct ExpectedDefinition
        {
            public ExpectedDefinition(
                string kindName,
                string displayName,
                string closedHanName,
                string openHanName,
                bool isYakuman)
            {
                KindName = kindName;
                DisplayName = displayName;
                ClosedHanName = closedHanName;
                OpenHanName = openHanName;
                IsYakuman = isYakuman;
            }

            public string KindName { get; }
            public string DisplayName { get; }
            public string ClosedHanName { get; }
            public string OpenHanName { get; }
            public bool IsYakuman { get; }
        }
    }

    public sealed class ProductionYakuCatalogSceneReferenceTests
    {
        private const string ScenePath = "Assets/Scenes/Mahjong Prototype.unity";
        private const string CatalogPath =
            "Assets/Scripts/MahjongPrototype/ScriptableObjects/YakuDefinitionCatalog.asset";
        private const string SerializedFieldName = "yakuDefinitionCatalog";
        private const string MahjongGameFlowTypeName = "MahjongPrototype.MahjongGameFlow, Assembly-CSharp";
        private const string YakuDefinitionCatalogTypeName =
            "MahjongPrototype.Definitions.YakuDefinitionCatalog, Assembly-CSharp";

        [Test]
        public void ProductionScene_MahjongGameFlowReferencesProductionYakuCatalog()
        {
            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath),
                Is.Not.Null,
                $"Production scene asset was not found at {ScenePath}.");
            Assert.That(
                AssetDatabase.LoadAssetAtPath(CatalogPath, RequireType(YakuDefinitionCatalogTypeName)),
                Is.Not.Null,
                $"Production yaku catalog asset was not found at {CatalogPath}.");

            string catalogGuid = AssetDatabase.AssetPathToGUID(CatalogPath);
            Assert.That(catalogGuid, Is.Not.Empty, $"Could not resolve GUID for production yaku catalog at {CatalogPath}.");

            string mahjongGameFlowScriptGuid = FindMahjongGameFlowScriptGuid();
            string sceneText = File.ReadAllText(ProjectPath(ScenePath));
            string[] mahjongGameFlowBlocks = SerializedBlocks(sceneText)
                .Where(block =>
                    block.Contains($"m_Script: {{fileID: 11500000, guid: {mahjongGameFlowScriptGuid}, type: 3}}") &&
                    block.Contains("m_EditorClassIdentifier: Assembly-CSharp::MahjongPrototype.MahjongGameFlow"))
                .ToArray();

            Assert.That(
                mahjongGameFlowBlocks,
                Has.Length.EqualTo(1),
                $"Expected exactly one MahjongGameFlow MonoBehaviour block in {ScenePath}.");

            string blockText = mahjongGameFlowBlocks[0];
            Match fieldMatch = Regex.Match(
                blockText,
                @"^\s*" + SerializedFieldName + @":\s*\{(?<body>[^}]*)\}",
                RegexOptions.Multiline);

            Assert.That(
                fieldMatch.Success,
                Is.True,
                $"{ScenePath} does not contain serialized field {SerializedFieldName} on MahjongGameFlow.");

            string referenceBody = fieldMatch.Groups["body"].Value;
            Match fileIdMatch = Regex.Match(referenceBody, @"fileID:\s*(?<fileID>-?\d+)");
            Match guidMatch = Regex.Match(referenceBody, @"guid:\s*(?<guid>[0-9a-fA-F]+)");

            Assert.That(
                fileIdMatch.Success,
                Is.True,
                $"{SerializedFieldName} on MahjongGameFlow does not contain a fileID.");
            Assert.That(
                fileIdMatch.Success ? fileIdMatch.Groups["fileID"].Value : string.Empty,
                Is.Not.EqualTo("0"),
                $"{SerializedFieldName} on MahjongGameFlow is a null reference.");
            Assert.That(
                guidMatch.Success,
                Is.True,
                $"{SerializedFieldName} on MahjongGameFlow does not contain a GUID.");
            Assert.That(
                guidMatch.Success ? guidMatch.Groups["guid"].Value : string.Empty,
                Is.EqualTo(catalogGuid),
                $"{SerializedFieldName} on MahjongGameFlow must reference {CatalogPath}.");
        }

        private static string FindMahjongGameFlowScriptGuid()
        {
            string[] scriptGuids = AssetDatabase.FindAssets("MahjongGameFlow t:MonoScript");
            Type gameFlowType = RequireType(MahjongGameFlowTypeName);
            foreach (string scriptGuid in scriptGuids)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                if (script != null && script.GetClass() == gameFlowType)
                    return scriptGuid;
            }

            Assert.Fail("Could not resolve the MonoScript GUID for MahjongPrototype.MahjongGameFlow.");
            return string.Empty;
        }

        private static string[] SerializedBlocks(string text)
        {
            return Regex.Split(text, @"(?=^--- !u!)", RegexOptions.Multiline);
        }

        private static string ProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), assetPath);
        }

        private static Type RequireType(string assemblyQualifiedName)
        {
            Type type = Type.GetType(assemblyQualifiedName, false);
            Assert.That(type, Is.Not.Null, $"Type not found: {assemblyQualifiedName}");
            return type;
        }
    }
}
