using Microsoft.Kinect;
using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using TouchlessScreenLibrary;

namespace TouchlessScreenService
{
    public partial class TouchlessScreenService : ServiceBase
    {
        private TouchlessScreen _touchlessScreen;

        public TouchlessScreenService()
        {
            InitializeComponent();

            // Windows Event Logger
            if (!System.Diagnostics.EventLog.SourceExists("TouchlessScreenLogSource"))
            {
                System.Diagnostics.EventLog.CreateEventSource("TouchlessScreenLogSource", "TouchlessScreenLog");
            }

            eventLogger.Source = "TouchlessScreenLogSource";
            eventLogger.Log = "TouchlessScreenLog";

            this.CanPauseAndContinue = false;
        }

        protected override void OnStart(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                eventLogger.WriteEntry("Touchless Screen Service Starting.", EventLogEntryType.Information);

                this._touchlessScreen = TouchlessScreen.Instance;

                this._touchlessScreen.Initialize();

                if (this._touchlessScreen.Sensor == null)
                {
                    eventLogger.WriteEntry("Failed to initialize Kinect Sensor.", EventLogEntryType.Error);

                    this.Stop();
                }

                this._touchlessScreen.Sensor.AllFramesReady += this.SensorDepthFrameReady;

                if (!this._touchlessScreen.TryStart())
                {
                    eventLogger.WriteEntry("Failed to start the Touchless Screen service.", EventLogEntryType.Error);

                    this.Stop();
                }
            }
            catch (Exception e)
            {
                eventLogger.WriteEntry(e.Message, EventLogEntryType.Error);

                this.Stop();
            }
        }

        protected override void OnStop()
        {
            try
            {
                eventLogger.WriteEntry("Touchless Screen Service Stopping.", EventLogEntryType.Information);
            }
            catch (Exception e)
            {
                eventLogger.WriteEntry(e.Message, EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's AllFramesReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorDepthFrameReady(object sender, AllFramesReadyEventArgs e)
        {
            try
            {
                this._touchlessScreen.HandleSensorEvent(sender, e);
            }
            catch (Exception ex)
            {
                StringBuilder builder = new StringBuilder("A fatal error has occurred.");
                builder.AppendLine("");
                builder.Append(ex.Message);

                this.Stop();
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (eventLogger != null)
            {
                eventLogger.WriteEntry(((Exception) e.ExceptionObject).Message, EventLogEntryType.Error);
            }

            this.Stop();
        }
    }
}
