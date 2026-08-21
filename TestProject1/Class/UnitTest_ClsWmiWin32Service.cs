using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.Versioning;
using CmnClsLib.Class;
using CmnClsLib.Module;
using CmnWmiLib.Class;
using Xunit;
using Assert = Xunit.Assert;

namespace TestProject1.Class
{
    [SupportedOSPlatform("windows")]
    public class UnitTest_ClsWmiWin32Service
    {
        private readonly ClsLogger _logger = new();

        /// <summary>
        /// テスト用の Win32_Service ManagementObject を取得するヘルパーメソッド
        /// </summary>
        private static ManagementObject? GetTestServiceManagementObject(string serviceName)
        {
            try
            {
                var ms = new ManagementScope(@"root\CIMv2");
                ms.Connect();
                var oq = new ObjectQuery($"SELECT * FROM Win32_Service WHERE Name = '{serviceName}'");
                using var mos = new ManagementObjectSearcher(ms, oq);
                foreach (ManagementObject mo in mos.Get())
                {
                    return mo;
                }
            }
            catch
            {
                // WMI 接続失敗時は null
            }
            return null;
        }

        // ====================================================================
        // 1. プロパティ初期値および設定・取得テスト
        // ====================================================================

        [Fact]
        public void Properties_初期値が正しいこと()
        {
            var service = new ClsWmiWin32Service(_logger);

            Assert.Equal("127.0.0.1", service.RemoteHost);
            Assert.Equal("", service.Username);
            Assert.Equal("", service.Password);
            Assert.False(service.IsSwitchUser);
            Assert.Equal("", service.ServiceName);
            Assert.Equal("status", service.Action);
            Assert.Equal(0, service.Verbose);
            Assert.Equal(60, service.Timeout);
            Assert.Equal(MdlConst.LVL_E, service.RetCodeAtTimeout);
            Assert.False(service.IsSync);
            Assert.False(service.IsIfExist);
            Assert.False(service.IsIfAuto);
            Assert.False(service.IsExist);
            Assert.False(service.IsStackTrace);
            Assert.False(service.IsSilent);
            Assert.NotNull(service.Services);
            Assert.Empty(service.Services);
            Assert.Equal("", service.StartMode);
            Assert.False(service.IsLogWrite);
        }

        [Fact]
        public void Properties_設定値が正しく保持されること()
        {
            var servicesDict = new Dictionary<string, string> { { "TestService", "Test Display Name" } };

            var service = new ClsWmiWin32Service(_logger)
            {
                RemoteHost = "192.168.1.100",
                Username = "admin",
                Password = "secretPassword",
                IsSwitchUser = true,
                ServiceName = "Spooler",
                Action = "running",
                Verbose = 5,
                Timeout = 120,
                RetCodeAtTimeout = MdlConst.LVL_W,
                IsSync = true,
                IsIfExist = true,
                IsIfAuto = true,
                IsExist = true,
                IsStackTrace = true,
                IsSilent = true,
                Services = servicesDict,
                IsLogWrite = true
            };

            Assert.Equal("192.168.1.100", service.RemoteHost);
            Assert.Equal("admin", service.Username);
            Assert.Equal("secretPassword", service.Password);
            Assert.True(service.IsSwitchUser);
            Assert.Equal("Spooler", service.ServiceName);
            Assert.Equal("running", service.Action);
            Assert.Equal(5, service.Verbose);
            Assert.Equal(120, service.Timeout);
            Assert.Equal(MdlConst.LVL_W, service.RetCodeAtTimeout);
            Assert.True(service.IsSync);
            Assert.True(service.IsIfExist);
            Assert.True(service.IsIfAuto);
            Assert.True(service.IsExist);
            Assert.True(service.IsStackTrace);
            Assert.True(service.IsSilent);
            Assert.Same(servicesDict, service.Services);
            Assert.True(service.IsLogWrite);
        }

        [Theory]
        [InlineData("automatic", "Automatic")]
        [InlineData("auto", "Automatic")]
        [InlineData("a", "Automatic")]
        [InlineData("AUTOMATIC", "Automatic")]
        [InlineData("AUTO", "Automatic")]
        [InlineData("A", "Automatic")]
        [InlineData("manual", "Manual")]
        [InlineData("man", "Manual")]
        [InlineData("m", "Manual")]
        [InlineData("MANUAL", "Manual")]
        [InlineData("MAN", "Manual")]
        [InlineData("M", "Manual")]
        [InlineData("disabled", "Disabled")]
        [InlineData("dis", "Disabled")]
        [InlineData("d", "Disabled")]
        [InlineData("DISABLED", "Disabled")]
        [InlineData("DIS", "Disabled")]
        [InlineData("D", "Disabled")]
        [InlineData("", "")]
        [InlineData(null, "")]
        [InlineData("other_mode", "")]
        [InlineData("123", "")]
        public void StartMode_各入力値に対して期待通り正規化されること(string? input, string expected)
        {
            var service = new ClsWmiWin32Service(_logger)
            {
                StartMode = input!
            };

            Assert.Equal(expected, service.StartMode);
        }

