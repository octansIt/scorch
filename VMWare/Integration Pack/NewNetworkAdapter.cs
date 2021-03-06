using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Collections.Generic;
using System.Text;
using Microsoft.SystemCenter.Orchestrator.Integration;
using System.Collections.ObjectModel;

namespace VMWareIntegrationPack
{
    [Activity("New Network Adapter")]
    class NewNetworkAdapter : IActivity
    {
        private ConnectionSettings settings;
        private String userName = String.Empty;
        private String password = String.Empty;
        private String virtualCenter = String.Empty;
        private String port = String.Empty;
        private String domain = String.Empty;

        private String MacAddress = String.Empty;
        private String NetworkName = String.Empty;
        private String StartConnected = String.Empty;
        private String WakeOnLan = String.Empty;
        private String Type = String.Empty;
        private String VM = String.Empty;

        [ActivityConfiguration]
        public ConnectionSettings Settings
        {
            get { return settings; }
            set { settings = value; }
        }

        public void Design(IActivityDesigner designer)
        {
            string[] typeOptions = new string[5];
            typeOptions[0] = "e1000";
            typeOptions[1] = "Flexible";
            typeOptions[2] = "Vmxnet";
            typeOptions[3] = "EnhancedVmxnet";
            typeOptions[4] = "Vmxnet3";

            string[] StartConnectedOptions = new string[2];
            StartConnectedOptions[0] = "true";
            StartConnectedOptions[1] = "false";

            string[] WakeOnLanOptions = new string[2];
            WakeOnLanOptions[0] = "true";
            WakeOnLanOptions[1] = "false";

            designer.AddInput("VM");
            designer.AddInput("MacAddress").NotRequired();
            designer.AddInput("NetworkName");
            designer.AddInput("StartConnected").NotRequired().WithListBrowser(StartConnectedOptions);
            designer.AddInput("WakeOnLan").NotRequired().WithListBrowser(WakeOnLanOptions);
            designer.AddInput("Type").NotRequired().WithListBrowser(typeOptions);

            designer.AddCorellatedData(typeof(networkAdapter));
        }

        public void Execute(IActivityRequest request, IActivityResponse response)
        {
            userName = settings.UserName;
            password = settings.Password;
            domain = settings.Domain;
            port = settings.Port;
            virtualCenter = settings.VirtualCenter;

            VM = request.Inputs["VM"].AsString();
            MacAddress = request.Inputs["MacAddress"].AsString();
            NetworkName = request.Inputs["NetworkName"].AsString();
            StartConnected = request.Inputs["StartConnected"].AsString();
            WakeOnLan = request.Inputs["WakeOnLan"].AsString();
            Type = request.Inputs["Type"].AsString();

            response.WithFiltering().PublishRange(runCommand());
        }

        private IEnumerable<networkAdapter> runCommand()
        {
            PSSnapInException warning = new PSSnapInException();
            Runspace runspace = RunspaceFactory.CreateRunspace();
            runspace.RunspaceConfiguration.AddPSSnapIn("VMware.VimAutomation.Core", out warning);
            runspace.Open();

            Pipeline pipeline = runspace.CreatePipeline();

            String Script = "Connect-VIServer -Server " + virtualCenter + " -Port " + port + " -User " + domain + "\\" + userName + " -Password " + password + "\n";

            String command = "New-NetworkAdapter";

            if (!(VM == String.Empty)) { command += " -VM \"" + VM + "\""; }
            if (!(MacAddress == String.Empty)) { command += " -MacAddress \"" + MacAddress + "\""; }
            if (!(NetworkName == String.Empty)) { command += " -NetworkName \"" + NetworkName + "\""; }
            if (StartConnected.Equals("true")) { command += " -StartConnected"; }
            if (WakeOnLan.Equals("true")) { command += " -WakeOnLan"; }
            if (!(Type == String.Empty)) { command += " -Type \"" + Type + "\""; }

            Script += command + " -confirm:$False";

            pipeline.Commands.AddScript(Script);

            Collection<PSObject> results = new Collection<PSObject>();

            try
            {
                results = pipeline.Invoke();
            }

            catch (Exception ex)
            {
                throw ex;
            }

            foreach (PSObject obj in results)
            {
                if (obj.BaseObject.GetType().ToString().Contains("NetworkAdapterImpl"))
                {
                    String Mac_Address = obj.Members["MacAddress"].Value.ToString();
                    String WakeOnLanEnabled = obj.Members["WakeOnLanEnabled"].Value.ToString();
                    String Network_Name = obj.Members["NetworkName"].Value.ToString();
                    String ParentId = obj.Members["ParentId"].Value.ToString();
                    String Id = obj.Members["Id"].Value.ToString();
                    String Name = obj.Members["Name"].Value.ToString();

                    yield return new networkAdapter(Mac_Address,WakeOnLanEnabled,Network_Name,ParentId,Id,Name);
                }
            }
            runspace.Close();
        }
    }
}

