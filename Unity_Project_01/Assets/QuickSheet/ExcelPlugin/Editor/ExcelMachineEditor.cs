///////////////////////////////////////////////////////////////////////////////
///
/// ExcelMachineEditor.cs
///
/// (c)2014 Kim, Hyoun Woo
///
///////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace UnityQuickSheet
{
    /// <summary>
    /// Custom editor script class for excel file setting.
    /// </summary>
    [CustomEditor(typeof(ExcelMachine))]
    public class ExcelMachineEditor : BaseMachineEditor
    {
        protected override void OnEnable()
        {
            base.OnEnable();

            machine = target as ExcelMachine;
            if (machine != null && ExcelSettings.Instance != null)
            {
                machine.ReInitialize();
                if (string.IsNullOrEmpty(ExcelSettings.Instance.RuntimePath) == false)
                    machine.RuntimeClassPath = ExcelSettings.Instance.RuntimePath;
                if (string.IsNullOrEmpty(ExcelSettings.Instance.EditorPath) == false)
                    machine.EditorClassPath = ExcelSettings.Instance.EditorPath;
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            ExcelMachine machine = target as ExcelMachine;

            GUILayout.Label("Excel Spreadsheet Settings:", headerStyle);

            machine.DataSource = (ExcelMachine.DataSourceKind)EditorGUILayout.EnumPopup("Source", machine.DataSource);

            if (!machine.UsesSpreadsheet)
            {
                DrawFileSelector(machine);
            }

            EditorGUI.BeginDisabledGroup(!machine.UsesSpreadsheet);
            machine.spreadsheetUrl = EditorGUILayout.TextField("Spreadsheet URL:", machine.spreadsheetUrl);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            bool hasSheetData = machine.SheetNames != null && machine.SheetNames.Any(s => string.IsNullOrEmpty(s) == false);
            string[] sheetOptions = hasSheetData ? machine.SheetNames : new string[] { "(Press Refresh)" };

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Worksheet: ", GUILayout.Width(100));
                EditorGUI.BeginDisabledGroup(!hasSheetData);
                int newIndex = EditorGUILayout.Popup(machine.CurrentSheetIndex, sheetOptions);
                EditorGUI.EndDisabledGroup();
                if (hasSheetData)
                {
                    machine.CurrentSheetIndex = Mathf.Clamp(newIndex, 0, machine.SheetNames.Length - 1);
                    machine.WorkSheetName = machine.SheetNames[machine.CurrentSheetIndex];
                }

                if (GUILayout.Button("Refresh", GUILayout.Width(80)))
                {
                    RefreshMachineAsset(machine);
                }
            }

            if (!hasSheetData)
            {
                EditorGUILayout.HelpBox("No worksheet is loaded. Set the source and press Refresh to pull sheet and column data.", MessageType.Info);
            }

            EditorGUILayout.Separator();

            GUILayout.BeginHorizontal();

            //if (machine.HasColumnHeader())
            //{
            //    if (GUILayout.Button("Update"))
            //        Import();
            //    if (GUILayout.Button("Reimport"))
            //}
            //else
            //{
            //    if (GUILayout.Button("Import"))
            //        Import();
            //}

            GUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            DrawHeaderSetting(machine);

            EditorGUILayout.Separator();


            EditorGUILayout.Separator();

            if (GUILayout.Button("Generate"))
            {
                if (string.IsNullOrEmpty(machine.SpreadSheetName) || string.IsNullOrEmpty(machine.WorkSheetName))
                {
                    Debug.LogWarning("No spreadsheet or worksheet is specified.");
                    return;
                }

                Directory.CreateDirectory(Application.dataPath + Path.DirectorySeparatorChar + machine.RuntimeClassPath);
                Directory.CreateDirectory(Application.dataPath + Path.DirectorySeparatorChar + machine.EditorClassPath);

                ScriptPrescription sp = Generate(machine);
                if (sp != null)
                {
                    Debug.Log("Successfully generated!");
                }
                else
                    Debug.LogError("Failed to create a script from excel.");
            }

            EditorGUILayout.Separator();

            DrawManagedSheetList(machine);


            if (GUI.changed)
            {
                EditorUtility.SetDirty(machine);
            }
        }

        private bool RefreshMachineAsset(ExcelMachine machine)
        {
            if (machine == null)
                return false;

            if (!TryReloadSheets(machine))
                return false;

            Import(true);
            SyncAllSheetData(machine);
            EditorUtility.SetDirty(machine);
            return true;
        }

        /// <summary>
        /// Import the specified excel file and prepare to set type of each cell.
        /// </summary>
        protected override void Import(bool reimport = false)
        {
            ExcelMachine machine = target as ExcelMachine;

            if (!TryGetAbsoluteDataPath(machine, out string path))
                return;

            string sheet = machine.WorkSheetName;

            int startRowIndex = 0;
            string error = string.Empty;
            var titles = new ExcelQuery(path, sheet).GetTitle(startRowIndex, ref error);
            if (titles == null || !string.IsNullOrEmpty(error))
            {
                EditorUtility.DisplayDialog("Error", error, "OK");
                return;
            }
            else
            {
                // check the column header is valid
                foreach (string column in titles)
                {
                    if (!IsValidHeader(column))
                    {
                        error = string.Format(@"Invalid column header name {0}. Any c# keyword should not be used for column header. Note it is not case sensitive.", column);
                        EditorUtility.DisplayDialog("Error", error, "OK");
                        return;
                    }
                }
            }

            List<string> titleList = titles.ToList();

            if (machine.HasColumnHeader() && reimport == false)
            {
                var headerDic = machine.ColumnHeaderList.ToDictionary(header => header.name);

                // collect non-changed column headers
                var exist = titleList.Select(t => GetColumnHeaderString(t))
                    .Where(e => headerDic.ContainsKey(e) == true)
                    .Select(t => new ColumnHeader { name = t, type = headerDic[t].type, isArray = headerDic[t].isArray, OrderNO = headerDic[t].OrderNO });


                // collect newly added or changed column headers
                var changed = titleList.Select(t => GetColumnHeaderString(t))
                    .Where(e => headerDic.ContainsKey(e) == false)
                    .Select(t => ParseColumnHeader(t, titleList.IndexOf(t)));

                // merge two list via LINQ
                var merged = exist.Union(changed).OrderBy(x => x.OrderNO);

                machine.ColumnHeaderList.Clear();
                machine.ColumnHeaderList = merged.ToList();
            }
            else
            {
                machine.ColumnHeaderList.Clear();
                if (titleList.Count > 0)
                {
                    int order = 0;
                    machine.ColumnHeaderList = titleList.Select(e => ParseColumnHeader(e, order++)).ToList();
                }
                else
                {
                    string msg = string.Format("An empty workhheet: [{0}] ", sheet);
                    Debug.LogWarning(msg);
                }
            }

            EditorUtility.SetDirty(machine);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Generate AssetPostprocessor editor script file.
        /// </summary>
        protected override void CreateAssetCreationScript(BaseMachine m, ScriptPrescription sp)
        {
            ExcelMachine machine = target as ExcelMachine;

            sp.className = machine.WorkSheetName;
            sp.dataClassName = machine.WorkSheetName + "Data";
            sp.worksheetClassName = machine.WorkSheetName;

            // where the imported excel file is.
            sp.importedFilePath = machine.excelFilePath;

            // path where the .asset file will be created (same folder as the ExcelMachine asset).
            string machineAssetPath = AssetDatabase.GetAssetPath(machine);
            string machineDirectory = string.IsNullOrEmpty(machineAssetPath) ? "Assets" : Path.GetDirectoryName(machineAssetPath);
            string path = Path.Combine(machineDirectory ?? "Assets", machine.WorkSheetName + ".asset");
            sp.assetFilepath = path.Replace('\\', '/');
            sp.assetPostprocessorClass = machine.WorkSheetName + "AssetPostprocessor";
            sp.template = GetTemplate("PostProcessor");

            // write a script to the given folder.
            using (var writer = new StreamWriter(TargetPathForAssetPostProcessorFile(machine.WorkSheetName)))
            {
                writer.Write(new ScriptGenerator(sp).ToString());
                writer.Close();
            }
        }

        private void DrawFileSelector(ExcelMachine machine)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(machine.UsesSpreadsheet ? "Cache File:" : "File:", GUILayout.Width(80));

            string currentPath = string.IsNullOrEmpty(machine.excelFilePath) ? Application.dataPath : machine.excelFilePath;
            bool isReadOnly = machine.UsesSpreadsheet;

            EditorGUI.BeginDisabledGroup(isReadOnly);
            string editedPath = GUILayout.TextField(currentPath, GUILayout.ExpandWidth(true));
            if (!isReadOnly)
            {
                machine.excelFilePath = editedPath;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(isReadOnly);
            if (GUILayout.Button("...", GUILayout.Width(24)))
            {
                string folder = Path.GetDirectoryName(ToAbsolutePath(currentPath));
                if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                {
                    folder = Application.dataPath;
                }

                string selectedPath = ShowExcelFilePanel(folder);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    ApplyExcelSelection(machine, selectedPath);
                }
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();
        }

        private string ShowExcelFilePanel(string folder)
        {
#if UNITY_EDITOR_WIN
            return EditorUtility.OpenFilePanel("Open Excel file", folder, "excel files;*.xls;*.xlsx");
#else
            return EditorUtility.OpenFilePanel("Open Excel file", folder, "xls");
#endif
        }

        private void ApplyExcelSelection(ExcelMachine machine, string absolutePath)
        {
            if (!TryMakeAssetsRelativePath(absolutePath, out string relativePath))
            {
                EditorUtility.DisplayDialog("Error",
                    @"Wrong folder is selected.
Set a folder under the 'Assets' folder!",
                    "OK");
                return;
            }

            machine.SpreadSheetName = Path.GetFileName(absolutePath);
            machine.excelFilePath = relativePath;

            var sheets = new ExcelQuery(absolutePath).GetSheetNames();
            if (sheets == null || sheets.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "Failed to read any worksheet from the selected file.", "OK");
                return;
            }

            machine.SheetNames = sheets;
            SyncWorksheetSelection(machine);
        }

        private bool TryReloadSheets(ExcelMachine machine)
        {
            if (machine.UsesSpreadsheet)
            {
                return TryDownloadSpreadsheet(machine);
            }

            return TryLoadLocalExcel(machine);
        }

        private bool TryLoadLocalExcel(ExcelMachine machine)
        {
            if (!TryGetAbsoluteDataPath(machine, out string path))
                return false;

            var sheets = new ExcelQuery(path).GetSheetNames();
            if (sheets == null || sheets.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "Failed to retrieve worksheets from the excel file.", "OK");
                return false;
            }

            PurgeMissingSheets(machine, sheets);
            machine.SheetNames = sheets;
            SyncWorksheetSelection(machine);
            return true;
        }

        private bool TryDownloadSpreadsheet(ExcelMachine machine)
        {
            if (string.IsNullOrEmpty(machine.spreadsheetUrl))
            {
                EditorUtility.DisplayDialog("Error", "Enter a spreadsheet URL first.", "OK");
                return false;
            }

            string exportUrl = BuildExportUrl(machine.spreadsheetUrl.Trim());
            if (string.IsNullOrEmpty(exportUrl))
            {
                EditorUtility.DisplayDialog("Error", "Unable to parse the spreadsheet URL.", "OK");
                return false;
            }

            string cacheFolder = GetSpreadsheetCacheAbsolutePath(machine, out string cacheFolderRelative);
            if (!Directory.Exists(cacheFolder))
            {
                Directory.CreateDirectory(cacheFolder);
            }

            string fileName = SanitizeFileName(machine.name) + ".xlsx";
            string absolutePath = Path.Combine(cacheFolder, fileName);
            bool downloadSuccess = false;
            try
            {
                EditorUtility.DisplayProgressBar("QuickSheet", "Downloading spreadsheet...", 0.5f);
                using (var client = new WebClient())
                {
                    client.Headers.Add("user-agent", "UnityEditor");
                    client.DownloadFile(exportUrl, absolutePath);
                }
                downloadSuccess = true;
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", "Failed to download spreadsheet.\n" + ex.Message, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (!downloadSuccess)
                return false;

            string relativePath = Path.Combine(cacheFolderRelative, fileName).Replace('\\', '/');
            machine.excelFilePath = relativePath;
            machine.SpreadSheetName = DeriveSpreadsheetDisplayName(machine.spreadsheetUrl);

            var sheets = new ExcelQuery(absolutePath).GetSheetNames();
            if (sheets == null || sheets.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "Downloaded spreadsheet does not contain any worksheets.", "OK");
                return false;
            }

            PurgeMissingSheets(machine, sheets);
            machine.SheetNames = sheets;
            SyncWorksheetSelection(machine);

            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            return true;
        }

        private void SyncWorksheetSelection(ExcelMachine machine)
        {
            if (machine.SheetNames == null || machine.SheetNames.Length == 0)
                return;

            machine.CurrentSheetIndex = Mathf.Clamp(machine.CurrentSheetIndex, 0, machine.SheetNames.Length - 1);
            machine.WorkSheetName = machine.SheetNames[machine.CurrentSheetIndex];
        }

        private bool TryGetAbsoluteDataPath(ExcelMachine machine, out string absolutePath)
        {
            absolutePath = string.Empty;
            if (string.IsNullOrEmpty(machine.excelFilePath))
            {
                string message = machine.UsesSpreadsheet
                    ? "No cached spreadsheet file is available. Press Refresh to download it."
                    : "Specify an excel file first.";
                EditorUtility.DisplayDialog("Error", message, "OK");
                return false;
            }

            absolutePath = ToAbsolutePath(machine.excelFilePath);
            if (!File.Exists(absolutePath))
            {
                EditorUtility.DisplayDialog("Error", string.Format("File at {0} does not exist. Press Refresh to retrieve it again.", absolutePath), "OK");
                return false;
            }

            return true;
        }

        private static bool TryMakeAssetsRelativePath(string absolutePath, out string relativePath)
        {
            relativePath = string.Empty;
            if (string.IsNullOrEmpty(absolutePath))
                return false;

            int index = absolutePath.IndexOf("Assets", StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return false;

            relativePath = absolutePath.Substring(index).Replace('\\', '/');
            return true;
        }

        private static string ToAbsolutePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            if (Path.IsPathRooted(path))
                return path;

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, path);
        }

        private static string GetSpreadsheetCacheAbsolutePath(ExcelMachine machine, out string relativeFolder)
        {
            relativeFolder = string.Empty;

            string assetPath = machine != null ? AssetDatabase.GetAssetPath(machine) : string.Empty;
            if (!string.IsNullOrEmpty(assetPath))
            {
                string assetDirectory = Path.GetDirectoryName(assetPath);
                if (!string.IsNullOrEmpty(assetDirectory))
                {
                    relativeFolder = Path.Combine(assetDirectory, "SpreadsheetCache").Replace('\\', '/');
                }
            }

            if (string.IsNullOrEmpty(relativeFolder))
            {
                relativeFolder = "Assets/QuickSheet/SpreadsheetCache";
            }

            string absolute = ToAbsolutePath(relativeFolder);
            return absolute;
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "Spreadsheet";

            var invalidChars = Path.GetInvalidFileNameChars();
            var safeChars = value.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray();
            string safe = new string(safeChars);
            return string.IsNullOrEmpty(safe) ? "Spreadsheet" : safe;
        }

        private static string DeriveSpreadsheetDisplayName(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "Spreadsheet";

            var match = Regex.Match(url, @"spreadsheets\/d\/([a-zA-Z0-9-_]+)", RegexOptions.IgnoreCase);
            if (match.Success)
                return "Spreadsheet-" + match.Groups[1].Value;

            return url;
        }

        private static string BuildExportUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            if (url.Contains("export?format="))
                return url;

            var match = Regex.Match(url, @"spreadsheets\/d\/([a-zA-Z0-9-_]+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string docId = match.Groups[1].Value;
                return string.Format("https://docs.google.com/spreadsheets/d/{0}/export?format=xlsx", docId);
            }

            return url;
        }

        private void SyncAllSheetData(ExcelMachine machine)
        {
            EnsureManagedSheetList(machine);
            var targets = GatherRefreshTargets(machine);
            int successCount = 0;
            int failureCount = 0;
            var failureReasons = new List<string>();
            foreach (var target in targets)
            {
                var result = UpdateScriptableObjectData(machine, target.sheetName, target.assetPath);
                if (result.Success)
                {
                    successCount++;
                }
                else
                {
                    failureCount++;
                    if (string.IsNullOrEmpty(result.Message) == false)
                        failureReasons.Add(result.Message);
                }
            }

            string summary = string.Format("QuickSheet: 로드 성공 {0} / 로드 실패 {1}", successCount, failureCount);
            if (failureReasons.Count > 0)
                summary += " / 실패원인: " + string.Join(", ", failureReasons);

            Debug.Log(summary);
        }

        private void PurgeMissingSheets(ExcelMachine machine, string[] latestSheets)
        {
            EnsureManagedSheetList(machine);
            if (machine.ManagedSheets == null || machine.ManagedSheets.Count == 0)
                return;

            var available = new HashSet<string>(latestSheets ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var removals = new List<ExcelMachine.SheetBinding>();

            foreach (var binding in machine.ManagedSheets)
            {
                if (binding == null || string.IsNullOrEmpty(binding.SheetName))
                    continue;

                if (!available.Contains(binding.SheetName))
                {
                    DeleteSheetArtifacts(machine, binding);
                    removals.Add(binding);
                }
            }

            foreach (var removal in removals)
            {
                machine.ManagedSheets.Remove(removal);
            }

            if (removals.Count > 0)
            {
                EditorUtility.SetDirty(machine);
                AssetDatabase.SaveAssets();
            }
        }

        private void DeleteSheetArtifacts(ExcelMachine machine, ExcelMachine.SheetBinding binding)
        {
            if (binding == null || string.IsNullOrEmpty(binding.SheetName))
                return;

            string sheetName = binding.SheetName;

            DeleteAssetIfExists(BuildRuntimeScriptPath(machine, sheetName));
            DeleteAssetIfExists(BuildRuntimeDataPath(machine, sheetName));
            DeleteAssetIfExists(BuildEditorScriptPath(machine, sheetName));
            DeleteAssetIfExists(BuildPostprocessorPath(machine, sheetName));

            string assetPath = !string.IsNullOrEmpty(binding.AssetPath)
                ? binding.AssetPath
                : GetScriptableAssetRelativePath(machine, sheetName);
            DeleteAssetIfExists(assetPath);
        }

        private static string BuildRuntimeScriptPath(ExcelMachine machine, string sheetName)
        {
            if (machine == null || string.IsNullOrEmpty(machine.RuntimeClassPath) || string.IsNullOrEmpty(sheetName))
                return string.Empty;

            return NormalizeAssetPath(Path.Combine("Assets/" + machine.RuntimeClassPath, sheetName + ".cs"));
        }

        private static string BuildRuntimeDataPath(ExcelMachine machine, string sheetName)
        {
            if (machine == null || string.IsNullOrEmpty(machine.RuntimeClassPath) || string.IsNullOrEmpty(sheetName))
                return string.Empty;

            return NormalizeAssetPath(Path.Combine("Assets/" + machine.RuntimeClassPath, sheetName + "Data.cs"));
        }

        private static string BuildEditorScriptPath(ExcelMachine machine, string sheetName)
        {
            if (machine == null || string.IsNullOrEmpty(machine.EditorClassPath) || string.IsNullOrEmpty(sheetName))
                return string.Empty;

            return NormalizeAssetPath(Path.Combine("Assets/" + machine.EditorClassPath, sheetName + "Editor.cs"));
        }

        private static string BuildPostprocessorPath(ExcelMachine machine, string sheetName)
        {
            if (machine == null || string.IsNullOrEmpty(machine.EditorClassPath) || string.IsNullOrEmpty(sheetName))
                return string.Empty;

            return NormalizeAssetPath(Path.Combine("Assets/" + machine.EditorClassPath, sheetName + "AssetPostProcessor.cs"));
        }

        private void DeleteAssetIfExists(string assetPath)
        {
            assetPath = NormalizeAssetPath(assetPath);
            if (string.IsNullOrEmpty(assetPath))
                return;

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            bool exists = string.IsNullOrEmpty(guid) == false;

            if (!exists)
            {
                string absolute = ToAbsolutePath(assetPath);
                exists = File.Exists(absolute);
            }

            if (!exists)
                return;

            AssetDatabase.DeleteAsset(assetPath);
        }

        private static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrEmpty(path) ? path : path.Replace('\\', '/');
        }

        private List<(string sheetName, string assetPath)> GatherRefreshTargets(ExcelMachine machine)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (machine.ManagedSheets != null)
            {
                foreach (var binding in machine.ManagedSheets)
                {
                    if (binding == null || string.IsNullOrEmpty(binding.SheetName))
                        continue;

                    if (!map.ContainsKey(binding.SheetName))
                        map.Add(binding.SheetName, binding.AssetPath);
                }
            }

            if (!string.IsNullOrEmpty(machine.WorkSheetName) && !map.ContainsKey(machine.WorkSheetName))
                map.Add(machine.WorkSheetName, null);

            return map.Select(kvp => (kvp.Key, kvp.Value)).ToList();
        }

        private SheetUpdateResult UpdateScriptableObjectData(ExcelMachine machine, string sheetName, string assetPathOverride)
        {
            EnsureManagedSheetList(machine);

            if (string.IsNullOrEmpty(sheetName))
            {
                return new SheetUpdateResult { Success = false, Message = "시트 이름이 비어 있습니다." };
            }

            if (!TryGetAbsoluteDataPath(machine, out string absoluteSourcePath))
            {
                MarkSheetStatus(machine, sheetName, "FAIL");
                return new SheetUpdateResult { Success = false, Message = sheetName + ": 원본 경로를 찾을 수 없습니다." };
            }

            string assetRelativePath = string.IsNullOrEmpty(assetPathOverride)
                ? GetScriptableAssetRelativePath(machine, sheetName)
                : assetPathOverride;

            if (string.IsNullOrEmpty(assetRelativePath))
            {
                string reason = sheetName + ": Asset 경로를 찾을 수 없습니다.";
                Debug.LogWarning("QuickSheet: " + reason);
                MarkSheetStatus(machine, sheetName, "FAIL");
                return new SheetUpdateResult { Success = false, Message = reason };
            }

            Type containerType = FindTypeByName(sheetName);
            if (containerType == null)
            {
                string reason = sheetName + ": ScriptableObject 타입을 찾을 수 없습니다.";
                Debug.LogWarning("QuickSheet: " + reason);
                MarkSheetStatus(machine, sheetName, "FAIL");
                return new SheetUpdateResult { Success = false, Message = reason };
            }

            Type dataType = FindTypeByName(sheetName + "Data");
            if (dataType == null)
            {
                string reason = sheetName + ": Data 타입을 찾을 수 없습니다.";
                Debug.LogWarning("QuickSheet: " + reason);
                MarkSheetStatus(machine, sheetName, "FAIL");
                return new SheetUpdateResult { Success = false, Message = reason };
            }

            ScriptableObject asset = LoadOrCreateScriptableAsset(assetRelativePath, containerType);
            if (asset == null)
            {
                string reason = sheetName + ": Asset 생성 또는 로드에 실패했습니다.";
                Debug.LogWarning("QuickSheet: " + reason);
                MarkSheetStatus(machine, sheetName, "FAIL");
                return new SheetUpdateResult { Success = false, Message = reason };
            }

            SetStringField(asset, "SheetName", machine.excelFilePath);
            SetStringField(asset, "WorksheetName", sheetName);

            ExcelQuery query = new ExcelQuery(absoluteSourcePath, sheetName);
            if (query == null || !query.IsValid())
            {
                string reason = sheetName + ": 원본 파일을 열 수 없습니다.";
                Debug.LogWarning("QuickSheet: " + reason);
                MarkSheetStatus(machine, sheetName, "FAIL");
                return new SheetUpdateResult { Success = false, Message = reason };
            }

            object list = InvokeDeserialize(query, dataType);
            if (list == null)
            {
                string reason = sheetName + ": 데이터 역직렬화에 실패했습니다.";
                Debug.LogWarning("QuickSheet: " + reason);
                MarkSheetStatus(machine, sheetName, "FAIL");
                return new SheetUpdateResult { Success = false, Message = reason };
            }

            object array = InvokeToArray(list);
            if (array == null)
            {
                string reason = sheetName + ": 데이터 배열 변환에 실패했습니다.";
                Debug.LogWarning("QuickSheet: " + reason);
                MarkSheetStatus(machine, sheetName, "FAIL");
                return new SheetUpdateResult { Success = false, Message = reason };
            }

            if (!SetDataArray(asset, array))
            {
                string reason = sheetName + ": dataArray 할당에 실패했습니다.";
                Debug.LogWarning("QuickSheet: " + reason);
                MarkSheetStatus(machine, sheetName, "FAIL");
                return new SheetUpdateResult { Success = false, Message = reason };
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            RegisterManagedSheet(machine, sheetName, assetRelativePath);
            MarkSheetStatus(machine, sheetName, "OK");
            return new SheetUpdateResult { Success = true };
        }

        private struct SheetUpdateResult
        {
            public bool Success;
            public string Message;
        }

        private void DrawManagedSheetList(ExcelMachine machine)
        {
            EnsureManagedSheetList(machine);
            EditorGUILayout.LabelField("Managed Sheets:", EditorStyles.boldLabel);
            if (machine.ManagedSheets.Count == 0)
            {
                EditorGUILayout.HelpBox("No managed sheets yet. Refresh a worksheet once to register it.", MessageType.Info);
                return;
            }

            ExcelMachine.SheetBinding removal = null;
            foreach (var binding in machine.ManagedSheets)
            {
                if (binding == null)
                    continue;

                string resolvedAssetPath = GetScriptableAssetRelativePath(machine, binding.SheetName);
                if (!string.IsNullOrEmpty(resolvedAssetPath) && binding.AssetPath != resolvedAssetPath)
                {
                    binding.AssetPath = resolvedAssetPath;
                    EditorUtility.SetDirty(machine);
                }

                using (new GUILayout.HorizontalScope())
                {
                    string status = string.IsNullOrEmpty(binding.LastStatus) ? "---" : binding.LastStatus;
                    EditorGUILayout.LabelField(string.Format("[{0}] {1}", status, binding.SheetName), GUILayout.ExpandWidth(true));
                    bool hasAsset = !string.IsNullOrEmpty(binding.AssetPath);
                    EditorGUI.BeginDisabledGroup(!hasAsset);
                    if (GUILayout.Button("Ping", GUILayout.Width(45)))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(binding.AssetPath);
                        if (asset != null)
                            EditorGUIUtility.PingObject(asset);
                    }
                    EditorGUI.EndDisabledGroup();

                    if (GUILayout.Button("Delete", GUILayout.Width(60)))
                    {
                        removal = binding;
                    }
                }
            }

            if (removal != null)
            {
                machine.ManagedSheets.Remove(removal);
                EditorUtility.SetDirty(machine);
            }
        }

        private void EnsureManagedSheetList(ExcelMachine machine)
        {
            if (machine.ManagedSheets == null)
                machine.ManagedSheets = new List<ExcelMachine.SheetBinding>();
        }

        private ExcelMachine.SheetBinding GetOrCreateBinding(ExcelMachine machine, string sheetName)
        {
            EnsureManagedSheetList(machine);
            if (string.IsNullOrEmpty(sheetName))
                return null;

            var existing = machine.ManagedSheets.FirstOrDefault(b => b != null && string.Equals(b.SheetName, sheetName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                return existing;

            var binding = new ExcelMachine.SheetBinding { SheetName = sheetName };
            machine.ManagedSheets.Add(binding);
            EditorUtility.SetDirty(machine);
            return binding;
        }

        private void RegisterManagedSheet(ExcelMachine machine, string sheetName, string assetPath)
        {
            if (string.IsNullOrEmpty(sheetName))
                return;

            var binding = GetOrCreateBinding(machine, sheetName);
            if (binding == null)
                return;

            if (binding.AssetPath != assetPath)
            {
                binding.AssetPath = assetPath;
                EditorUtility.SetDirty(machine);
            }
        }

        private void MarkSheetStatus(ExcelMachine machine, string sheetName, string status)
        {
            if (machine == null || string.IsNullOrEmpty(sheetName))
                return;

            var binding = GetOrCreateBinding(machine, sheetName);
            if (binding == null)
                return;

            if (binding.LastStatus != status)
            {
                binding.LastStatus = status;
                EditorUtility.SetDirty(machine);
            }
        }

        private static string GetScriptableAssetRelativePath(ExcelMachine machine, string sheetName)
        {
            if (string.IsNullOrEmpty(machine.excelFilePath) || string.IsNullOrEmpty(sheetName))
                return string.Empty;

            string existingPath = FindExistingAssetPath(sheetName);
            if (!string.IsNullOrEmpty(existingPath))
                return existingPath;

            string directory = Path.GetDirectoryName(machine.excelFilePath);
            if (string.IsNullOrEmpty(directory))
                return string.Empty;

            string path = Path.Combine(directory, sheetName + ".asset");
            return path.Replace('\\', '/');
        }

        private static string FindExistingAssetPath(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return string.Empty;

            string[] guids = AssetDatabase.FindAssets(typeName + " t:ScriptableObject");
            foreach (var guid in guids)
            {
                string candidatePath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(candidatePath);
                if (asset != null && asset.GetType().Name == typeName)
                    return candidatePath;
            }

            return string.Empty;
        }

        private ScriptableObject LoadOrCreateScriptableAsset(string relativePath, Type containerType)
        {
            ScriptableObject asset = AssetDatabase.LoadAssetAtPath(relativePath, containerType) as ScriptableObject;
            if (asset != null)
                return asset;

            string absolutePath = ToAbsolutePath(relativePath);
            string folder = Path.GetDirectoryName(absolutePath);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            asset = ScriptableObject.CreateInstance(containerType);
            AssetDatabase.CreateAsset(asset, relativePath);
            AssetDatabase.ImportAsset(relativePath);
            return asset;
        }

        private static void SetStringField(ScriptableObject asset, string fieldName, string value)
        {
            var field = asset.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(string))
            {
                field.SetValue(asset, value);
            }
        }

        private static object InvokeDeserialize(ExcelQuery query, Type dataType)
        {
            MethodInfo method = typeof(ExcelQuery).GetMethod("Deserialize");
            if (method == null)
                return null;

            MethodInfo generic = method.MakeGenericMethod(dataType);
            return generic.Invoke(query, new object[] { 1 });
        }

        private static object InvokeToArray(object list)
        {
            if (list == null)
                return null;

            MethodInfo toArray = list.GetType().GetMethod("ToArray");
            if (toArray == null)
                return null;

            return toArray.Invoke(list, null);
        }

        private static bool SetDataArray(ScriptableObject asset, object array)
        {
            if (asset == null || array == null)
                return false;

            FieldInfo field = asset.GetType().GetField("dataArray", BindingFlags.Public | BindingFlags.Instance);
            if (field == null)
                return false;

            field.SetValue(asset, array);
            return true;
        }

        private static Type FindTypeByName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                    return type;

                try
                {
                    type = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
                    if (type != null)
                        return type;
                }
                catch (ReflectionTypeLoadException)
                {
                    // ignore assemblies that cannot load all types
                }
            }

            return null;
        }

        [MenuItem("★ACB/▶ Helper/▶QuickSheet Refresh", false, 30)]
        private static void RefreshAllManagedQuickSheetAssetsMenu()
        {
            RefreshAllManagedQuickSheetAssets();
        }

        public static void RefreshAllManagedQuickSheetAssets()
        {
            List<ExcelMachine> machines = LoadAllExcelMachines();
            if (machines.Count == 0)
            {
                Debug.Log("QuickSheet: ExcelMachine asset을 찾을 수 없습니다.");
                return;
            }

            int total = machines.Count;
            int success = 0;
            List<string> failures = new List<string>();

            foreach (var machine in machines)
            {
                if (machine == null)
                    continue;

                var editor = CreateEditor(machine) as ExcelMachineEditor;
                if (editor == null)
                {
                    failures.Add(machine.name + ": 에디터 생성 실패");
                    continue;
                }

                try
                {
                    if (editor.RefreshMachineAsset(machine))
                    {
                        success++;
                    }
                    else
                    {
                        failures.Add(machine.name + ": 리프레시 실패");
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(editor);
                }
            }

            string summary = string.Format("QuickSheet Refresh: 성공 {0}/{1}", success, total);
            if (failures.Count > 0)
            {
                summary += " / 실패: " + string.Join(", ", failures);
            }

            Debug.Log(summary);
        }

        private static List<ExcelMachine> LoadAllExcelMachines()
        {
            var machines = new List<ExcelMachine>();
            string[] guids = AssetDatabase.FindAssets("t:ExcelMachine");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var machine = AssetDatabase.LoadAssetAtPath<ExcelMachine>(path);
                if (machine != null)
                    machines.Add(machine);
            }

            return machines;
        }
    }
}