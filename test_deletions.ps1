[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

# 1. Login as Admin
$loginBody = @{
    nombreUsuario = "admin"
    password = "Admin123!"
} | ConvertTo-Json

$loginRes = Invoke-RestMethod -Uri "https://localhost:7204/api/v1/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginRes.data.token
$headers = @{ "Authorization" = "Bearer $token" }

Write-Host "=== TEST 1: CATEGORIA ==="
# Create dummy categoria
$catBody = @{ nombre = "Cat Test Deletion"; descripcion = "Testing deletion" } | ConvertTo-Json
$catRes = Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Categoria" -Method Post -Headers $headers -Body $catBody -ContentType "application/json"
$catId = $catRes.id
Write-Host "Created Categoria Id: $catId"

# Deactivate categoria (Activo = false)
$catUpdateBody = @{ id = $catId; nombre = "Cat Test Deletion"; descripcion = "Testing deletion"; activo = $false } | ConvertTo-Json
Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Categoria" -Method Put -Headers $headers -Body $catUpdateBody -ContentType "application/json"

# Check in soloActivas=false (must appear as Inactivo)
$allCats = (Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Categoria?soloActivas=false" -Method Get -Headers $headers).data.items
$foundCat = $allCats | Where-Object { $_.id -eq $catId }
Write-Host "Deactivated Categoria in soloActivas=false -> Found: $($foundCat -ne $null), Activo: $($foundCat.activo)"

# Check in soloActivas=true (must NOT appear)
$actCats = (Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Categoria?soloActivas=true" -Method Get -Headers $headers).data.items
$foundActCat = $actCats | Where-Object { $_.id -eq $catId }
Write-Host "Deactivated Categoria in soloActivas=true -> Found: $($foundActCat -ne $null)"

# Delete dummy categoria permanently
Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Categoria/$catId" -Method Delete -Headers $headers
$allCatsAfter = (Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Categoria?soloActivas=false" -Method Get -Headers $headers).data.items
$deletedCat = $allCatsAfter | Where-Object { $_.id -eq $catId }
Write-Host "Deleted Categoria in DB -> Found: $($deletedCat -ne $null)"


Write-Host "`n=== TEST 2: MARCA ==="
# Create dummy marca
$marcaBody = @{ nombre = "Marca Test HardDelete"; descripcion = "Testing deletion" } | ConvertTo-Json
$marcaRes = Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Marca" -Method Post -Headers $headers -Body $marcaBody -ContentType "application/json"
$marcaId = $marcaRes.id
Write-Host "Created Marca Id: $marcaId"

# Deactivate marca (Activo = false)
$marcaUpdateBody = @{ id = $marcaId; nombre = "Marca Test HardDelete"; descripcion = "Testing deletion"; activo = $false } | ConvertTo-Json
Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Marca" -Method Put -Headers $headers -Body $marcaUpdateBody -ContentType "application/json"

# Check in soloActivas=false
$allMarcas = (Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Marca?soloActivas=false" -Method Get -Headers $headers).data.items
$foundMarca = $allMarcas | Where-Object { $_.id -eq $marcaId }
Write-Host "Deactivated Marca in soloActivas=false -> Found: $($foundMarca -ne $null), Activo: $($foundMarca.activo)"

# Delete dummy marca permanently
Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Marca/$marcaId" -Method Delete -Headers $headers
$allMarcasAfter = (Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Marca?soloActivas=false" -Method Get -Headers $headers).data.items
$deletedMarca = $allMarcasAfter | Where-Object { $_.id -eq $marcaId }
Write-Host "Deleted Marca in DB -> Found: $($deletedMarca -ne $null)"


Write-Host "`n=== TEST 3: PROVEEDOR ==="
# Create dummy proveedor
$provBody = @{ razonSocial = "Prov Test HardDelete"; cuit = "30-99887766-5"; telefono = "11223344"; email = "p@test.com"; direccion = "Dir 123"; contacto = "Juan" } | ConvertTo-Json
$provRes = Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Proveedor" -Method Post -Headers $headers -Body $provBody -ContentType "application/json"
$provId = $provRes.id
Write-Host "Created Proveedor Id: $provId"

# Deactivate proveedor (Activo = false)
$provUpdateBody = @{ id = $provId; razonSocial = "Prov Test HardDelete"; cuit = "30-99887766-5"; telefono = "11223344"; email = "p@test.com"; direccion = "Dir 123"; contacto = "Juan"; activo = $false } | ConvertTo-Json
Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Proveedor" -Method Put -Headers $headers -Body $provUpdateBody -ContentType "application/json"

# Check in soloActivos=false
$allProvs = (Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Proveedor?soloActivos=false" -Method Get -Headers $headers).data.items
$foundProv = $allProvs | Where-Object { $_.id -eq $provId }
Write-Host "Deactivated Proveedor in soloActivos=false -> Found: $($foundProv -ne $null), Activo: $($foundProv.activo)"

# Delete dummy proveedor permanently
Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Proveedor/$provId" -Method Delete -Headers $headers
$allProvsAfter = (Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Proveedor?soloActivos=false" -Method Get -Headers $headers).data.items
$deletedProv = $allProvsAfter | Where-Object { $_.id -eq $provId }
Write-Host "Deleted Proveedor in DB -> Found: $($deletedProv -ne $null)"


Write-Host "`n=== TEST 4: DEPOSITO ==="
# Create dummy deposito
$depBody = @{ nombre = "Dep Test HardDelete"; ubicacion = "Pasillo Z"; sucursalId = 1 } | ConvertTo-Json
$depRes = Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Deposito" -Method Post -Headers $headers -Body $depBody -ContentType "application/json"
$depId = $depRes.id
Write-Host "Created Deposito Id: $depId"

# Deactivate deposito (Activo = false)
$depUpdateBody = @{ id = $depId; nombre = "Dep Test HardDelete"; ubicacion = "Pasillo Z"; sucursalId = 1; activo = $false } | ConvertTo-Json
Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Deposito" -Method Put -Headers $headers -Body $depUpdateBody -ContentType "application/json"

# Check in soloActivos=false
$allDeps = (Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Deposito?soloActivos=false" -Method Get -Headers $headers).data.items
$foundDep = $allDeps | Where-Object { $_.id -eq $depId }
Write-Host "Deactivated Deposito in soloActivos=false -> Found: $($foundDep -ne $null), Activo: $($foundDep.activo)"

# Delete dummy deposito permanently
Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Deposito/$depId" -Method Delete -Headers $headers
$allDepsAfter = (Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Deposito?soloActivos=false" -Method Get -Headers $headers).data.items
$deletedDep = $allDepsAfter | Where-Object { $_.id -eq $depId }
Write-Host "Deleted Deposito in DB -> Found: $($deletedDep -ne $null)"


Write-Host "`n=== TEST 5: SERVICIO ==="
# Create dummy servicio
$servBody = @{ nombre = "Servicio Test Delete"; descripcion = "Desc test"; duracionMinutos = 30; precio = 1500; productosUtilizados = "" } | ConvertTo-Json
$servRes = Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Servicio" -Method Post -Headers $headers -Body $servBody -ContentType "application/json"
$servId = $servRes.id
Write-Host "Created Servicio Id: $servId"

# Deactivate servicio (Activo = false)
$servUpdateBody = @{ id = $servId; nombre = "Servicio Test Delete"; descripcion = "Desc test"; duracionMinutos = 30; precio = 1500; activo = $false } | ConvertTo-Json
Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Servicio" -Method Put -Headers $headers -Body $servUpdateBody -ContentType "application/json"

# Check in soloActivos=false
$allServs = (Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Servicio?soloActivos=false" -Method Get -Headers $headers).data.items
$foundServ = $allServs | Where-Object { $_.id -eq $servId }
Write-Host "Deactivated Servicio in soloActivos=false -> Found: $($foundServ -ne $null), Activo: $($foundServ.activo)"

# Check in soloActivos=true
$actServs = (Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Servicio?soloActivos=true" -Method Get -Headers $headers).data.items
$foundActServ = $actServs | Where-Object { $_.id -eq $servId }
Write-Host "Deactivated Servicio in soloActivos=true -> Found: $($foundActServ -ne $null)"

# Delete dummy servicio permanently
Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Servicio/$servId" -Method Delete -Headers $headers
$allServsAfter = (Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Servicio?soloActivos=false" -Method Get -Headers $headers).data.items
$deletedServ = $allServsAfter | Where-Object { $_.id -eq $servId }
Write-Host "Deleted Servicio in DB -> Found: $($deletedServ -ne $null)"


Write-Host "`n=== TEST 6: ATTEMPT DELETE LINKED ITEMS (MUST BE REJECTED) ==="
try {
    # Attempt to delete Categoria 1 (has products)
    Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Categoria/1" -Method Delete -Headers $headers
    Write-Host "ERROR: Categoria 1 should not have been deleted!"
} catch {
    Write-Host "Expected rejection on linked Categoria 1: $($_.Exception.Message)"
}

try {
    # Attempt to delete Servicio 1 (Consulta General - has turnos)
    Invoke-RestMethod -Uri "https://localhost:7204/api/v1/Servicio/1" -Method Delete -Headers $headers
    Write-Host "ERROR: Servicio 1 should not have been deleted!"
} catch {
    Write-Host "Expected rejection on linked Servicio 1: $($_.Exception.Message)"
}
