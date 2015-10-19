$_default_ServiceFabricConnection = "localhost:19000"
$_default_MgmtSvcName= "fabric:/IoTProcessorManagementApp/ProcessorManagementService";



Function Ensure-IoTManagementApiEndPoint
(
	$EndPoint
)
{
	$newEndPoint = $EndPoint 
	if($newEndPoint -eq $null -or  $newEndPoint -eq "")
	{
		$newEndPoint = Get-IoTManagementApiEndPoint
	}

	$newEndPoint
}



Function Get-IoTProcessorName
(
	$IoTProcessor
)
{
		if ($IoTProcessor.GetType()  -Eq [String])
		{
			if([string]::IsNullOrEmpty($IoTProcessor))
			{
				Write-Error "Processor paramter is empty"
				return
			}
			$IoTProcessorName = $IoTProcessor
	    }
		else
		{

			<#									 
			if($IoTProcessor -isnot [IoTProcessorManagement.Clients.Processor])
			{
				Write-Error "Processor paramter is not of type [IoTProcessManagement.Clients.Processor]"
				exit
			}
			#>
			$IoTProcessorName = $IoTProcessor.Name
		}

	$IoTProcessorName 
}


Function Get-IoTManagementApiEndPoint
{
 <#
  .Synopsis
   Gets the endpoint address for IoT Management Service.
 #>
[CmdletBinding()]
param
(
      [Parameter(Mandatory = $false,Position = 0,valueFromPipeline=$false)]
      [string]
      $ServiceFabricEndPoint = $_default_ServiceFabricConnection,
      [Parameter(Mandatory = $false,Position = 1,valueFromPipeline=$false)]
      [string]
      $IoTManagementSvcName = $_default_MgmtSvcName
) 

	$MgmtEndpoint = [IoTProcessorManagement.TestLib.IoTManagementTestLib]::getMgmtEndPoint($ServiceFabricEndPoint, $IoTManagementSvcName).Result
	$MgmtEndpoint
}


Function Add-IoTProcessor
{
 <#
  .Synopsis
   Adds a processor
 #>
[CmdletBinding()]
param
(
	  [Parameter(Mandatory = $true,Position = 0,valueFromPipeline=$true)]
      [ValidateScript({Test-Path $_ -PathType 'leaf'})]  
      [string]
      $FilePath = "",

	  
	  [Parameter(Mandatory = $false,Position = 1,valueFromPipeline=$false)]
      #[ValidatePattern("/^(http?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?$/")]
	  [string]
      $ManagementEndPoint = ""
) 
	$ManagementEndPoint = Ensure-IoTManagementApiEndPoint -EndPoint $ManagementEndPoint


	$content = (Get-Content $FilePath) 

	$ret = [IoTProcessorManagement.TestLib.IoTManagementTestLib]::MgmtAddProcessor($ManagementEndPoint, $content).Result	
	$p = [ IoTProcessorManagement.TestLib.IoTManagementTestLib]::GetHttpResponseAsProcessor($ret).Result;
	$p
}



Function Remove-IoTProcessor
{
 <#
  .Synopsis
   stops for processor
 #>
[CmdletBinding()]
param
(
	 [Parameter(Mandatory = $true,Position = 0,valueFromPipeline=$true)]
	 [string]
      $IoTProcessor ,
	  [Parameter(Mandatory = $false,Position = 1,valueFromPipeline=$false)]
      [string]
      $ManagementEndPoint 
      
) 
	$ManagementEndPoint = Ensure-IoTManagementApiEndPoint -EndPoint $ManagementEndPoint
	$IoTProcessorName = Get-IoTProcessorName -IoTProcessor $IoTProcessor


	$ret = [IoTProcessorManagement.TestLib.IoTManagementTestLib]::MgmtDeleteProcessor($ManagementEndPoint, $IoTProcessorName ).Result	
}

Function Get-IoTProcessor
{
 <#
  .Synopsis
   Gets One or all processors
 #>
[CmdletBinding()]
param
(
	  [Parameter(Mandatory = $false,Position = 0,valueFromPipeline=$true)]
      $IoTProcessor ,
	  [Parameter(Mandatory = $false,Position = 1,valueFromPipeline=$false)]
	  [string]
      $ManagementEndPoint = ""
      
) 
	
	$ManagementEndPoint = Ensure-IoTManagementApiEndPoint -EndPoint $ManagementEndPoint
	
	if ($IoTProcessor -eq $null)
	{
		$ret = [IoTProcessorManagement.TestLib.IoTManagementTestLib]::MgmgGetAllProcesseros($ManagementEndPoint).Result	
		$p   = [ IoTProcessorManagement.TestLib.IoTManagementTestLib]::GetHttpResponseAsProcessors($ret).Result
	}
	else
	{
		$IoTProcessorName = Get-IoTProcessorName -IoTProcessor $IoTProcessor

		$ret = [ IoTProcessorManagement.TestLib.IoTManagementTestLib]::MgmtGetPrcossor($ManagementEndPoint, $IoTProcessorName).Result
		$p = [ IoTProcessorManagement.TestLib.IoTManagementTestLib]::GetHttpResponseAsProcessor($ret).Result;
	}

	$p;
}



