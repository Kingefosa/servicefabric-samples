Import-Module .\IoT-MgmtFunctions.psm1

Function Send-IoTEventHubMessages
{
 <#
  .Synopsis
   stops for processor
 #>
[CmdletBinding()]
param
(
	  [Parameter(Mandatory = $true,Position = 0, valueFromPipeline=$true)]
      $IoTProcessor,

      [Parameter(Mandatory = $false,Position = 1, valueFromPipeline=$false)]
      [int]
      $NumOfMessages = 1,

      [Parameter(Mandatory = $false,Position = 2, valueFromPipeline=$false)]
      [int]
      $NumOfPublishers = 1
) 
    $p = Get-IoTProcessor -IoTProcessor $IoTProcessor 
 
    foreach($h in $p.Hubs)
    {
        [IoTProcessorManagement.TestLib.IoTEventHubSender]::SendEventHubMessages($h.ConnectionString,
                                                                                 $h.EventHubName ,
                                                                                   $NumOfMessages,
                                                                                   $NumOfPublishers).Wait()
        
    }


}



<# Referenced Types #>
   Add-Type -Path '.\IoTProcessorManagement.Clients.dll'
   Add-Type -Path '.\IoTProcessorManagement.TestLib.dll'


   Export-ModuleMember -function Send-IoTEventHubMessages