        // ====================================================================
        // 2. 初期化メソッドテスト
        // ====================================================================

        [Fact]
        public void Initialize_呼び出しで正常に初期化が行われること()
        {
            var service = new ClsWmiWin32Service(_logger);
            var ex = Record.Exception(() => service.Initialize());
            Assert.Null(ex);
        }

        [Fact]
        public void Init_非推奨メソッドが正常に呼び出せること()
        {
            var service = new ClsWmiWin32Service(_logger);
#pragma warning disable CS0618
            var ex = Record.Exception(() => service.Init());
#pragma warning restore CS0618
            Assert.Null(ex);
        }

        // ====================================================================
        // 3. WMI実オブジェクトを用いた個別メソッドテスト
        // ====================================================================

        [Fact]
        public void GetStatus_正常にサービス状態文字列が取得できること()
        {
            using var mo = GetTestServiceManagementObject("Winmgmt");
            Assert.NotNull(mo);

            var service = new ClsWmiWin32Service(_logger);
            string status = service.GetStatus(mo);

            Assert.False(string.IsNullOrEmpty(status));
            Assert.Contains(status, new[] { "Running", "Stopped", "Paused", "Start Pending", "Stop Pending" });
        }

        [Fact]
        public void GetServiceStatusLowerCase_正常に小文字のサービス状態文字列が取得できること()
        {
            using var mo = GetTestServiceManagementObject("Winmgmt");
            Assert.NotNull(mo);

            var service = new ClsWmiWin32Service(_logger);
            string statusLower = service.GetServiceStatusLowerCase(mo);

            Assert.False(string.IsNullOrEmpty(statusLower));
            Assert.Contains(statusLower, new[] { "running", "stopped", "paused", "start pending", "stop pending" });
        }

        [Fact]
        public void GetStatusToLower_非推奨メソッドで正常に取得できること()
        {
            using var mo = GetTestServiceManagementObject("Winmgmt");
            Assert.NotNull(mo);

            var service = new ClsWmiWin32Service(_logger);
#pragma warning disable CS0618
            string statusLower = service.GetStatusToLower(mo);
#pragma warning restore CS0618

            Assert.False(string.IsNullOrEmpty(statusLower));
        }

        [Fact]
        public void GetStartMode_正常にスタートアップモードが取得できること()
        {
            using var mo = GetTestServiceManagementObject("Winmgmt");
            Assert.NotNull(mo);

            var service = new ClsWmiWin32Service(_logger);
            string startMode = service.GetStartMode(mo);

            Assert.False(string.IsNullOrEmpty(startMode));
            Assert.Contains(startMode, new[] { "Auto", "Manual", "Disabled" });
        }

        [Fact]
        public void GetStartModeLowerCase_正常に小文字のスタートアップモードが取得できること()
        {
            using var mo = GetTestServiceManagementObject("Winmgmt");
            Assert.NotNull(mo);

            var service = new ClsWmiWin32Service(_logger);
            string startModeLower = service.GetStartModeLowerCase(mo);

            Assert.False(string.IsNullOrEmpty(startModeLower));
            Assert.Contains(startModeLower, new[] { "auto", "manual", "disabled" });
        }

        [Fact]
        public void GetStartModeToLower_非推奨メソッドで正常に取得できること()
        {
            using var mo = GetTestServiceManagementObject("Winmgmt");
            Assert.NotNull(mo);

            var service = new ClsWmiWin32Service(_logger);
#pragma warning disable CS0618
            string startModeLower = service.GetStartModeToLower(mo);
#pragma warning restore CS0618

            Assert.False(string.IsNullOrEmpty(startModeLower));
        }

        [Fact]
        public void EvaluateStatus_Actionがstatusの場合はLVL_Iを返すこと()
        {
            using var mo = GetTestServiceManagementObject("Winmgmt");
            Assert.NotNull(mo);

            var service = new ClsWmiWin32Service(_logger)
            {
                Action = "status"
            };

            int result = service.EvaluateStatus(mo, "Winmgmt");
            Assert.Equal(MdlConst.LVL_I, result);
        }

        [Fact]
        public void EvaluateStatus_Actionがnoneの場合はLVL_Iを返すこと()
        {
            using var mo = GetTestServiceManagementObject("Winmgmt");
            Assert.NotNull(mo);

            var service = new ClsWmiWin32Service(_logger)
            {
                Action = "none"
            };

            int result = service.EvaluateStatus(mo, "Winmgmt");
            Assert.Equal(MdlConst.LVL_I, result);
        }

