# Manual del Usuario — Rol: Administrador

**Sistema de Gestión Veterinaria — Veterinaria Ñandubay**  
**Versión:** 2.0  
**Fecha:** Mayo 2026  

---

## 1. Introducción

Bienvenido/a al sistema de gestión de **Veterinaria Ñandubay**. Como **Administrador**, tenés acceso completo a todas las funcionalidades del sistema, incluyendo la gestión de usuarios y la configuración general.

### ¿Qué podés hacer como Administrador?

| Módulo | Funciones |
|--------|-----------|
| **Dashboard** | Ver resumen general del día |
| **Mascotas (Pacientes)** | Gestión completa de pacientes |
| **Dueños (Clientes)** | Gestión completa de propietarios |
| **Agenda** | Gestión completa de turnos |
| **Historial Clínico** | Ficha clínica integral de cada paciente |
| **Vacunas** | Catálogo de vacunas |
| **Inventario** | Gestión completa de productos, categorías, marcas, proveedores, depósitos |
| **Ventas (POS)** | Registrar ventas y consultar historial |
| **Reportes** | Generar todos los reportes del sistema |
| **Veterinarios** | Gestión de profesionales veterinarios |
| **Administrar Usuarios** | Crear, editar y gestionar usuarios del sistema |

> **Nota:** El Administrador es el único rol con capacidad de crear, editar y gestionar los permisos de los usuarios del sistema. El Gerente puede ver el listado de usuarios y el log de auditoría de acciones.

---

## 2. Acceso al Sistema

### 2.1 Iniciar Sesión

1. Abrí el navegador web (Google Chrome o Microsoft Edge).
2. Ingresá la dirección del sistema.
3. Completá tu **usuario** y **contraseña**.
4. Hacé clic en **"Ingresar"**.

### 2.2 Cerrar Sesión

- En la esquina superior derecha, hacé clic en **"Cerrar Sesión"**.

---

## 3. Dashboard (Centro de Comando)

El dashboard es tu panel de control principal, mostrando:

- **Caja del Mes:** Total de ventas acumuladas en el mes.
- **Turnos de Hoy:** Cantidad completados sobre total del día.
- **Pacientes Activos:** Total de mascotas registradas activas.
- **Stock en Alerta:** Productos con stock por debajo del mínimo.
- **Historial de Caja:** Gráfico de ventas de los últimos 7 días.
- **Centro de Resoluciones:** Alertas activas (productos con stock bajo, etc.).

---

## 4. Mascotas (Pacientes)

Gestión completa de pacientes:

### 4.1 Ver listado
- Hacé clic en **"Mascotas (Pacientes)"** en el menú lateral.
- Filtrá por nombre, propietario o especie.

### 4.2 Registrar nuevo paciente
1. Hacé clic en **"Nuevo Paciente"**.
2. Completá los campos obligatorios: Nombre, Especie, Sexo, Propietario.
3. Opcionalmente: Raza, Fecha de Nacimiento, Observaciones.
4. Hacé clic en **"Guardar"**.

### 4.3 Editar / Ver detalle
- Usá los íconos de edición para modificar datos.
- Hacé clic en el nombre para ver el detalle completo.

---

## 5. Dueños (Clientes / Propietarios)

### 5.1 Registrar propietario
1. Hacé clic en **"Nuevo Propietario"**.
2. Campos obligatorios: Nombre, Apellido, DNI, Teléfono.
3. Opcionales: Email, Dirección.

> **Importante:** El DNI debe ser único por propietario.

### 5.2 Editar propietario
- Se pueden modificar todos los campos excepto el DNI.

---

## 6. Agenda

Gestión completa de turnos:

- **Crear turnos:** Seleccioná paciente, veterinario, servicio, fecha/hora.
- **Completar turnos:** Genera automáticamente una consulta en el historial clínico.
- **Cancelar turnos:** Marca el turno como cancelado.

---

## 7. Historial Clínico

Acceso completo a la ficha clínica de cada paciente:

### 7.1 Acceder
1. Menú lateral → **"Historial Clínico"**.
2. Buscá al paciente y hacé clic en **"Ver Ficha"**.

