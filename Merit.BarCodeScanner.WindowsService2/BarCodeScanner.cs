using System;
using System.Configuration;
using System.ServiceProcess;
using Merit.BarCodeScanner.Logging;
using Merit.BarCodeScanner.Models;
using Merit.BarCodeScanner.Services;
using System.Timers;

namespace Merit.BarCodeScanner.WindowsService2
{
    public partial class BarCodeScanner : ServiceBase
    {
        private ILogger _logService = new FileLogManager(typeof(BarCodeScanner));
        private Timer timer;
        private AppSettingsSection _appSettings;
        private BarCodeScannerService service;
        private ImportRequest importRequest;

        public BarCodeScanner()
        {
            InitializeComponent();
            log4net.Config.XmlConfigurator.Configure();
            
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            _appSettings = configuration.AppSettings;
            service = new BarCodeScannerService();
        }

        protected override void OnStart(string[] args)
        {
            _logService.LogInfo("Service is Start");


            var intervalValue = _appSettings.Settings["Interval"];
            var timeValue = _appSettings.Settings["TimeRun"];

            int interval = intervalValue == null ? 30000 : int.Parse(intervalValue.Value);
            var time = timeValue == null ? "183000" : timeValue.Value;

            importRequest = new ImportRequest
            {
                FileName = _appSettings.Settings["FileName"]?.Value,
                PathFolder = _appSettings.Settings["PathFolder"]?.Value,
            };
            
            var currentDate = DateTime.Now.ToString("HHmmss");

            if (currentDate.Equals(time))
            {
                timer = new Timer();
                this.timer.Interval = interval; //10000 ~ 1s
                this.timer.Elapsed += new System.Timers.ElapsedEventHandler(WorkProcess);
            }

            barCodeTimer.Enabled = false;
        }

        private void WorkProcess(object sender, ElapsedEventArgs e)
        {
            service.Import(importRequest, _appSettings);
        }

        protected override void OnStop()
        {
            _logService.LogInfo("Service is Stoped");
            barCodeTimer.Enabled = false;
        }
    }
}
