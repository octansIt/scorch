using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SystemCenter.Orchestrator.Integration;
using System.Management;
using System.Management.Instrumentation;
using SCCMInterop;
using Microsoft.ConfigurationManagement;
using Microsoft.ConfigurationManagement.ManagementProvider;
using Microsoft.ConfigurationManagement.ManagementProvider.WqlQueryEngine;

namespace SCCMExtension
{
    [Activity("Delete SCCM Generic Object")]
    public class DeleteGenericObject : IActivity
    {
        private ConnectionCredentials settings;
        private String userName = String.Empty;
        private String password = String.Empty;
        private String SCCMServer = String.Empty;

        private int ObjCount = 0;

        [ActivityConfiguration]
        public ConnectionCredentials Settings
        {
            get { return settings; }
            set { settings = value; }
        }
        public void Design(IActivityDesigner designer)
        {
            designer.AddInput("Filter Query").WithDefaultValue("ResourceID='375'");
            designer.AddInput("Class").WithDefaultValue("SMS_R_SYSTEM");
            
            designer.AddOutput("Number of Objects");
        }
        public void Execute(IActivityRequest request, IActivityResponse response)
        {
            SCCMServer = settings.SCCMSERVER;
            userName = settings.UserName;
            password = settings.Password;

            String filter = request.Inputs["Filter Query"].AsString();
            String objClass = request.Inputs["Class"].AsString();

            //Setup WQL Connection and WMI Management Scope
            WqlConnectionManager connection = CMInterop.connectSCCMServer(SCCMServer, userName, password);

            using(connection)
            {  
                IResultObject col = CMInterop.getSCCMObject(connection, objClass, filter);

                CMInterop.removeSCCMObject(connection, filter, objClass);

                foreach (IResultObject obj in col)
                {
                    ObjCount++;
                }
                response.Publish("Number of Objects", ObjCount);
            }
        }
    }
}

