[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

$token = (Invoke-RestMethod -Uri "https://localhost:7204/api/v1/auth/login" -Method Post -Body (@{nombreUsuario='admin';password='Admin123!'} | ConvertTo-Json) -ContentType 'application/json').data.token
$headers = @{ Authorization = "Bearer $token" }

try {
    $provBody = @{
        razonSocial = "Distribuidora Mascotas SA"
        cuit = "30-55443322-9"
        telefono = "011-4455-6677"
        email = "contacto@distribuidora.com"
        direccion = "Av. Corrientes 1234"
        contacto = "Marcos Gomez"
    } | ConvertTo-Json

    $res = Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Proveedor" -Method Post -Headers $headers -Body $provBody -ContentType "application/json"
    Write-Host "Created Proveedor: " ($res | ConvertTo-Json)
    $provId = $res.id

    # Deactivate
    $updateBody = @{
        id = $provId
        razonSocial = "Distribuidora Mascotas SA"
        cuit = "30-55443322-9"
        telefono = "011-4455-6677"
        email = "contacto@distribuidora.com"
        direccion = "Av. Corrientes 1234"
        contacto = "Marcos Gomez"
        activo = $false
    } | ConvertTo-Json

    Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Proveedor" -Method Put -Headers $headers -Body $updateBody -ContentType "application/json"
    Write-Host "Deactivated successfully!"

    # Check
    $all = (Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Proveedor?soloActivos=false" -Method Get -Headers $headers).data.items
    $found = $all | Where-Object { $_.id -eq $provId }
    Write-Host "Found in soloActivos=false: $($found -ne $null), Activo: $($found.activo)"

    # Delete
    Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Proveedor/$provId" -Method Delete -Headers $headers
    Write-Host "Deleted successfully!"

    $allAfter = (Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Proveedor?soloActivos=false" -Method Get -Headers $headers).data.items
    $foundAfter = $allAfter | Where-Object { $_.id -eq $provId }
    Write-Host "Found in DB after delete: $($foundAfter -ne $null)"
} catch {
    Write-Host "Error: " $_.Exception.Message
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    Write-Host "Body: " $reader.ReadToEnd()
}