### 7.2 Secciones de la ficha

#### Consultas Médicas
- Ver todas las consultas del paciente.
- Registrar nuevas consultas manualmente.
- Las consultas se generan automáticamente al completar turnos.

#### Plan de Vacunación
- Ver todas las vacunaciones.
- Registrar nuevas vacunaciones seleccionando del catálogo.
- Campos: Vacuna, Fecha de aplicación, Próxima dosis, Observaciones.

#### Tratamientos Médicos
- Ver tratamientos activos y finalizados.
- Registrar nuevos tratamientos (Diagnóstico, Descripción, Medicación).
- **Finalizar tratamientos** en curso con el botón **"Finalizar"**.

---

## 8. Vacunas (Catálogo)

- Consultar el catálogo de vacunas disponibles.
- Registrar nuevas vacunas.
- Editar vacunas existentes.

> El catálogo define las vacunas que pueden aplicarse en la ficha clínica del paciente.

---

## 9. Inventario

Gestión completa del inventario:

### 9.1 Productos
- Ver listado con stock actual, precio, categoría, marca.
- Registrar nuevos productos: nombre, descripción, precio, stock, stock mínimo, categoría, marca, proveedor, depósito.
- Editar productos existentes.

### 9.2 Categorías
- Clasificaciones de productos (ej: Medicamentos, Alimentos, Accesorios).
- Crear, editar y eliminar categorías.

### 9.3 Marcas
- Marcas comerciales de los productos.
- Crear, editar y eliminar.

### 9.4 Proveedores
- Empresas proveedoras con datos de contacto.
- Crear, editar y eliminar.

### 9.5 Depósitos
- Ubicaciones físicas de almacenamiento.
- Crear, editar y eliminar.

> **Nota:** Al eliminar cualquier registro, este se desactiva (soft delete), no se borra permanentemente.

---

## 10. Ventas (POS)

### 10.1 Registrar venta
1. Menú lateral → **"Ventas (POS)"**.
2. Seleccioná el cliente o dejá como "Consumidor Final".
3. Agregá productos al carrito (buscar, indicar cantidad).
4. Revisá el resumen y hacé clic en **"Finalizar Venta"**.

> El stock se actualiza automáticamente al finalizar la venta.

---

## 11. Reportes

### 11.1 Acceder
- Menú lateral → **"Reportes"**.

### 11.2 Reportes disponibles

#### Resumen de Ventas
- Ventas por período con filtros por fecha y vendedor.
- Total de ingresos, cantidad de tickets.

#### Resumen de Stock
- Estado del inventario, productos con stock bajo.
- Filtros por categoría y marca.

#### Histórico de Tratamientos
- Todos los tratamientos realizados.
- Filtros por paciente, dueño, veterinario y período.
- Datos: fecha, paciente, dueño, veterinario, estado.

---

## 12. Veterinarios

- Consultar el listado de veterinarios con matrícula y especialidad.
- Registrar nuevos veterinarios.
- Editar datos de veterinarios existentes.

---

## 13. Administrar Usuarios

> **Exclusivo del Administrador.** Este módulo no es visible para otros roles operativos de forma completa.

### 13.1 Acceder
- En el menú lateral (sección inferior, separada visualmente), hacé clic en **"Administrar Usuarios"**.

### 13.2 Ver listado de usuarios
- Se muestra una tabla con todos los usuarios del sistema:
  - **Nombre de usuario**
  - **Nombre completo**
  - **Rol** (Admin, Gerente, Veterinario, Recepcionista)
  - **Estado** (Activo/Inactivo)
  - **Último login**

### 13.3 Crear nuevo usuario
1. Hacé clic en **"Nuevo Usuario"**.
2. Completá los campos:
   - **Nombre de usuario** (obligatorio, único)
   - **Nombre completo** (obligatorio)
   - **Contraseña** (obligatorio, mínimo 6 caracteres)
   - **Rol** (obligatorio): Seleccioná entre:
     - **Admin:** Acceso total.
     - **Gerente:** Acceso a reportes, personal e inventario (sin operaciones clínicas ni POS).
     - **Veterinario:** Acceso clínico + reportes.
     - **Recepcionista:** Acceso operativo (sin clínica ni reportes).
   - **Activo:** Indica si el usuario puede iniciar sesión.
