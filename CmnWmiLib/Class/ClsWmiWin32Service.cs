using System;
using System.Management;
using System.Collections.Generic;
using System.Threading;
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
    /// WMI を使用して Win32_Service (Windows サービス) の状態取得・開始・停止・スタートアップモード変更を行うクラスです。
    /// </summary>
    public class ClsWmiWin32Service
    {
        private ICmnLogger _logger;
        private string _remoteHost = "127.0.0.1";
        private string _serviceName = "";
        private string _action = "status";
        private string _startMode = "";
        private string _username = "";
        private string _password = "";
        private bool _isAction = false;
        private bool _isSync = false;
        private bool _isIfExist = false;
        private bool _isIfAuto = false;
        private bool _isExist = false;
        private bool _isSwitchUser = false;
        private bool _isStackTrace = false;
        private bool _isSilent = false;
        private int _verbose = 0;
        private int _timeout = 60;
        private int _retCodeAtTimeout = MdlConst.LVL_E;
        private readonly Dictionary<uint, string> _messageDic = [];
        private Dictionary<string, string> _services = [];
        private bool _isLogWrite = false;
        private readonly List<string> _listNames = [];

        /// <summary>
        /// <see cref="ClsWmiWin32Service"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="logger">ログ出力に使用するロガーインスタンス</param>
        /// <example>
        /// <code>
        /// ICmnLogger logger = new ClsLogger();
        /// var serviceManager = new ClsWmiWin32Service(logger);
        /// </code>
        /// </example>
        public ClsWmiWin32Service(ICmnLogger logger)
        {
            _logger = logger;
            Initialize();
        }

        /// <summary>接続対象のリモートホスト名または IP アドレスを取得または設定します。デフォルトは "127.0.0.1" です。</summary>
        public string RemoteHost { get => _remoteHost; set => _remoteHost = value; }

        /// <summary>リモート接続時のユーザー名を取得または設定します。</summary>
        public string Username { get => _username; set => _username = value; }

        /// <summary>リモート接続時のパスワードを取得または設定します。</summary>
        public string Password { get => _password; set => _password = value; }

        /// <summary>ユーザー切り替えを行うかどうかを示すフラグを取得または設定します。</summary>
        public bool IsSwitchUser { get => _isSwitchUser; set => _isSwitchUser = value; }

        /// <summary>対象となるサービス名または表示名を取得または設定します。'%' を含めることでワイルドカード検索が可能です。</summary>
        public string ServiceName { get => _serviceName; set => _serviceName = value; }

        /// <summary>実行するアクション ("start", "stop", "status", "running", "stopped", "mode", "none") を取得または設定します。</summary>
        public string Action { get => _action; set => _action = value; }

        /// <summary>詳細ログレベル (0〜7) を取得または設定します。</summary>
        public int Verbose { get => _verbose; set => _verbose = value; }

        /// <summary>同期モード処理時のタイムアウト時間（秒）を取得または設定します。デフォルトは 60 です。</summary>
        public int Timeout { get => _timeout; set => _timeout = value; }

        /// <summary>タイムアウトが発生した際に返却するコードを取得または設定します。</summary>
        public int RetCodeAtTimeout { get => _retCodeAtTimeout; set => _retCodeAtTimeout = value; }

        /// <summary>サービス状態の変更を同期的に待機するかどうかを示すフラグを取得または設定します。</summary>
        public bool IsSync { get => _isSync; set => _isSync = value; }

        /// <summary>サービスが存在する場合のみ処理を継続し、存在しない場合はスキップ（成功扱いに）するかどうかを示すフラグを取得または設定します。</summary>
        public bool IsIfExist { get => _isIfExist; set => _isIfExist = value; }

        /// <summary>スタートアップモードが Auto の場合のみサービスを開始するかどうかを示すフラグを取得または設定します。</summary>
        public bool IsIfAuto { get => _isIfAuto; set => _isIfAuto = value; }

        /// <summary>検索したサービスが存在したかどうかを示すフラグを取得または設定します。</summary>
        public bool IsExist { get => _isExist; set => _isExist = value; }

        /// <summary>例外発生時にスタックトレースを出力するかどうかを示すフラグを取得または設定します。</summary>
        public bool IsStackTrace { get => _isStackTrace; set => _isStackTrace = value; }

        /// <summary>サイレントモード（ログメッセージを出力しない）にするかどうかを示すフラグを取得または設定します。</summary>
        public bool IsSilent { get => _isSilent; set => _isSilent = value; }

        /// <summary>取得したサービスの一覧（サービス名と表示名の辞書）を取得または設定します。</summary>
        public Dictionary<string, string> Services { get => _services; set => _services = value; }

        /// <summary>変更対象のスタートアップモード ("Automatic", "Manual", "Disabled") を取得または設定します。</summary>
        public string StartMode
        {
            get => _startMode;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _startMode = "";
                }
                else
                {
                    _startMode = value.ToLowerInvariant() switch
                    {
                        "automatic" or "auto" or "a" => "Automatic",
                        "manual" or "man" or "m" => "Manual",
                        "disabled" or "dis" or "d" => "Disabled",
                        _ => ""
                    };
                }
            }
        }

        /// <summary>ログ書き込みを有効化するかどうかを示すフラグを取得または設定します。</summary>
        public bool IsLogWrite { get => _isLogWrite; set => _isLogWrite = value; }

        /// <summary>
        /// WMI サービス操作時のリターンコードに対するメッセージ辞書を初期化します。
        /// </summary>
        /// <example>
        /// <code>
        /// serviceManager.Initialize();
        /// </code>
        /// </example>
        public void Initialize()
        {
            // https://msdn.microsoft.com/en-us/library/aa384901(v=vs.85).aspx
            _messageDic[0] = "The request was accepted.";
            _messageDic[1] = "The request is not supported.";
            _messageDic[2] = "The user did not have the necessary access.";
            _messageDic[3] = "The service cannot be stopped because other services that are running are dependent on it.";
            _messageDic[4] = "The requested control code is not valid, or it is unacceptable to the service.";
            _messageDic[5] = "The requested control code cannot be sent to the service because the state of the service (Win32_BaseService State property) is equal to 0, 1, or 2.";
            _messageDic[6] = "The service has not been started.";
            _messageDic[7] = "The service did not respond to the start request in a timely fashion.";
            _messageDic[8] = "Unknown failure when starting the service.";
            _messageDic[9] = "The directory path to the service executable file was not found.";
            _messageDic[10] = "The service is already running.";
            _messageDic[11] = "The database to add a new service is locked.";
            _messageDic[12] = "A dependency this service relies on has been removed from the system.";
            _messageDic[13] = "The service failed to find the service needed from a dependent service.";
            _messageDic[14] = "The service has been disabled from the system.";
            _messageDic[15] = "The service does not have the correct authentication to run on the system.";
            _messageDic[16] = "This service is being removed from the system.";
            _messageDic[17] = "The service has no execution thread.";
            _messageDic[18] = "The service has circular dependencies when it starts.";
            _messageDic[19] = "A service is running under the same name.";
            _messageDic[20] = "The service name has invalid characters.";
            _messageDic[21] = "Invalid parameters have been passed to the service.";
            _messageDic[22] = "The account under which this service runs is either invalid or lacks the permissions to run the service.";
            _messageDic[23] = "The service exists in the database of services available from the system.";
            _messageDic[24] = "The service is currently paused in the system.";
        }
        [Obsolete("代わりに 'Initialize()' を使用します。")]
        public void Init()
        {
            Initialize();
        }

        /// <summary>
        /// (非推奨) 停止している自動開始サービスのリストを取得します。代わりに <see cref="GetStoppedAutoServices(string)"/> を使用してください。
        /// </summary>
        /// <param name="serviceName">検索対象のサービス名</param>
        /// <returns>操作結果レベル (MdlConst.LVL_I / LVL_E)</returns>
        [Obsolete("代わりに 'GetStoppedAutoServices(string)' を使用します。")]
        public int GetDownServicesList(string serviceName) => GetStoppedAutoServices(serviceName);

        /// <summary>
        /// 指定されたサービス名に基づいて停止している自動開始（Auto）サービスのリストを取得します。
        /// </summary>
        /// <param name="serviceName">検索対象のサービス名（'%' ワイルドカード指定可）</param>
        /// <returns>操作結果を示すレベル値 (MdlConst.LVL_I: 成功, MdlConst.LVL_E: エラー)</returns>
        /// <example>
        /// <code>
        /// var serviceManager = new ClsWmiWin32Service(logger);
        /// int result = serviceManager.GetStoppedAutoServices("WSearch%");
        /// </code>
        /// </example>
        public int GetStoppedAutoServices(string serviceName)
        {
            var options = new ConnectionOptions();
            string providerPath = @"root\CIMv2";
            int result = MdlConst.LVL_E;
            _services.Clear();

            string query = "SELECT * FROM Win32_Service WHERE StartMode = 'Auto' and State = 'Stopped'";
            if (!string.IsNullOrEmpty(serviceName))
            {
                if (!string.IsNullOrEmpty(_serviceName) && _serviceName.Contains('%'))
                {
                    query += $" AND (Name LIKE '{_serviceName}' OR DisplayName LIKE '{_serviceName}')";
                }
                else
                {
                    query += $" AND (Name = '{_serviceName}' OR DisplayName = '{_serviceName}')";
                }
            }

            if (!_remoteHost.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) && !_remoteHost.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                providerPath = $@"\\{_remoteHost}\root\CIMv2";
                _isSwitchUser = true;
                if (_verbose > 6) LogWriteLine(MdlConst.LVL_NONE, $"[ClsWmiWin32Service.QueryDownService()] ProviderPath = {providerPath}");
            }
            if (_isSwitchUser)
            {
                options.Username = _username;
                options.Password = _password;
                if (_verbose > 6) LogWriteLine(MdlConst.LVL_NONE, $"[ClsWmiWin32Service.QueryDownService()] Username = {_username}");
                if (_verbose > 6) LogWriteLine(MdlConst.LVL_NONE, $"[ClsWmiWin32Service.QueryDownService()] Password = {_password}");
            }

            try
            {
                if (_verbose > 6) LogWriteLine(MdlConst.LVL_NONE, "[ClsWmiWin32Service.QueryDownService()] new ManagementScope()");
                var ms = new ManagementScope(providerPath, options);
                if (_verbose > 6) LogWriteLine(MdlConst.LVL_NONE, "[ClsWmiWin32Service.QueryDownService()] ms.Connect()");
                ms.Connect();
                if (_verbose > 2) LogWriteLine(MdlConst.LVL_NONE, query);

                var oq = new ObjectQuery(query);
                using var mos = new ManagementObjectSearcher(ms, oq);
                foreach (ManagementObject mo in mos.Get())
                {
                    using (mo)
                    {
                        string name = mo["Name"]?.ToString()?.Trim() ?? "";
                        string displayName = mo["DisplayName"]?.ToString()?.Trim() ?? "";
                        if (_verbose > 4) LogWriteLine(MdlConst.LVL_NONE, $"HIT : {displayName} : {name}");
                        _services[name] = displayName;
                    }
                }
                result = MdlConst.LVL_I;
            }
            catch (Exception ex)
            {
                LogWriteLine(MdlConst.LVL_NONE, $"[ClsWmiWin32Service.GetStoppedAutoServices()] EXCEPTION : {ex.Message}");
                if (_isStackTrace)
                {
                    LogWriteLine(MdlConst.LVL_NONE, "");
                    LogWriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? "");
                    LogWriteLine(MdlConst.LVL_NONE, "");
                }
            }
            return result;
        }

        /// <summary>
        /// 設定された条件（ServiceName, Action, StartMode など）に従い、サービス状態の確認・変更およびスタートアップモードの変更を実行します。
        /// </summary>
        /// <returns>操作結果を示すレベル値 (MdlConst.LVL_I: 成功, MdlConst.LVL_W: 警告, MdlConst.LVL_E: エラー)</returns>
        /// <example>
        /// <code>
        /// var serviceManager = new ClsWmiWin32Service(logger)
        /// {
        ///     ServiceName = "Spooler",
        ///     Action = "start",
        ///     IsSync = true
        /// };
        /// int result = serviceManager.Execute();
        /// </code>
        /// </example>
        public int Execute()
        {
            var options = new ConnectionOptions();
            string providerPath = @"root\CIMv2";
            int result = MdlConst.LVL_E;
            bool isFailedChangeServiceMode = false;

            if (!_remoteHost.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) && !_remoteHost.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                providerPath = $@"\\{_remoteHost}\root\CIMv2";
                _isSwitchUser = true;
                if (_verbose > 6) LogWriteLine(MdlConst.LVL_NONE, $"[ClsWmiWin32Service.Execute()] ProviderPath = {providerPath}");
            }
            if (_isSwitchUser)
            {
                options.Username = _username;
                options.Password = _password;
                if (_verbose > 6) LogWriteLine(MdlConst.LVL_NONE, $"[ClsWmiWin32Service.Execute()] Username = {_username}");
                if (_verbose > 6) LogWriteLine(MdlConst.LVL_NONE, $"[ClsWmiWin32Service.Execute()] Password = {_password}");
            }

            try
            {
                if (_verbose > 6) LogWriteLine(MdlConst.LVL_NONE, "[ClsWmiWin32Service.Execute()] new ManagementScope()");
                var ms = new ManagementScope(providerPath, options);
                if (_verbose > 6) LogWriteLine(MdlConst.LVL_NONE, "[ClsWmiWin32Service.Execute()] ms.Connect()");
                ms.Connect();

                string query = _serviceName.Contains('%')
                    ? $"SELECT * FROM Win32_Service WHERE Name LIKE '{_serviceName}' Or DisplayName LIKE '{_serviceName}'"
                    : $"SELECT * FROM Win32_Service WHERE Name = '{_serviceName}' Or DisplayName = '{_serviceName}'";

                if (_verbose > 2) LogWriteLine(MdlConst.LVL_NONE, query);

                var oq = new ObjectQuery(query);
                using var mos = new ManagementObjectSearcher(ms, oq);
                int serviceCount = 0;

                // サービスの起動モードを「Manual」「Disabled」に変更する場合
                if (!string.IsNullOrEmpty(_startMode))
                {
                    string modeLower = _startMode.ToLowerInvariant();
                    if (modeLower is "manual" or "disabled")
                    {
                        foreach (ManagementObject mo in mos.Get())
                        {
                            using (mo)
                            {
                                if (MdlConst.LVL_I != ChangeStartupMode(mo))
                                {
                                    isFailedChangeServiceMode = true;
                                }
                            }
                        }
                    }
                }

                // サービス起動／停止
                foreach (ManagementObject mo in mos.Get())
                {
                    using (mo)
                    {
                        serviceCount++;
                        result = EvaluateStatus(mo);
                        if (_isAction)
                        {
                            if (_isIfAuto && string.Equals("start", _action, StringComparison.OrdinalIgnoreCase))
                            {
                                string startMode = GetStartMode(mo);
                                if (string.Equals(startMode, "auto", StringComparison.OrdinalIgnoreCase))
                                {
                                    result = ChangeServiceState(mo);
                                    if (!_isSync && result == MdlConst.LVL_I) result = MdlConst.LVL_W;
                                }
                                else
                                {
                                    LogWriteLine(MdlConst.LVL_NONE, $"-- : SKIP => STARTUP MODE IS NOT AUTO : {startMode}");
                                    result = MdlConst.LVL_I;
                                    _isAction = false;
                                }
                            }
                            else
                            {
                                result = ChangeServiceState(mo);
                            }
                        }
                    }
                }

                // サービスが存在する場合
                if (serviceCount > 0)
                {
                    _isExist = true;
                    // 同期モードの場合：サービスの状態を変更結果確認
                    if (_isAction && _isSync && result == MdlConst.LVL_I)
                    {
                        string name = "";
                        string state = "";
                        bool isBreak = false;
                        _listNames.Clear();
                        for (int i = 0; i < _timeout; i++)
                        {
                            int services = 0;
                            Thread.Sleep(1000);
                            foreach (ManagementObject mo in mos.Get())
                            {
                                using (mo)
                                {
                                    services++;
                                    name = mo.Properties["Name"]?.Value?.ToString()?.Trim() ?? "";
                                    if (!_listNames.Contains(name))
                                    {
                                        state = mo.Properties["State"]?.Value?.ToString()?.Trim()?.ToLowerInvariant() ?? "";
                                        if (string.Equals("start", _action, StringComparison.OrdinalIgnoreCase) && state.Equals("running", StringComparison.OrdinalIgnoreCase))
                                        {
                                            _listNames.Add(name);
                                            LogWriteLine(MdlConst.LVL_NONE, $"OK : SERVICE[{name}] IS RUNNING (確認回数={i + 1})");
                                        }
                                        if (string.Equals("stop", _action, StringComparison.OrdinalIgnoreCase) && state.Equals("stopped", StringComparison.OrdinalIgnoreCase))
                                        {
                                            _listNames.Add(name);
                                            LogWriteLine(MdlConst.LVL_NONE, $"OK : SERVICE[{name}] IS STOPPED (確認回数={i + 1})");
                                        }
                                    }
                                }
                            }
                            if (_listNames.Count == services)
                            {
                                isBreak = true;
                                break;
                            }
                        }
                        // タイムアウトした場合
                        if (!isBreak)
                        {
                            LogWriteLine(MdlConst.LVL_NONE, $"NG : SYNC TIMEOUT (確認回数={_timeout}) => CURRENT STATE IS {state.ToUpperInvariant()}");
                            if (result != MdlConst.LVL_E) result = _retCodeAtTimeout;
                        }
                    }

                    // サービスの起動モードを「Automatic」に変更する場合、または「失敗フラグ（isFailedChangeServiceMode）」が立っている場合
                    if (!string.IsNullOrEmpty(_startMode))
                    {
                        bool isChangeServiceMode = string.Equals(_startMode, "automatic", StringComparison.OrdinalIgnoreCase) || isFailedChangeServiceMode;
                        if (isChangeServiceMode)
                        {
                            foreach (ManagementObject mo in mos.Get())
                            {
                                using (mo)
                                {
                                    if (MdlConst.LVL_I != ChangeStartupMode(mo))
                                    {
                                        if (result != MdlConst.LVL_E) result = MdlConst.LVL_W;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    _isExist = false;
                    if (_isIfExist)
                    {
                        LogWriteLine(MdlConst.LVL_NONE, $"SKIP : NO SUCH A SERVICE : {_serviceName}");
                        result = MdlConst.LVL_I;
                    }
                    else
                    {
                        LogWriteLine(MdlConst.LVL_NONE, $"NG : NO SUCH A SERVICE : {_serviceName}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriteLine(MdlConst.LVL_NONE, $"[ClsWmiWin32Service.Execute()] EXCEPTION : {ex.Message}");
                if (_isStackTrace)
                {
                    LogWriteLine(MdlConst.LVL_NONE, "");
                    LogWriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? "");
                    LogWriteLine(MdlConst.LVL_NONE, "");
                }
            }
            return result;
        }

        /// <summary>
        /// 指定された WMI 管理オブジェクトからサービスの状態（"Running", "Stopped" 等）を取得します。
        /// </summary>
        /// <param name="mo">Win32_Service を表す ManagementObject</param>
        /// <returns>サービスの状態文字列（取得できない場合は空文字列）</returns>
        /// <example>
        /// <code>
        /// string status = serviceManager.GetStatus(mo);
        /// </code>
        /// </example>
        public string GetStatus(ManagementObject mo)
        {
            return mo.Properties["State"]?.Value?.ToString()?.Trim() ?? "";
        }

        /// <summary>
        /// (非推奨) サービスの状態を小文字で取得します。代わりに <see cref="GetServiceStatusLowerCase(ManagementObject)"/> を使用してください。
        /// </summary>
        /// <param name="mo">管理オブジェクト</param>
        /// <returns>小文字のサービス状態文字列</returns>
        [Obsolete("代わりに 'GetServiceStatusLowerCase(ManagementObject)' を使用します。")]
        public string GetStatusToLower(ManagementObject mo) => GetServiceStatusLowerCase(mo);

        /// <summary>
        /// 指定された WMI 管理オブジェクトからサービスの状態を小文字の文字列で取得します。
        /// </summary>
        /// <param name="mo">Win32_Service を表す ManagementObject</param>
        /// <returns>小文字のサービス状態文字列（例: "running", "stopped"）</returns>
        /// <example>
        /// <code>
        /// string statusLower = serviceManager.GetServiceStatusLowerCase(mo);
        /// </code>
        /// </example>
        public string GetServiceStatusLowerCase(ManagementObject mo)
        {
            return mo.Properties["State"]?.Value?.ToString()?.Trim()?.ToLowerInvariant() ?? "";
        }

        /// <summary>
        /// 指定された WMI 管理オブジェクトからサービスのスタートアップモード（"Auto", "Manual", "Disabled" 等）を取得します。
        /// </summary>
        /// <param name="mo">Win32_Service を表す ManagementObject</param>
        /// <returns>サービスのスタートアップモード文字列</returns>
        /// <example>
        /// <code>
        /// string startMode = serviceManager.GetStartMode(mo);
        /// </code>
        /// </example>
        public string GetStartMode(ManagementObject mo)
        {
            return mo.Properties["StartMode"]?.Value?.ToString()?.Trim() ?? "";
        }

        /// <summary>
        /// (非推奨) サービスの開始モードを小文字で取得します。代わりに <see cref="GetStartModeLowerCase(ManagementObject)"/> を使用してください。
        /// </summary>
        /// <param name="mo">管理オブジェクト</param>
        /// <returns>小文字のスタートアップモード文字列</returns>
        [Obsolete("代わりに 'GetStartModeLowerCase(ManagementObject)' を使用します。")]
        public string GetStartModeToLower(ManagementObject mo) => GetStartModeLowerCase(mo);

        /// <summary>
        /// 指定された WMI 管理オブジェクトからサービスのスタートアップモードを小文字の文字列で取得します。
        /// </summary>
        /// <param name="mo">Win32_Service を表す ManagementObject</param>
        /// <returns>小文字のスタートアップモード文字列（例: "auto", "manual", "disabled"）</returns>
        /// <example>
        /// <code>
        /// string startModeLower = serviceManager.GetStartModeLowerCase(mo);
        /// </code>
        /// </example>
        public string GetStartModeLowerCase(ManagementObject mo)
        {
            return mo.Properties["StartMode"]?.Value?.ToString()?.Trim()?.ToLowerInvariant() ?? "";
        }

        /// <summary>
        /// (非推奨) サービスの状態を評価します。代わりに <see cref="EvaluateStatus(ManagementObject, string)"/> を使用してください。
        /// </summary>
        /// <param name="mo">管理オブジェクト</param>
        /// <param name="name">サービス名</param>
        /// <returns>評価結果コード</returns>
        [Obsolete("代わりに 'EvaluateStatus(ManagementObject, string)' を使用します。")]
        public int EvalStatus(ManagementObject mo, string name) => EvaluateStatus(mo, name);

        /// <summary>
        /// 設定されている <see cref="Action"/> に応じてサービスの状態を評価し、ログ出力およびアクション要否フラグの設定を行います。
        /// </summary>
        /// <param name="mo">Win32_Service を表す ManagementObject</param>
        /// <param name="name">評価対象のサービス名</param>
        /// <returns>評価結果コード (MdlConst.LVL_I: 正常状態, MdlConst.LVL_W: 警告状態, MdlConst.LVL_E: 継続処理要)</returns>
        /// <example>
        /// <code>
        /// int statusResult = serviceManager.EvaluateStatus(mo, "Spooler");
        /// </code>
        /// </example>
        public int EvaluateStatus(ManagementObject mo, string name)
        {
            int result = MdlConst.LVL_E;
            string state = GetStatus(mo);
            string stateToLower = state.ToLowerInvariant();

            _isAction = false;

            switch (_action.ToLowerInvariant())
            {
                case "start":
                    if (stateToLower.Equals("running", StringComparison.OrdinalIgnoreCase))
                    {
                        LogWriteLine(MdlConst.LVL_NONE, $"OK : SERVICE[{name}] IS ALLREADY RUNNING");
                        result = MdlConst.LVL_I;
                    }
                    else
                    {
                        LogWriteLine(MdlConst.LVL_NONE, $"-- : SERVICE[{name}] CURRENT STATE IS {state.ToUpperInvariant()}");
                        _isAction = true;
                    }
                    break;

                case "stop":
                    if (stateToLower.Equals("stopped", StringComparison.OrdinalIgnoreCase))
                    {
                        LogWriteLine(MdlConst.LVL_NONE, $"OK : SERVICE[{name}] IS ALLREADY STOPPED");
                        result = MdlConst.LVL_I;
                    }
                    else
                    {
                        LogWriteLine(MdlConst.LVL_NONE, $"-- : SERVICE[{name}] CURRENT STATE IS {state.ToUpperInvariant()}");
                        _isAction = true;
                    }
                    break;

                case "status":
                    LogWriteLine(MdlConst.LVL_NONE, $"SERVICE[{name}] IS {state.ToUpperInvariant()}");
                    result = MdlConst.LVL_I;
                    break;

                case "running":
                    if (stateToLower.Equals("running", StringComparison.OrdinalIgnoreCase))
                    {
                        LogWriteLine(MdlConst.LVL_NONE, $"SERVICE[{name}] IS {state.ToUpperInvariant()}");
                        result = MdlConst.LVL_I;
                    }
                    else
                    {
                        if (_isIfAuto)
                        {
                            string startMode = GetStartMode(mo);
                            if (startMode.Equals("auto", StringComparison.OrdinalIgnoreCase))
                            {
                                LogWriteLine(MdlConst.LVL_NONE, $"SERVICE[{name}] IS {state.ToUpperInvariant()} : STARTUP MODE IS {startMode.ToUpperInvariant()}");
                            }
                            else
                            {
                                LogWriteLine(MdlConst.LVL_NONE, $"SERVICE[{name}] IS {state.ToUpperInvariant()} : STARTUP MODE IS NOT AUTO ({startMode.ToUpperInvariant()})");
                                result = MdlConst.LVL_W;
                            }
                        }
                        else
                        {
                            LogWriteLine(MdlConst.LVL_NONE, $"SERVICE[{name}] IS {state.ToUpperInvariant()}");
                        }
                    }
                    break;

                case "stopped":
                    LogWriteLine(MdlConst.LVL_NONE, $"SERVICE[{name}] IS {state.ToUpperInvariant()}");
                    if (stateToLower.Equals("stopped", StringComparison.OrdinalIgnoreCase)) result = MdlConst.LVL_I;
                    break;

                case "mode":
                    string mode = GetStartMode(mo);
                    LogWriteLine(MdlConst.LVL_NONE, $"SERVICE[{name}] IS {state.ToUpperInvariant()} : STARTUP MODE IS {mode.ToUpperInvariant()}");
                    break;

                case "none":
                    result = MdlConst.LVL_I;
                    break;
            }
            return result;
        }

        /// <summary>
        /// (非推奨) サービスの状態を評価します。代わりに <see cref="EvaluateStatus(ManagementObject)"/> を使用してください。
        /// </summary>
        /// <param name="mo">管理オブジェクト</param>
        /// <returns>評価結果コード</returns>
        [Obsolete("代わりに 'EvaluateStatus(ManagementObject)' を使用します。")]
        public int EvalStatus(ManagementObject mo) => EvaluateStatus(mo);

        /// <summary>
        /// WMI オブジェクトからサービス名を取得し、設定されている <see cref="Action"/> に応じてサービスの状態を評価します。
        /// </summary>
        /// <param name="mo">Win32_Service を表す ManagementObject</param>
        /// <returns>評価結果コード</returns>
        /// <example>
        /// <code>
        /// int statusResult = serviceManager.EvaluateStatus(mo);
        /// </code>
        /// </example>
        public int EvaluateStatus(ManagementObject mo)
        {
            string name = mo.Properties["Name"]?.Value?.ToString()?.Trim() ?? "";
            return EvaluateStatus(mo, name);
        }

        /// <summary>
        /// WMI メソッド (StartService / StopService) を呼び出し、サービスの状態を変更します。
        /// </summary>
        /// <param name="mo">Win32_Service を表す ManagementObject</param>
        /// <param name="name">サービス名</param>
        /// <returns>操作結果コード (MdlConst.LVL_I: 成功, MdlConst.LVL_E: エラー)</returns>
        /// <example>
        /// <code>
        /// serviceManager.Action = "start";
        /// int changeResult = serviceManager.ChangeServiceState(mo, "Spooler");
        /// </code>
        /// </example>
        public int ChangeServiceState(ManagementObject mo, string name)
        {
            int result = MdlConst.LVL_E;
            try
            {
                string serviceAction = _action.ToLowerInvariant() switch
                {
                    "start" => "StartService",
                    "stop" => "StopService",
                    _ => ""
                };

                if (string.IsNullOrEmpty(serviceAction))
                {
                    return result;
                }

                ManagementBaseObject outParams = mo.InvokeMethod(serviceAction, null, null);
                string returnValueString = outParams?["ReturnValue"]?.ToString() ?? "";
                uint returnValue = Convert.ToUInt32(outParams?["ReturnValue"] ?? 999999);

                switch (returnValue)
                {
                    case 0:
                        LogWriteLine(MdlConst.LVL_NONE, $"OK : SERVICE[{name}] {serviceAction.ToUpperInvariant()} (WMI:{returnValueString})");
                        result = MdlConst.LVL_I;
                        break;
                    case 5:
                        LogWriteLine(MdlConst.LVL_NONE, $"OK : SERVICE[{name}] IS ALLREADY STOPPED (WMI:{returnValueString})");
                        result = MdlConst.LVL_I;
                        break;
                    case 10:
                        LogWriteLine(MdlConst.LVL_NONE, $"OK : SERVICE[{name}] IS ALLREADY RUNNING (WMI:{returnValueString})");
                        result = MdlConst.LVL_I;
                        break;
                    default:
                        if (_messageDic.TryGetValue(returnValue, out string? message))
                        {
                            LogWriteLine(MdlConst.LVL_NONE, $"NG : SERVICE[{name}] {message.ToUpperInvariant()} (WMI:{returnValueString})");
                        }
                        else
                        {
                            LogWriteLine(MdlConst.LVL_NONE, $"NG : SERVICE[{name}] OTHER ERROR (WMI:{returnValueString})");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                LogWriteLine(MdlConst.LVL_NONE, $"[ClsWmiWin32Service.ChangeServiceState()] SERVICE[{name}] EXCEPTION : {ex.Message}");
                if (_isStackTrace)
                {
                    LogWriteLine(MdlConst.LVL_NONE, "");
                    LogWriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? "");
                    LogWriteLine(MdlConst.LVL_NONE, "");
                }
            }
            return result;
        }

        /// <summary>
        /// WMI オブジェクトからサービス名を取得し、サービスの状態を変更します。
        /// </summary>
        /// <param name="mo">Win32_Service を表す ManagementObject</param>
        /// <returns>操作結果コード</returns>
        /// <example>
        /// <code>
        /// int changeResult = serviceManager.ChangeServiceState(mo);
        /// </code>
        /// </example>
        public int ChangeServiceState(ManagementObject mo)
        {
            string name = mo.Properties["Name"]?.Value?.ToString()?.Trim() ?? "";
            return ChangeServiceState(mo, name);
        }

        /// <summary>
        /// WMI メソッド (ChangeStartMode) を呼び出し、サービスのスタートアップモード ("Automatic", "Manual", "Disabled") を変更します。
        /// </summary>
        /// <param name="mo">Win32_Service を表す ManagementObject</param>
        /// <param name="name">サービス名</param>
        /// <returns>操作結果コード (MdlConst.LVL_I: 成功, MdlConst.LVL_E: エラー)</returns>
        /// <example>
        /// <code>
        /// serviceManager.StartMode = "Automatic";
        /// int modeResult = serviceManager.ChangeStartupMode(mo, "Spooler");
        /// </code>
        /// </example>
        public int ChangeStartupMode(ManagementObject mo, string name)
        {
            int result = MdlConst.LVL_I;
            if (!string.IsNullOrEmpty(_startMode))
            {
                try
                {
                    bool isExecutionNeeded = true;
                    string currentStartupMode = GetStartMode(mo);
                    LogWriteLine(MdlConst.LVL_NONE, $"-- : SERVICE[{name}] CURRENT STARTUP MODE IS {currentStartupMode}");

                    if (string.Equals(_startMode, "automatic", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(currentStartupMode, "auto", StringComparison.OrdinalIgnoreCase))
                        {
                            LogWriteLine(MdlConst.LVL_NONE, $"OK : SERVICE[{name}] STARTUP MODE IS ALLREADY AUTOMATIC");
                            isExecutionNeeded = false;
                        }
                    }
                    else
                    {
                        if (string.Equals(currentStartupMode, "manual", StringComparison.OrdinalIgnoreCase))
                        {
                            LogWriteLine(MdlConst.LVL_NONE, $"OK : SERVICE[{name}] STARTUP MODE IS ALLREADY MANUAL");
                            isExecutionNeeded = false;
                        }
                    }

                    if (isExecutionNeeded)
                    {
                        ManagementBaseObject inParams = mo.GetMethodParameters("ChangeStartMode");
                        inParams["StartMode"] = _startMode;
                        ManagementBaseObject outParams = mo.InvokeMethod("ChangeStartMode", inParams, null);
                        string? returnValueString = outParams?["ReturnValue"]?.ToString();
                        uint returnValue = Convert.ToUInt32(outParams?["ReturnValue"] ?? 999999);

                        switch (returnValue)
                        {
                            case 0:
                                LogWriteLine(MdlConst.LVL_NONE, $"OK : SERVICE[{name}] STARTUP MODE CHANGE : MODE = {_startMode} (WMI:{returnValueString})");
                                break;
                            default:
                                result = MdlConst.LVL_E;
                                if (_messageDic.TryGetValue(returnValue, out string? message))
                                {
                                    LogWriteLine(MdlConst.LVL_NONE, $"NG : SERVICE[{name}] STARTUP MODE CHANGE : {message.ToUpperInvariant()} (WMI:{returnValueString})");
                                }
                                else
                                {
                                    LogWriteLine(MdlConst.LVL_NONE, $"NG : SERVICE[{name}] STARTUP MODE CHANGE : OTHER ERROR (WMI:{returnValueString})");
                                }
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    result = MdlConst.LVL_E;
                    LogWriteLine(MdlConst.LVL_NONE, $"[ClsWmiWin32Service.ChangeStartupMode()] SERVICE[{name}] EXCEPTION : {ex.Message}");
                    if (_isStackTrace)
                    {
                        LogWriteLine(MdlConst.LVL_NONE, "");
                        LogWriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? "");
                        LogWriteLine(MdlConst.LVL_NONE, "");
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// WMI オブジェクトからサービス名を取得し、サービスのスタートアップモードを変更します。
        /// </summary>
        /// <param name="mo">Win32_Service を表す ManagementObject</param>
        /// <returns>操作結果コード</returns>
        /// <example>
        /// <code>
        /// int modeResult = serviceManager.ChangeStartupMode(mo);
        /// </code>
        /// </example>
        public int ChangeStartupMode(ManagementObject mo)
        {
            string name = mo.Properties["Name"]?.Value?.ToString()?.Trim() ?? "";
            return ChangeStartupMode(mo, name);
        }

        /// <summary>
        /// ロガーを通じてログメッセージを 1 行出力します（サイレントモードでなく、コンソール出力が有効な場合）。
        /// </summary>
        /// <param name="level">ログレベル</param>
        /// <param name="message">出力するログメッセージ</param>
        /// <example>
        /// <code>
        /// serviceManager.LogWriteLine(MdlConst.LVL_NONE, "メッセージ");
        /// </code>
        /// </example>
        public void LogWriteLine(int level, string message)
        {
            bool isConsole = _logger.GetValueByKey(ClsLogger.IS_CONSOLE, true);
            if (!_isSilent && isConsole) _logger.WriteLine(level, message);
        }
    }
}
