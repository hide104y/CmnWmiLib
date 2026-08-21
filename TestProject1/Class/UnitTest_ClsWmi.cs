using System.Collections.Generic;
using System.Runtime.Versioning;
using CmnClsLib.Class;
using CmnClsLib.Module;
using CmnWmiLib.Class;
using Xunit;
using Assert = Xunit.Assert;

namespace TestProject1.Class
{
    [SupportedOSPlatform("windows")]
    public class UnitTest_ClsWmi
    {
        private readonly ClsLogger _logger = new();

        // --------------------------------------------------------------------
        // プロパティ初期値および設定・取得テスト
        // --------------------------------------------------------------------
        [Fact]
        public void Properties_初期値が正しいこと()
        {
            var wmi = new ClsWmi(_logger);

            Assert.Equal("", wmi.RemoteHost);
            Assert.Equal("", wmi.Username);
            Assert.Equal("", wmi.Password);
            Assert.Equal(",", wmi.Delimiter);
            Assert.Equal("", wmi.ClassName);
            Assert.Equal("", wmi.Condition);
            Assert.False(wmi.IsSwitchUser);
            Assert.Equal(0, wmi.Verbose);
            Assert.False(wmi.IsStackTrace);
            Assert.NotNull(wmi.KeyDictionary);
            Assert.Empty(wmi.KeyDictionary);
            Assert.NotNull(wmi.DataDictionary);
            Assert.Empty(wmi.DataDictionary);
        }

        [Fact]
        public void Properties_設定値が正しく保持されること()
        {
            var keyDict = new Dictionary<string, string> { { "Key1", "Val1" } };
            var dataDict = new Dictionary<string, string> { { "Data1", "Val2" } };

            var wmi = new ClsWmi(_logger)
            {
                RemoteHost = "192.168.1.1",
                Username = "testuser",
                Password = "password",
                Delimiter = "|",
                ClassName = "Win32_Process",
                Condition = "ProcessId = 0",
                IsSwitchUser = true,
                Verbose = 5,
                IsStackTrace = true,
                KeyDictionary = keyDict,
                DataDictionary = dataDict
            };

            Assert.Equal("192.168.1.1", wmi.RemoteHost);
            Assert.Equal("testuser", wmi.Username);
            Assert.Equal("password", wmi.Password);
            Assert.Equal("|", wmi.Delimiter);
            Assert.Equal("Win32_Process", wmi.ClassName);
            Assert.Equal("ProcessId = 0", wmi.Condition);
            Assert.True(wmi.IsSwitchUser);
            Assert.Equal(5, wmi.Verbose);
            Assert.True(wmi.IsStackTrace);
            Assert.Same(keyDict, wmi.KeyDictionary);
            Assert.Same(dataDict, wmi.DataDictionary);
        }

        // --------------------------------------------------------------------
        // FetchData() / GetData() 正常系テスト
        // --------------------------------------------------------------------
        [Fact]
        public void FetchData_Win32_OperatingSystemから正常にデータ取得できること()
        {
            var wmi = new ClsWmi(_logger)
            {
                ClassName = "Win32_OperatingSystem",
                KeyDictionary = new Dictionary<string, string>
                {
                    { "Caption", "" },
                    { "Version", "" }
                }
            };

            int result = wmi.FetchData();

            Assert.Equal(MdlConst.LVL_I, result);
            Assert.True(wmi.DataDictionary.ContainsKey("Caption"));
            Assert.False(string.IsNullOrEmpty(wmi.DataDictionary["Caption"]));
            Assert.True(wmi.DataDictionary.ContainsKey("Version"));
            Assert.False(string.IsNullOrEmpty(wmi.DataDictionary["Version"]));
        }

        [Fact]
        public void FetchData_複数行取得時に指定したDelimiterで結合されること()
        {
            var wmi = new ClsWmi(_logger)
            {
                ClassName = "Win32_LogicalDisk",
                Condition = "DriveType = 3",
                Delimiter = ";",
                KeyDictionary = new Dictionary<string, string>
                {
                    { "DeviceID", "" }
                }
            };

            int result = wmi.FetchData();

            Assert.Equal(MdlConst.LVL_I, result);
            Assert.True(wmi.DataDictionary.ContainsKey("DeviceID"));
            Assert.False(string.IsNullOrEmpty(wmi.DataDictionary["DeviceID"]));
            Assert.Contains(":", wmi.DataDictionary["DeviceID"]);
        }

        [Fact]
        public void FetchData_DataDictionaryに既存値がある場合はDelimiterで結合されること()
        {
            var wmi = new ClsWmi(_logger)
            {
                ClassName = "Win32_OperatingSystem",
                Delimiter = "###",
                KeyDictionary = new Dictionary<string, string>
                {
                    { "Caption", "" }
                },
                DataDictionary = new Dictionary<string, string>
                {
                    { "Caption", "ExistingCaption" }
                }
            };

            int result = wmi.FetchData();

            Assert.Equal(MdlConst.LVL_I, result);
            Assert.StartsWith("ExistingCaption###", wmi.DataDictionary["Caption"]);
        }

