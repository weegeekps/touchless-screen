namespace TouchlessScreenService
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.touchlessScreenServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.touchlessScreenServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // touchlessScreenServiceProcessInstaller
            // 
            this.touchlessScreenServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.touchlessScreenServiceProcessInstaller.Password = null;
            this.touchlessScreenServiceProcessInstaller.Username = null;
            // 
            // touchlessScreenServiceInstaller
            // 
            this.touchlessScreenServiceInstaller.ServiceName = "TouchlessScreenService";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.touchlessScreenServiceProcessInstaller,
            this.touchlessScreenServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller touchlessScreenServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller touchlessScreenServiceInstaller;
    }
}