Function Suspend-IoTProcessor
{
 <#
  .Synopsis
   Pauses a processor
 #>
[CmdletBinding()]
param
(
	  [Parameter(Mandatory = $true,Position = 0,valueFromPipeline=$true)]
      $IoTProcessor,
		
	  [Parameter(Mandatory = $false,Position = 0,valueFromPipeline=$false)]
      [string]
      $ManagementEndPoint 
    
) 
	$ManagementEndPoint = Ensure-IoTManagementApiEndPoint -EndPoint $ManagementEndPoint
	$IoTProcessorName = Get-IoTProcessorName -IoTProcessor $IoTProcessor
	$ret = [IoTProcessorManagement.TestLib.IoTManagementTestLib]::MgmtPauseProcessor($ManagementEndPoint, $IoTProcessorName).Result	
}

Function Resume-IoTProcessor
{
 <#
  .Synopsis
   resumes a processor
 #>
[CmdletBinding()]
param
(
      [Parameter(Mandatory = $true,Position = 0,valueFromPipeline=$true)]
      $IoTProcessor,

	  [Parameter(Mandatory = $false,Position = 1,valueFromPipeline=$false)]
      [string]
      $ManagementEndPoint 
)
	$ManagementEndPoint = Ensure-IoTManagementApiEndPoint -EndPoint $ManagementEndPoint
	$IoTProcessorName = Get-IoTProcessorName -IoTProcessor $IoTProcessor
 
	$ret = [IoTProcessorManagement.TestLib.IoTManagementTestLib]::MgmtResumeProcessor($ManagementEndPoint, $IoTProcessorName ).Result	
}

Function Stop-IoTProcessor
{
 <#
  .Synopsis
   stops for processor
 #>
[CmdletBinding()]
param
(
	  [Parameter(Mandatory = $true,Position = 0,valueFromPipeline=$true)]
      $IoTProcessor,

	
	  [Parameter(Mandatory = $false,Position = 1,valueFromPipeline=$false)]
      [switch]
      $drain,


	  [Parameter(Mandatory = $false,Position = 2,valueFromPipeline=$false)]
      [string]
      $ManagementEndPoint 
) 
	$ManagementEndPoint = Ensure-IoTManagementApiEndPoint -EndPoint $ManagementEndPoint
	$IoTProcessorName = Get-IoTProcessorName -IoTProcessor $IoTProcessor
 
	if($drain -eq $true)
	{
		$ret = [IoTProcessorManagement.TestLib.IoTManagementTestLib]::MgmtDrainStopProcessor($ManagementEndPoint, $IoTProcessorName).Result	
	}
	else
	{
		$ret = [IoTProcessorManagement.TestLib.IoTManagementTestLib]::MgmtStopProcessor($ManagementEndPoint, $IoTProcessorName).Result	
	}

	$ret
}



Function Update-IoTProcessor
{
 <#
  .Synopsis
   updates a processor
 #>
[CmdletBinding()]
param
(
	  [Parameter(Mandatory = $true,Position = 0,valueFromPipeline=$true)]
	  [ValidateScript({Test-Path $_ -PathType 'leaf'})]  
      [string]
      $FilePath = "",

	  [Parameter(Mandatory = $false,Position = 1,valueFromPipeline=$false)]
      [string]
      $ManagementEndPoint 
      
	  
)
	$ManagementEndPoint = Ensure-IoTManagementApiEndPoint -EndPoint $ManagementEndPoint

 
	$content = (Get-Content $FilePath) 
	$ret = [IoTProcessorManagement.TestLib.IoTManagementTestLib]::MgmtUpdateProcessor($ManagementEndPoint, $content).Result	
	$p = [ IoTProcessorManagement.TestLib.IoTManagementTestLib]::GetHttpResponseAsProcessor($ret).Result;
	$p
}