        [Fact]
        public void FetchData_存在しないプロパティ指定時にデフォルト値が設定されること()
        {
            var wmi = new ClsWmi(_logger)
            {
                ClassName = "Win32_OperatingSystem",
                KeyDictionary = new Dictionary<string, string>
                {
                    { "NonExistentProperty_DummyKey_12345", "DEFAULT_VALUE" }
                }
            };

            int result = wmi.FetchData();

            Assert.Equal(MdlConst.LVL_I, result);
            Assert.True(wmi.DataDictionary.ContainsKey("NonExistentProperty_DummyKey_12345"));
            Assert.Equal("DEFAULT_VALUE", wmi.DataDictionary["NonExistentProperty_DummyKey_12345"]);
        }

        [Fact]
        public void FetchData_条件に合致するレコードがない場合はDataDictionaryに追加されないこと()
        {
            var wmi = new ClsWmi(_logger)
            {
                ClassName = "Win32_OperatingSystem",
                Condition = "Caption = 'NonExistentOperatingSystem_12345'",
                KeyDictionary = new Dictionary<string, string>
                {
                    { "Caption", "DefaultCaption" }
                }
            };

            int result = wmi.FetchData();

            Assert.Equal(MdlConst.LVL_I, result);
            Assert.Empty(wmi.DataDictionary);
        }

        [Fact]
        public void GetData_旧メソッドで正常にデータ取得できること()
        {
            var wmi = new ClsWmi(_logger)
            {
                ClassName = "Win32_OperatingSystem",
                KeyDictionary = new Dictionary<string, string>
                {
                    { "Caption", "" }
                }
            };

#pragma warning disable CS0618
            int result = wmi.GetData();
#pragma warning restore CS0618

            Assert.Equal(MdlConst.LVL_I, result);
            Assert.True(wmi.DataDictionary.ContainsKey("Caption"));
            Assert.False(string.IsNullOrEmpty(wmi.DataDictionary["Caption"]));
        }

        [Fact]
        public void FetchData_VerboseおよびIsStackTrace有効時でも正常に動作すること()
        {
            var wmi = new ClsWmi(_logger)
            {
                ClassName = "Win32_OperatingSystem",
                Verbose = 10,
                IsStackTrace = true,
                KeyDictionary = new Dictionary<string, string>
                {
                    { "Caption", "" }
                }
            };

            int result = wmi.FetchData();

            Assert.Equal(MdlConst.LVL_I, result);
            Assert.True(wmi.DataDictionary.ContainsKey("Caption"));
            Assert.False(string.IsNullOrEmpty(wmi.DataDictionary["Caption"]));
        }

        [Fact]
        public void FetchData_ローカル接続でIsSwitchUser有効時はWMI仕様によりLVL_Eを返すこと()
        {
            // Windows WMIではローカル接続(root\CIMv2)に対してUsername/Passwordの資格情報設定は許可されず例外となる
            var wmi = new ClsWmi(_logger)
            {
                ClassName = "Win32_OperatingSystem",
                IsSwitchUser = true,
                Username = "dummy_user",
                Password = "dummy_password",
                Verbose = 10,
                IsStackTrace = true,
                KeyDictionary = new Dictionary<string, string>
                {
                    { "Caption", "" }
                }
            };

            int result = wmi.FetchData();

            Assert.Equal(MdlConst.LVL_E, result);
        }

        [Fact]
        public void FetchData_RemoteHostがローカル指定の場合は通常パスで処理されること()
        {
            var wmi1 = new ClsWmi(_logger)
            {
                RemoteHost = "127.0.0.1",
                ClassName = "Win32_OperatingSystem",
                KeyDictionary = new Dictionary<string, string> { { "Caption", "" } }
            };
            int result1 = wmi1.FetchData();
            Assert.Equal(MdlConst.LVL_I, result1);

            var wmi2 = new ClsWmi(_logger)
            {
                RemoteHost = "localhost",
                ClassName = "Win32_OperatingSystem",
                KeyDictionary = new Dictionary<string, string> { { "Caption", "" } }
            };
            int result2 = wmi2.FetchData();
            Assert.Equal(MdlConst.LVL_I, result2);
        }

        // --------------------------------------------------------------------
        // FetchData() 異常系テスト
        // --------------------------------------------------------------------
        [Fact]
        public void FetchData_不正なClassName指定時にLVL_Eを返すこと()
        {
            var wmi = new ClsWmi(_logger)
            {
                ClassName = "Invalid_Wmi_ClassName_12345",
                KeyDictionary = new Dictionary<string, string>
                {
                    { "Dummy", "val" }
                }
            };

            int result = wmi.FetchData();

            Assert.Equal(MdlConst.LVL_E, result);
        }

        [Fact]
        public void FetchData_不正なCondition構文指定時にLVL_Eを返すこと()
        {
            var wmi = new ClsWmi(_logger)
            {
                ClassName = "Win32_OperatingSystem",
                Condition = "INVALID SYNTAX ???",
                Verbose = 10,
                IsStackTrace = true,
                KeyDictionary = new Dictionary<string, string>
                {
                    { "Caption", "" }
                }
            };

            int result = wmi.FetchData();

            Assert.Equal(MdlConst.LVL_E, result);
        }

        [Fact]
        public void FetchData_無効なRemoteHost指定時にLVL_Eを返すこと()
        {
            var wmi = new ClsWmi(_logger)
            {
                RemoteHost = "invalid_remote_host_99999",
                ClassName = "Win32_OperatingSystem",
                Verbose = 10,
                IsStackTrace = true,
                KeyDictionary = new Dictionary<string, string>
                {
                    { "Caption", "" }
                }
            };

            int result = wmi.FetchData();

            Assert.Equal(MdlConst.LVL_E, result);
        }
    }
}