        [Fact]
        public void EvaluateStatus_Actionがrunningの場合は状態に応じた結果を返すこと()
        {
            using var mo = GetTestServiceManagementObject("Winmgmt");
            Assert.NotNull(mo);

            var service = new ClsWmiWin32Service(_logger)
            {
                Action = "running"
            };

            string state = service.GetServiceStatusLowerCase(mo);
            int result = service.EvaluateStatus(mo, "Winmgmt");

            if (state == "running")
            {
                Assert.Equal(MdlConst.LVL_I, result);
            }
            else
            {
                Assert.Equal(MdlConst.LVL_E, result);
            }
        }

        [Fact]
        public void EvaluateStatus_ActionがrunningかつIsIfAuto有効時の動作()
        {
            using var mo = GetTestServiceManagementObject("Winmgmt");
            Assert.NotNull(mo);

            var service = new ClsWmiWin32Service(_logger)
            {
                Action = "running",
                IsIfAuto = true
            };

            string state = service.GetServiceStatusLowerCase(mo);
            string mode = service.GetStartModeLowerCase(mo);
            int result = service.EvaluateStatus(mo, "Winmgmt");

            if (state == "running")
            {
                Assert.Equal(MdlConst.LVL_I, result);
            }
            else
            {
                if (mode == "auto")
                {
                    Assert.Equal(MdlConst.LVL_E, result);
                }
                else
                {
                    Assert.Equal(MdlConst.LVL_W, result);
                }
            }
        }

        [Fact]
        public void EvaluateStatus_オーバーロードおよび非推奨メソッドが動作すること()
        {
            using var mo = GetTestServiceManagementObject("Winmgmt");
            Assert.NotNull(mo);

            var service = new ClsWmiWin32Service(_logger)
            {
                Action = "status"
            };

            int result1 = service.EvaluateStatus(mo);
            Assert.Equal(MdlConst.LVL_I, result1);

#pragma warning disable CS0618
            int result2 = service.EvalStatus(mo, "Winmgmt");
            Assert.Equal(MdlConst.LVL_I, result2);

            int result3 = service.EvalStatus(mo);
            Assert.Equal(MdlConst.LVL_I, result3);
#pragma warning restore CS0618
        }

        // ====================================================================
        // 4. GetStoppedAutoServices / GetDownServicesList テスト
        // ====================================================================

        [Fact]
        public void GetStoppedAutoServices_空文字指定で正常終了すること()
        {
            var service = new ClsWmiWin32Service(_logger);
            int result = service.GetStoppedAutoServices("");

            Assert.Equal(MdlConst.LVL_I, result);
            Assert.NotNull(service.Services);
        }

        [Fact]
        public void GetStoppedAutoServices_ワイルドカード指定で正常終了すること()
        {
            var service = new ClsWmiWin32Service(_logger)
            {
                ServiceName = "W%"
            };
            int result = service.GetStoppedAutoServices("W%");

            Assert.Equal(MdlConst.LVL_I, result);
            Assert.NotNull(service.Services);
        }

        [Fact]
        public void GetStoppedAutoServices_存在しないサービス指定時はServicesが空でLVL_Iを返すこと()
        {
            var service = new ClsWmiWin32Service(_logger)
            {
                ServiceName = "NonExistentDummyService_123456789"
            };
            int result = service.GetStoppedAutoServices("NonExistentDummyService_123456789");

            Assert.Equal(MdlConst.LVL_I, result);
            Assert.Empty(service.Services);
        }

        [Fact]
        public void GetDownServicesList_非推奨メソッドが正常に動作すること()
        {
            var service = new ClsWmiWin32Service(_logger);
#pragma warning disable CS0618
            int result = service.GetDownServicesList("");
#pragma warning restore CS0618

            Assert.Equal(MdlConst.LVL_I, result);
        }

        [Fact]
        public void GetStoppedAutoServices_無効なホスト名指定時にLVL_Eを返すこと()
        {
            var service = new ClsWmiWin32Service(_logger)
            {
                RemoteHost = "invalid_remote_host_99999",
                Verbose = 10,
                IsStackTrace = true
            };
            int result = service.GetStoppedAutoServices("");

            Assert.Equal(MdlConst.LVL_E, result);
        }

        [Fact]
        public void GetStoppedAutoServices_ローカル接続でIsSwitchUser有効時はWMI仕様によりLVL_Eを返すこと()
        {
            var service = new ClsWmiWin32Service(_logger)
            {
                IsSwitchUser = true,
                Username = "dummy_user",
                Password = "dummy_password",
                Verbose = 10,
                IsStackTrace = true
            };
            int result = service.GetStoppedAutoServices("");

            Assert.Equal(MdlConst.LVL_E, result);
        }