Function Get-IoTProcessorRuntimeStatus
{
 <#
  .Synopsis
   stops for processor
 #>
[CmdletBinding()]
param
(
	  [Parameter(Mandatory = $true,Position = 0,valueFromPipeline=$true)]
      $IoTProcessor,

	  [Parameter(Mandatory = $false,Position = 1,valueFromPipeline=$false)]
      [switch]
      $RollUp ,

	  [Parameter(Mandatory = $false,Position = 2,valueFromPipeline=$false)]
      [string]
      $ManagementEndPoint 
    


)
	$ManagementEndPoint = Ensure-IoTManagementApiEndPoint -EndPoint $ManagementEndPoint
	$IoTProcessorName = Get-IoTProcessorName -IoTProcessor $IoTProcessor
	
	$ret = [IoTProcessorManagement.TestLib.IoTManagementTestLib]::MgmtGetDetailedProcessorStatus($ManagementEndPoint, $IoTProcessorName).Result	
	$runtimeStatus = [IoTProcessorManagement.TestLib.IoTManagementTestLib]::GetHttpResponseAsRuntimeStatus($ret).Result;

	if($RollUp  -eq $true)
	{
		$count = $runtimeStatus.length
		
		$TotalPostedLastMinute    = 0;      
		$TotalProcessedLastMinute = 0;      
		$TotalPostedLastHour      = 0;      
		$TotalProcessedLastHour   = 0;      
		$AveragePostedPerMinLastHour  = 0;  
		$AverageProcessedPerMinLastHour = 0;
		$NumberOfActiveQueues = 0;
		$IsInErrorState  = $false;               
		$ErrorMessage    ="" ;      


		if($count-eq 0)
		{
			Write-Error "Runtime status for partitions in this processor are empty"
			return	
		}
		
		
		foreach ($RTStatus in $runtimeStatus) 
		{
			$TotalPostedLastMinute = $TotalPostedLastMinute + $RTStatus.TotalPostedLastMinute;
			$TotalProcessedLastMinute = $TotalProcessedLastMinute + $RTStatus.TotalProcessedLastMinute;      
			$TotalPostedLastHour      = $TotalPostedLastHour + $RTStatus.TotalProcessedLastMinute;      
			$TotalProcessedLastHour   = $TotalProcessedLastHour + $RTStatus.TotalProcessedLastMinute;      
			$AveragePostedPerMinLastHour  = $AveragePostedPerMinLastHour + $RTStatus.TotalProcessedLastMinute;  
			$AverageProcessedPerMinLastHour = $AverageProcessedPerMinLastHour + $RTStatus.TotalProcessedLastMinute;
			$NumberOfActiveQueues = $NumberOfActiveQueues + $RTStatus.TotalProcessedLastMinute;
			if($RTStatus.IsInErrorState -eq $true)
			{
				$IsInErrorState  = $true
			}

			$ErrorMessage = $ErrorMessage +  '`n' + $RTStatus.ErrorMessage
		}
			
		 

			$AveragePostedPerMinLastHour  = $AveragePostedPerMinLastHour / $count;  
			$AverageProcessedPerMinLastHour = $AverageProcessedPerMinLastHour / $count;

		

			# return as powershell object
				$properties = @{			
					'TotalPostedLastMinute' = $TotalPostedLastMinute ;
					'TotalProcessedLastMinute' = $TotalProcessedLastMinute ;      
					'TotalPostedLastHour'      = $TotalPostedLastHour ;      
					'TotalProcessedLastHour'   = $TotalProcessedLastHour ;      
					'AveragePostedPerMinLastHour'  = $AveragePostedPerMinLastHour ;  
					'AverageProcessedPerMinLastHour' = $AverageProcessedPerMinLastHour ;
					'NumberOfActiveQueues' = $NumberOfActiveQueues ;
					}

			$object = New-Object –TypeName PSObject –Prop $properties
			$object
	}
	else
	{
		$runtimeStatus
	}

}


<# IMPORT External Types #>

   Add-Type -Path '.\IoTProcessorManagement.Clients.dll'
   Add-Type -Path '.\IoTProcessorManagement.TestLib.dll'

<# Modules Export #>   
   Export-ModuleMember -function Get-IoTProcessor
   Export-ModuleMember -function Add-IoTProcessor
   Export-ModuleMember -function Update-IoTProcessor
    
   Export-ModuleMember -function Stop-IoTProcessor
   Export-ModuleMember -function Resume-IoTProcessor
   Export-ModuleMember -function Suspend-IoTProcessor
   Export-ModuleMember -function Remove-IoTProcessor
   Export-ModuleMember -function Get-IoTProcessorRuntimeStatus