3. Hacé clic en **"Guardar"**.

### 13.4 Editar usuario
1. Hacé clic en el ícono de edición del usuario.
2. Podés modificar: nombre completo, rol, estado activo.
3. Para cambiar la contraseña, completá el campo de nueva contraseña.
4. Hacé clic en **"Guardar"**.

### 13.5 Desactivar usuario
- Editá el usuario y desmarcá la opción **"Activo"**.
- El usuario no podrá iniciar sesión pero su registro se mantiene.

> **Atención:** No desactives tu propio usuario de administrador, ya que perderías acceso al sistema.

### 13.6 Roles y Permisos

| Funcionalidad | Admin | Gerente | Veterinario | Recepcionista |
|---------------|:-----:|:-------:|:-----------:|:-------------:|
| Dashboard | Sí | Sí | Sí | Sí |
| Mascotas (Pacientes) | Sí | Solo lectura | Sí | Sí |
| Dueños (Clientes) | Sí | Solo lectura | Sí | Sí |
| Agenda | Sí | Solo lectura (supervisión) | Sí | Sí |
| Historial Clínico | Sí | No | Sí | No |
| Vacunas | Sí | No | Sí | No |
| Inventario | Sí | Sí | Sí | Sí |
| Ventas (POS) | Sí | Solo lectura (reportes) | Sí | Sí |
| Reportes | Sí | Sí | Sí | No |
| Veterinarios | Sí | Sí | Sí | Sí |
| Administrar Usuarios | Sí | Solo lectura (logs) | No | No |

---

## 14. Buenas Prácticas de Administración

### 14.1 Gestión de usuarios
- Asigná el **rol mínimo necesario** a cada usuario (principio de menor privilegio).
- Desactivá usuarios que ya no trabajen en la clínica en lugar de eliminarlos.
- Cambiá las contraseñas periódicamente.

### 14.2 Inventario
- Revisá las alertas de stock diariamente en el Dashboard.
- Mantené actualizados los precios y categorías de productos.

### 14.3 Datos clínicos
- Asegurate de que los veterinarios completen los turnos para que las consultas se registren automáticamente.
- Verificá periódicamente que el catálogo de vacunas esté actualizado.

### 14.4 Reportes
- Consultá los reportes semanalmente para detectar tendencias.
- Usá el reporte de stock para planificar compras a proveedores.

---

## 15. Preguntas Frecuentes

**¿Puedo crear otro usuario administrador?**  
Sí. Podés crear múltiples usuarios con rol Admin.

**¿Qué pasa si desactivo todos los usuarios admin?**  
Perderás acceso al módulo de usuarios. Asegurate de siempre tener al menos un admin activo.

**¿Se pueden recuperar datos eliminados?**  
Sí. Las eliminaciones son "soft delete" (desactivaciones). Los registros permanecen en la base de datos y pueden reactivarse.

**¿Cómo sé qué versión del sistema estoy usando?**  
Consultá con el equipo de IT. La versión se puede verificar en la documentación técnica.

**¿Puedo exportar reportes?**  
Actualmente los reportes se visualizan en pantalla. Para exportar, usá la función de impresión del navegador (Ctrl+P) o la exportación a CSV disponible para los roles autorizados.

---

## 16. Glosario

| Término | Significado |
|---------|-------------|
| **Paciente** | Mascota registrada en el sistema |
| **Propietario** | Persona responsable de la mascota |
| **Ficha Clínica** | Expediente integral de un paciente |
| **POS** | Punto de Venta (Point of Sale) |
| **Soft Delete** | Desactivación lógica de un registro sin eliminación física |
| **Stock mínimo** | Umbral de alerta para reposición de inventario |
| **Rol** | Nivel de acceso del usuario (Admin, Gerente, Veterinario, Recepcionista) |
| **JWT** | Token de autenticación que valida la sesión del usuario |
