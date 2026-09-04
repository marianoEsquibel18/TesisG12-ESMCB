[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

$token = (Invoke-RestMethod -Uri "https://localhost:7204/api/v1/auth/login" -Method Post -Body (@{nombreUsuario='admin';password='Admin123!'} | ConvertTo-Json) -ContentType 'application/json').data.token
$headers = @{ Authorization = "Bearer $token" }

$catRes = Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Categoria" -Method Post -Headers $headers -Body (@{nombre="Cat Nueva 123"; descripcion="Test"} | ConvertTo-Json) -ContentType "application/json"
Write-Host "Cat Create Raw: " ($catRes | ConvertTo-Json -Depth 5)
$catId = $catRes.id
if (-not $catId -and $catRes.data) { $catId = $catRes.data.id }
Write-Host "Extracted CatId: $catId"

# Now Delete it
Write-Host "Deleting Categoria $catId..."
$delRes = Invoke-WebRequest -Uri "https://localhost:7204/api/v1/Categoria/$catId" -Method Delete -Headers $headers
Write-Host "Delete status code: $($delRes.StatusCode)"

# Verify it was deleted
$allCats = (Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Categoria?soloActivas=false" -Method Get -Headers $headers).data.items
$found = $allCats | Where-Object { $_.id -eq $catId }
Write-Host "Found in DB after delete: $($found -ne $null)"
