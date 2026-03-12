///////////////////////////////////////////////////////////////////////////////
///
/// ExcelMachine.cs
///
/// (c)2014 Kim, Hyoun Woo
///
///////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;

namespace UnityQuickSheet
{
    /// <summary>
    /// A class for various setting to read excel file and generated related script files.
    /// </summary>
    internal class ExcelMachine : BaseMachine
    {
        [Serializable]
        public class SheetBinding
        {
            public string SheetName;
            public string AssetPath;
            public string LastStatus;
        }

        /// <summary>
        /// Supported locations for table data.
        /// </summary>
        public enum DataSourceKind
        {
            ExcelFile,
            SpreadsheetUrl
        }

        /// <summary>
        /// Selected source kind (local excel file or remote spreadsheet).
        /// </summary>
        public DataSourceKind DataSource = DataSourceKind.ExcelFile;

        /// <summary>
        /// Convenience flag to check if spreadsheet URL should be used.
        /// </summary>
        public bool UsesSpreadsheet => DataSource == DataSourceKind.SpreadsheetUrl;

        /// <summary>
        /// where the .xls or .xlsx file is. The path should start with "Assets/".
        /// When DataSource is SpreadsheetUrl this holds the generated cache path.
        /// </summary>
        public string excelFilePath;

        /// <summary>
        /// Google Spreadsheet share URL to pull data from when DataSource is SpreadsheetUrl.
        /// </summary>
        public string spreadsheetUrl = string.Empty;

        /// <summary>
        /// Persistent list of worksheet-to-asset bindings that are synchronized on every refresh.
        /// </summary>
        public List<SheetBinding> ManagedSheets = new List<SheetBinding>();

        // both are needed for popup editor control.
        public string[] SheetNames = { "" };
        public int CurrentSheetIndex
        {
            get { return currentSelectedSheet; }
            set { currentSelectedSheet = value; }
        }

        [SerializeField]
        protected int currentSelectedSheet = 0;


        /// <summary>
        /// Note: Called when the asset file is created.
        /// </summary>

        private void Awake()
        {
            if (ExcelSettings.Instance != null)
            {
                // excel and google plugin have its own template files,
                // so we need to set the different path when the asset file is created.
                TemplatePath = ExcelSettings.Instance.TemplatePath;
            }
        }

        /// <summary>
        /// A menu item which create a 'ExcelMachine' asset file.
        /// </summary>
        [MenuItem("Assets/Create/QuickSheet/Tools/Excel")]
        public static void CreateScriptMachineAsset()
        {
            ExcelMachine inst = ScriptableObject.CreateInstance<ExcelMachine>();
            string path = CustomAssetUtility.GetUniqueAssetPathNameOrFallback(ImportSettingFilename);
            AssetDatabase.CreateAsset(inst, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = inst;
        }
    }
}