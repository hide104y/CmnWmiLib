using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.Versioning;
using CmnClsLib.Class;
using CmnClsLib.Interface;
using CmnClsLib.Module;

// 2026/08/08 Gemini 3.6 Flash (High) Review & Modified

namespace CmnWmiLib.Class
{
    // Windows専用クラス宣言
    [SupportedOSPlatform("windows")]

    /// <summary>
    /// WMI (Windows Management Instrumentation) 経由でシステム情報を取得するクラスです。
    /// </summary>
    /// <example>
    /// <code>
    /// ICmnLogger logger = new CmnLogger();
    /// ClsWmi wmi = new ClsWmi(logger)
    /// {
    ///     ClassName = "Win32_OperatingSystem",
    ///     KeyDictionary = new Dictionary&lt;string, string&gt; { { "Caption", "" }, { "Version", "" } }
    /// };
    /// int statusCode = wmi.FetchData();
    /// string caption = wmi.DataDictionary["Caption"];
    /// </code>
    /// </example>
    public class ClsWmi
    {
        private ICmnLogger _logger;

        /// <summary>
        /// <see cref="ClsWmi"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="logger">ログ出力を行うキャンバスロガーオブジェクト</param>
        /// <example>
        /// <code>
        /// ICmnLogger logger = new CmnLogger();
        /// ClsWmi wmi = new ClsWmi(logger);
        /// </code>
        /// </example>
        public ClsWmi(ICmnLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 接続対象のリモートホスト名またはIPアドレスを取得または設定します。
        /// </summary>
        public string RemoteHost { get; set; } = "";

        /// <summary>
        /// WMI接続時に使用するユーザー名を取得または設定します。
        /// </summary>
        public string Username { get; set; } = "";

        /// <summary>
        /// WMI接続時に使用するパスワードを取得または設定します。
        /// </summary>
        public string Password { get; set; } = "";

        /// <summary>
        /// 複数値取得時にデータを結合する区切り文字を取得または設定します。既定値はカンマ (",") です。
        /// </summary>
        public string Delimiter { get; set; } = ",";

        /// <summary>
        /// 取得対象の WMI クラス名を取得または設定します（例: "Win32_OperatingSystem"）。
        /// </summary>
        public string ClassName { get; set; } = "";

        /// <summary>
        /// WMI クエリの WHERE 句に指定する抽出条件を取得または設定します。
        /// </summary>
        public string Condition { get; set; } = "";

        /// <summary>
        /// ユーザー認証を切り替えて接続を行うかどうかを示す値を取得または設定します。
        /// </summary>
        public bool IsSwitchUser { get; set; } = false;

        /// <summary>
        /// ログ出力の出力量（詳細度レベル）を取得または設定します。
        /// </summary>
        public int Verbose { get; set; } = 0;

        /// <summary>
        /// 例外発生時にスタックトレースを出力するかどうかを示す値を取得または設定します。
        /// </summary>
        public bool IsStackTrace { get; set; } = false;

        /// <summary>
        /// 取得対象となる WMI プロパティキーとデフォルト値のペアを格納する辞書を取得または設定します。
        /// </summary>
        public Dictionary<string, string> KeyDictionary { get; set; } = new();

        /// <summary>
        /// WMI から取得した結果データを格納する辞書を取得または設定します。
        /// </summary>
        public Dictionary<string, string> DataDictionary { get; set; } = new();

        /// <summary>
        /// WMI経由でデータを取得します。（旧メソッド互換用）
        /// </summary>
        /// <returns>取得結果のステータスコード（正常終了時: MdlConst.LVL_I、例外発生時: MdlConst.LVL_E）</returns>
        /// <example>
        /// <code>
        /// var wmi = new ClsWmi(logger);
        /// int result = wmi.GetData();
        /// </code>
        /// </example>
        [Obsolete("代わりに 'FetchData()' を使用します。")]
        public int GetData()
        {
            return FetchData();
        }

        /// <summary>
        /// 設定された条件に基づいて WMI からデータを取得し、<see cref="DataDictionary"/> に格納します。
        /// </summary>
        /// <returns>取得結果のステータスコード（正常終了時: MdlConst.LVL_I、例外発生時: MdlConst.LVL_E）</returns>
        /// <example>
        /// <code>
        /// var wmi = new ClsWmi(logger)
        /// {
        ///     ClassName = "Win32_LogicalDisk",
        ///     Condition = "DriveType = 3",
        ///     KeyDictionary = new Dictionary&lt;string, string&gt; { { "Caption", "" }, { "Size", "0" } }
        /// };
        /// int status = wmi.FetchData();
        /// </code>
        /// </example>
        public int FetchData()
        {
            ConnectionOptions options = new ConnectionOptions();
            string providerPath = @"root\CIMv2";
            int resultCode = MdlConst.LVL_I;
            string query = string.IsNullOrEmpty(Condition)
                ? $"SELECT * FROM {ClassName}"
                : $"SELECT * FROM {ClassName} WHERE {Condition}";

            if (!string.IsNullOrEmpty(RemoteHost) &&
                !string.Equals(RemoteHost, "127.0.0.1", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(RemoteHost, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                providerPath = $@"\\{RemoteHost}\root\CIMv2";
                IsSwitchUser = true;
                if (Verbose > 6) _logger.WriteLine(MdlConst.LVL_NONE, $"[ClsWmi.FetchData()] ProviderPath = {providerPath}");
            }
            if (IsSwitchUser)
            {
                options.Username = Username;
                options.Password = Password;
                if (Verbose > 6) _logger.WriteLine(MdlConst.LVL_NONE, $"[ClsWmi.FetchData()] Username = {Username}");
                // セキュリティ保護のためパスワードのログ出力は実施しない
            }
            try
            {
                if (Verbose > 6) _logger.WriteLine(MdlConst.LVL_NONE, "[ClsWmi.FetchData()] new ManagementScope()");
                ManagementScope scope = new ManagementScope(providerPath, options);
                if (Verbose > 6) _logger.WriteLine(MdlConst.LVL_NONE, "[ClsWmi.FetchData()] scope.Connect()");
                scope.Connect();

                ObjectQuery objectQuery = new ObjectQuery(query);
                using ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, objectQuery);
                using ManagementObjectCollection collection = searcher.Get();

                void AppendValue(string key, string val)
                {
                    if (DataDictionary.TryGetValue(key, out string? existingValue))
                    {
                        DataDictionary[key] = $"{existingValue}{Delimiter}{val}";
                    }
                    else
                    {
                        DataDictionary[key] = val;
                    }
                }

                foreach (ManagementObject managementObject in collection)
                {
                    using (managementObject)
                    {
                        foreach (KeyValuePair<string, string> entry in KeyDictionary)
                        {
                            string valueString;
                            try
                            {
                                var valueObject = managementObject[entry.Key];
                                valueString = valueObject?.ToString() ?? entry.Value;

                                if (Verbose > 4) _logger.WriteLine(MdlConst.LVL_NONE, $"{entry.Key} = {valueString}");
                            }
                            catch
                            {
                                valueString = entry.Value;
                            }

                            AppendValue(entry.Key, valueString);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                resultCode = MdlConst.LVL_E;
                _logger.WriteLine(MdlConst.LVL_NONE, $"[ClsWmi.FetchData()] EXCEPTION : {ex.Message}");
                if (IsStackTrace)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                    _logger.WriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? "");
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                }
            }
            return resultCode;
        }
    }
}
