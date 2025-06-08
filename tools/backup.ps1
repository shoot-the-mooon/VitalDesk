# VitalDesk Manual Backup Script
# This script creates a backup of the VitalDesk database

param(
    [string]$BackupPath = ".\Backup",
    [string]$DatabasePath = ".\Temperatures.db"
)

# Function to write log messages
function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "[$timestamp] $Message"
}

try {
    Write-Log "Starting VitalDesk backup process..."
    
    # Check if database file exists
    if (-not (Test-Path $DatabasePath)) {
        Write-Log "ERROR: Database file not found at $DatabasePath"
        exit 1
    }
    
    # Create backup directory if it doesn't exist
    if (-not (Test-Path $BackupPath)) {
        New-Item -ItemType Directory -Path $BackupPath -Force | Out-Null
        Write-Log "Created backup directory: $BackupPath"
    }
    
    # Generate backup filename with timestamp
    $timestamp = Get-Date -Format "yyyyMMdd_HHmm"
    $backupFileName = "$timestamp.zip"
    $backupFilePath = Join-Path $BackupPath $backupFileName
    
    # Create ZIP archive
    Write-Log "Creating backup archive: $backupFilePath"
    Compress-Archive -Path $DatabasePath -DestinationPath $backupFilePath -Force
    
    # Verify backup was created
    if (Test-Path $backupFilePath) {
        $fileSize = (Get-Item $backupFilePath).Length
        Write-Log "Backup created successfully. Size: $fileSize bytes"
        
        # Clean up old backups (keep last 30 days)
        $cutoffDate = (Get-Date).AddDays(-30)
        $oldBackups = Get-ChildItem -Path $BackupPath -Filter "*.zip" | Where-Object { $_.CreationTime -lt $cutoffDate }
        
        if ($oldBackups.Count -gt 0) {
            Write-Log "Cleaning up $($oldBackups.Count) old backup(s)..."
            $oldBackups | Remove-Item -Force
        }
        
        Write-Log "Backup process completed successfully."
    } else {
        Write-Log "ERROR: Backup file was not created."
        exit 1
    }
}
catch {
    Write-Log "ERROR: $($_.Exception.Message)"
    exit 1
} 