﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STranslate.Helper;
using STranslate.Log;
using STranslate.Model;
using STranslate.Util;
using STranslate.ViewModels.Preference;
using STranslate.ViewModels.Preference.Services;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace STranslate
{
    public class ConfigHelper
    {
        #region 公共方法

        public ConfigHelper()
        {
            if (!Directory.Exists(ApplicationData)) //判断是否存在
            {
                Directory.CreateDirectory(ApplicationData); //创建新路径
                ShortcutUtil.SetDesktopShortcut(); //创建桌面快捷方式
            }
            if (!File.Exists(CnfName)) //文件不存在
            {
                FileStream fs = new(CnfName, FileMode.Create, FileAccess.ReadWrite);
                fs.Close();
                WriteConfig(InitialConfig());
            }

            //初始化时将初始值赋给Config属性
            CurrentConfig = ResetConfig;
        }

        /// <summary>
        /// 初始化操作
        /// </summary>
        public void InitialOperate()
        {
            //初始化主题
            Application.Current.Resources.MergedDictionaries.First().Source = CurrentConfig?.IsBright ?? true
                ? ConstStr.LIGHTURI
                : ConstStr.DARKURI;

            //初始化代理设置
            ProxyUtil.UpdateDynamicProxy(CurrentConfig?.IsDisableSystemProxy ?? false);
        }

        /// <summary>
        /// 退出时保存位置
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool WriteConfig(double x, double y)
        {
            bool isSuccess = false;
            if (CurrentConfig is not null)
            {
                CurrentConfig.Position = $"{x},{y}";
                WriteConfig(CurrentConfig);
                isSuccess = true;
            }
            return isSuccess;
        }

        /// <summary>
        /// 写入服务到配置
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public bool WriteConfig(BindingList<ITranslator> services)
        {
            bool isSuccess = false;
            if (CurrentConfig is not null)
            {
                // 写入时加密AppID、AppKey
                services.ToList().ForEach(service =>
                {
                    service.AppID = string.IsNullOrEmpty(service.AppID) ? service.AppID : DESUtil.DesEncrypt(service.AppID);
                    service.AppKey = string.IsNullOrEmpty(service.AppKey) ? service.AppKey : DESUtil.DesEncrypt(service.AppKey);
                });
                CurrentConfig.Services = services;
                WriteConfig(CurrentConfig);
                isSuccess = true;
            }
            return isSuccess;
        }

        /// <summary>
        /// 写入源语言、目标语言到配置
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool WriteConfig(string source, string target)
        {
            bool isSuccess = false;
            if (CurrentConfig is not null)
            {
                CurrentConfig.SourceLanguage = source;
                CurrentConfig.TargetLanguage = target;
                WriteConfig(CurrentConfig);
                isSuccess = true;
            }
            return isSuccess;
        }

        /// <summary>
        /// 写入热键到配置
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool WriteConfig(Hotkeys hotkeys)
        {
            bool isSuccess = false;
            if (CurrentConfig is not null)
            {
                CurrentConfig.Hotkeys = hotkeys;
                WriteConfig(CurrentConfig);
                isSuccess = true;
            }
            return isSuccess;
        }

        /// <summary>
        /// 写入常规配置项到当前配置
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool WriteConfig(CommonViewModel model)
        {
            bool isSuccess = false;
            if (CurrentConfig is not null)
            {
                CurrentConfig.IsStartup = model.IsStartup;
                CurrentConfig.NeedAdministrator = model.NeedAdmin;
                CurrentConfig.HistorySize = model.HistorySize;
                CurrentConfig.AutoScale = model.AutoScale;
                CurrentConfig.IsBright = model.IsBright;
                CurrentConfig.IsFollowMouse = model.IsFollowMouse;
                CurrentConfig.CloseUIOcrRetTranslate = model.CloseUIOcrRetTranslate;
                CurrentConfig.UnconventionalScreen = model.UnconventionalScreen;
                CurrentConfig.IsDisableSystemProxy = model.IsDisableSystemProxy;
                CurrentConfig.IsOcrAutoCopyText = model.IsOcrAutoCopyText;
                CurrentConfig.IsAdjustContentTranslate = model.IsAdjustContentTranslate;
                CurrentConfig.IsRemoveLineBreakGettingWords = model.IsRemoveLineBreakGettingWords;
                CurrentConfig.DoubleTapTrayFunc = model.DoubleTapTrayFunc;

                WriteConfig(CurrentConfig);
                isSuccess = true;
            }
            return isSuccess;
        }

        #endregion 公共方法

        #region 私有方法

        internal ConfigModel ReadConfig()
        {
            try
            {
                var settings = new JsonSerializerSettings { Converters = { new TranslatorConverter() } };
                var content = File.ReadAllText(CnfName);
                var config = JsonConvert.DeserializeObject<ConfigModel>(content, settings) ?? throw new Exception("反序列化失败...");
                // 读取时解密AppID、AppKey
                config.Services?.ToList().ForEach(service =>
                {
                    service.AppID = string.IsNullOrEmpty(service.AppID) ? service.AppID : DESUtil.DesDecrypt(service.AppID);
                    service.AppKey = string.IsNullOrEmpty(service.AppKey) ? service.AppKey : DESUtil.DesDecrypt(service.AppKey);
                });
                return config;
            }
            catch (Exception ex)
            {
                LogService.Logger.Error($"[READCONFIG] 读取配置错误，本次运行加载初始化配置...", ex);
                return InitialConfig();
            }
        }

        internal T ReadConfig<T>()
            where T : class
        {
            try
            {
                var settings = new JsonSerializerSettings { Converters = { new TranslatorConverter() } };
                var content = File.ReadAllText(CnfName);
                return JsonConvert.DeserializeObject<T>(content, settings) ?? throw new Exception("反序列化失败...");
            }
            catch (Exception ex)
            {
                throw new Exception("[READCONFIG] 读取配置错误，请检查配置文件", ex);
            }
        }

        internal void WriteConfig(object obj)
        {
            File.WriteAllText(CnfName, JsonConvert.SerializeObject(obj, Formatting.Indented));
        }

        #endregion 私有方法

        #region 字段 && 属性

        /// <summary>
        /// 重置Config
        /// </summary>
        public ConfigModel ResetConfig => ReadConfig();

        /// <summary>
        /// 初始Config
        /// </summary>
        private ConfigModel InitialConfig()
        {
            var hk = new Hotkeys();
            hk.InputTranslate.Update(KeyModifiers.MOD_ALT, KeyCodes.A, ConstStr.DEFAULTINPUTHOTKEY);
            hk.CrosswordTranslate.Update(KeyModifiers.MOD_ALT, KeyCodes.D, ConstStr.DEFAULTCROSSWORDHOTKEY);
            hk.ScreenShotTranslate.Update(KeyModifiers.MOD_ALT, KeyCodes.S, ConstStr.DEFAULTSCREENSHOTHOTKEY);
            hk.OpenMainWindow.Update(KeyModifiers.MOD_ALT, KeyCodes.G, ConstStr.DEFAULTOPENHOTKEY);
            hk.MousehookTranslate.Update(KeyModifiers.MOD_ALT | KeyModifiers.MOD_SHIFT, KeyCodes.D, ConstStr.DEFAULTMOUSEHOOKHOTKEY);
            hk.OCR.Update(KeyModifiers.MOD_ALT | KeyModifiers.MOD_SHIFT, KeyCodes.S, ConstStr.DEFAULTOCRHOTKEY);
            return new ConfigModel
            {
                HistorySize = 100,
                AutoScale = 0.8,
                Hotkeys = hk,
                IsBright = true,
                IsStartup = false,
                IsFollowMouse = false,
                IsOcrAutoCopyText = false,
                UnconventionalScreen = false,
                IsDisableSystemProxy = false,
                CloseUIOcrRetTranslate = false,
                IsAdjustContentTranslate = false,
                IsRemoveLineBreakGettingWords = false,
                DoubleTapTrayFunc = DoubleTapFuncEnum.InputFunc,
                SourceLanguage = LanguageEnum.AUTO.GetDescription(),
                TargetLanguage = LanguageEnum.AUTO.GetDescription(),
                Services =
                [
                    new TranslatorApi(Guid.NewGuid(), "https://deeplx.deno.dev/translate", "DeepL"),
                    new TranslatorApi(Guid.NewGuid(), "https://googlet.deno.dev/translate", "Google", IconType.Google, isEnabled: true),
                    new TranslatorApi(Guid.NewGuid(), "https://iciba.deno.dev/translate", "爱词霸", IconType.Iciba, isEnabled: false)
                ]
            };
        }

        private ConfigModel? _config;

        public ConfigModel? CurrentConfig
        {
            get => _config;
            private set => _config = value;
        }

        /// <summary>
        /// 配置文件
        /// </summary>
        private string CnfName => $"{ApplicationData}\\{_AppName.ToLower()}.json";

#if true

        /// <summary>
        /// C:\Users\user\AppData\Local\STranslate
        /// </summary>
        private string ApplicationData => $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{_AppName}";

#else
        /// <summary>
        /// 当前目录
        /// </summary>
        private string ApplicationData => $"{AppDomain.CurrentDomain.BaseDirectory}";
#endif

        private readonly string _AppName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()!.Location);

        #endregion 字段 && 属性
    }

    #region JsonConvert

    public class TranslatorConverter : JsonConverter<ITranslator>
    {
        public override ITranslator ReadJson(
            JsonReader reader,
            Type objectType,
            ITranslator? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        )
        {
            JObject jsonObject = JObject.Load(reader);

            // 根据Type字段的值来决定具体实现类
            var type = jsonObject["Type"]!.Value<int>();
            ITranslator translator;

            translator = type switch
            {
                (int)ServiceType.ApiService => new TranslatorApi(),
                (int)ServiceType.BaiduService => new TranslatorBaidu(),
                (int)ServiceType.BingService => new TranslatorBing(),
                //TODO: 新接口需要适配
                _ => throw new NotSupportedException($"Unsupported ServiceType: {type}")
            };

            serializer.Populate(jsonObject.CreateReader(), translator);
            return translator;
        }

        public override void WriteJson(JsonWriter writer, ITranslator? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    #endregion JsonConvert
}