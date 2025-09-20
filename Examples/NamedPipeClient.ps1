# PowerShell Named Pipe Client Example for FiendFriend
# This script demonstrates how to communicate with FiendFriend via Named Pipes

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("random", "setbase", "setface", "setboth", "status", "list")]
    [string]$Command,
    
    [string]$BaseImage = "",
    [string]$FaceImage = ""
)

$pipeName = "FiendFriend_IPC"

try {
    # Create the request object
    $request = @{
        command = $Command.ToLower()
        baseImage = $BaseImage
        faceImage = $FaceImage
        random = ($Command -eq "random")
    }
    
    # Convert to JSON
    $jsonRequest = $request | ConvertTo-Json -Compress
    
    # Connect to named pipe
    $pipe = New-Object System.IO.Pipes.NamedPipeClientStream(".", $pipeName, [System.IO.Pipes.PipeDirection]::InOut)
    $pipe.Connect(5000) # 5 second timeout
    
    # Send request
    $writer = New-Object System.IO.StreamWriter($pipe)
    $reader = New-Object System.IO.StreamReader($pipe)
    
    $writer.Write($jsonRequest)
    $writer.Flush()
    $pipe.WaitForPipeDrain()
    
    # Read response
    $response = $reader.ReadToEnd()
    
    # Parse and display response
    $responseObj = $response | ConvertFrom-Json
    Write-Host "Success: $($responseObj.success)" -ForegroundColor $(if($responseObj.success) { "Green" } else { "Red" })
    Write-Host "Message: $($responseObj.message)"
    
    if ($responseObj.currentBaseImage) {
        Write-Host "Current Base Image: $($responseObj.currentBaseImage)" -ForegroundColor Yellow
    }
    
    if ($responseObj.currentFaceImage) {
        Write-Host "Current Face Image: $($responseObj.currentFaceImage)" -ForegroundColor Yellow
    }
    
    if ($responseObj.availableBaseImages) {
        Write-Host "Available Base Images:" -ForegroundColor Cyan
        $responseObj.availableBaseImages | ForEach-Object { Write-Host "  - $_" }
    }
    
    if ($responseObj.availableFaceImages) {
        Write-Host "Available Face Images:" -ForegroundColor Cyan
        $responseObj.availableFaceImages | ForEach-Object { Write-Host "  - $_" }
    }
    
} catch {
    Write-Error "Failed to communicate with FiendFriend: $_"
} finally {
    if ($reader) { $reader.Close() }
    if ($writer) { $writer.Close() }
    if ($pipe) { $pipe.Close() }
}

# Usage Examples:
# .\NamedPipeClient.ps1 -Command "random"
# .\NamedPipeClient.ps1 -Command "status"
# .\NamedPipeClient.ps1 -Command "list"
# .\NamedPipeClient.ps1 -Command "setbase" -BaseImage "base1.png"
# .\NamedPipeClient.ps1 -Command "setboth" -BaseImage "base1.png" -FaceImage "face1.png"
