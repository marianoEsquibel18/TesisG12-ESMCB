[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

try {
    $body = @{ nombreUsuario = "admin"; password = "Admin123!" } | ConvertTo-Json
    $res = Invoke-WebRequest -Uri "https://localhost:7204/api/v1/auth/login" -Method Post -Body $body -ContentType "application/json"
    Write-Host "Success: " $res.Content
} catch {
    Write-Host "Status: " $_.Exception.Response.StatusCode
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    Write-Host "Response body: " $reader.ReadToEnd()
}