        // ====================================================================
        // 5. Execute メソッドテスト
        // ====================================================================

        [Fact]
        public void Execute_実在するサービス名でstatusアクションが正常終了すること()
        {
            var service = new ClsWmiWin32Service(_logger)
            {
                ServiceName = "Winmgmt",
                Action = "status",
                Verbose = 5
            };

            int result = service.Execute();

            Assert.Equal(MdlConst.LVL_I, result);
            Assert.True(service.IsExist);
        }

        [Fact]
        public void Execute_実在するサービス名でnoneアクションが正常終了すること()
        {
            var service = new ClsWmiWin32Service(_logger)
            {
                ServiceName = "RpcSs",
                Action = "none"
            };

            int result = service.Execute();

            Assert.Equal(MdlConst.LVL_I, result);
            Assert.True(service.IsExist);
        }

        [Fact]
        public void Execute_ワイルドカード指定で正常終了すること()
        {
            var service = new ClsWmiWin32Service(_logger)
            {
                ServiceName = "Win%",
                Action = "status"
            };

            int result = service.Execute();

            Assert.Equal(MdlConst.LVL_I, result);
            Assert.True(service.IsExist);
        }

        [Fact]
        public void Execute_存在しないサービス名でIsIfExistがfalseの場合はLVL_Eを返すこと()
        {
            var service = new ClsWmiWin32Service(_logger)
            {
                ServiceName = "NonExistentDummyService_123456789",
                Action = "status",
                IsIfExist = false
            };

            int result = service.Execute();

            Assert.Equal(MdlConst.LVL_E, result);
            Assert.False(service.IsExist);
        }

        [Fact]
        public void Execute_存在しないサービス名でIsIfExistがtrueの場合はLVL_Iを返すこと()
        {
            var service = new ClsWmiWin32Service(_logger)
            {
                ServiceName = "NonExistentDummyService_123456789",
                Action = "status",
                IsIfExist = true
            };

            int result = service.Execute();

            Assert.Equal(MdlConst.LVL_I, result);
            Assert.False(service.IsExist);
        }

        [Fact]
        public void Execute_VerboseおよびIsStackTrace有効時でも正常に動作すること()
        {
            var service = new ClsWmiWin32Service(_logger)
            {
                ServiceName = "Winmgmt",
                Action = "status",
                Verbose = 10,
                IsStackTrace = true
            };

            int result = service.Execute();

            Assert.Equal(MdlConst.LVL_I, result);
            Assert.True(service.IsExist);
        }

        [Fact]
        public void Execute_無効なホスト名指定時にLVL_Eを返すこと()
        {
            var service = new ClsWmiWin32Service(_logger)
            {
                RemoteHost = "invalid_remote_host_99999",
                ServiceName = "Winmgmt",
                Verbose = 10,
                IsStackTrace = true
            };

            int result = service.Execute();

            Assert.Equal(MdlConst.LVL_E, result);
        }

        [Fact]
        public void Execute_ローカル接続でIsSwitchUser有効時はWMI仕様によりLVL_Eを返すこと()
        {
            var service = new ClsWmiWin32Service(_logger)
            {
                ServiceName = "Winmgmt",
                IsSwitchUser = true,
                Username = "dummy_user",
                Password = "dummy_password",
                Verbose = 10,
                IsStackTrace = true
            };

            int result = service.Execute();

            Assert.Equal(MdlConst.LVL_E, result);
        }

        [Fact]
        public void Execute_RemoteHostがlocalhost指定で正常に動作すること()
        {
            var service = new ClsWmiWin32Service(_logger)
            {
                RemoteHost = "localhost",
                ServiceName = "Winmgmt",
                Action = "status"
            };

            int result = service.Execute();

            Assert.Equal(MdlConst.LVL_I, result);
            Assert.True(service.IsExist);
        }

        // ====================================================================
        // 6. LogWriteLine / ログ出力テスト
        // ====================================================================

        [Fact]
        public void LogWriteLine_サイレントモードが無効の場合はログ出力が実行されること()
        {
            var service = new ClsWmiWin32Service(_logger)
            {
                IsSilent = false
            };

            var ex = Record.Exception(() => service.LogWriteLine(MdlConst.LVL_I, "Test message"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogWriteLine_サイレントモードが有効の場合は例外なくスキップされること()
        {
            var service = new ClsWmiWin32Service(_logger)
            {
                IsSilent = true
            };

            var ex = Record.Exception(() => service.LogWriteLine(MdlConst.LVL_I, "Test message silent"));
            Assert.Null(ex);
        }
    }
}
